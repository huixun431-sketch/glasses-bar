using System;
using System.Linq;
using Godot;

namespace GlassesBar.Tests;

public partial class BarRuntimeGeometryTests : Node
{
    public override void _Ready()
    {
        try
        {
            var main = GetNode<Node3D>("Main");
            var reality = main.GetNode<Node3D>("RealityWorld");
            var neutral = main.GetNode<Node3D>("NeutralGameplay");

            Require(!reality.HasNode("FrontBarWestChamfer") &&
                    !reality.HasNode("FrontBarEastChamfer") &&
                    !neutral.HasNode("FrontBarWestChamferCollider") &&
                    !neutral.HasNode("FrontBarEastChamferCollider"),
                "obsolete chamfer overlays are hard-deleted");

            var body = reality.GetNode<MeshInstance3D>("FrontBarBody");
            var playerTop = reality.GetNode<MeshInstance3D>("PlayerWorktop");
            var guestRiser = reality.GetNode<MeshInstance3D>("GuestCounterRiser");
            var guestTop = reality.GetNode<MeshInstance3D>("GuestCounterTop");
            Require(body.Mesh is ArrayMesh && playerTop.Mesh is ArrayMesh &&
                    guestRiser.Mesh is ArrayMesh && guestTop.Mesh is ArrayMesh,
                "front body and both tops are generated polygon meshes");

            var collider = neutral.GetNode<StaticBody3D>("FrontBarBodyCollider");
            Require(collider.GetChildCount() >= 4 &&
                    collider.GetChild(0) is CollisionShape3D { Shape: ConvexPolygonShape3D },
                "front collision is decomposed from the authoritative polygon");

            Require(body.GetAabb().Size.X > 7f && guestTop.GetAabb().Size.X > 9f,
                "new front geometry visibly spans the approved length");

            var forbiddenNames = new[]
            {
                "MergedBottleRackBack", "MergedShelf0", "MergedShelf1",
                "BottleRackBackCompatibility", "EastWetFixtures",
                "EastWetOuterSupport", "EastWetInnerSupport", "InspectionDoor",
                "FrontBarWestChamfer", "FrontBarEastChamfer"
            };
            var realityDescendants = reality.FindChildren("*", string.Empty, true, false);
            Require(forbiddenNames.All(name =>
                    realityDescendants.All(node => node.Name.ToString() != name)),
                "obsolete Z3 geometry is hard-deleted");
            Require(Enumerable.Range(0, 14).All(index =>
                    realityDescendants.All(node => node.Name.ToString() != $"BackLiquor{index}")),
                "placeholder rack bottles are absent");
            var rackNodes = realityDescendants.Count(node =>
                node.Name.ToString().StartsWith("BottleRackBay", StringComparison.Ordinal));
            Require(rackNodes == 15,
                "five rack bays contain one back and two shelves each");
            Require(reality.HasNode("FrontEastSinkFixtures") &&
                    !reality.HasNode("EastWetFixtures"),
                "front east sink replaces the obsolete side wet zone");
            Require(reality.HasNode("ExposedSinkPlumbing") &&
                    reality.GetNode<Node3D>("ExposedSinkPlumbing").GetChildCount() >= 4,
                "open sink underbay contains exposed drain and supply plumbing");
            Require(neutral.GetNodeOrNull<Node3D>("sink_left_drawer_upper") is null &&
                    neutral.GetNodeOrNull<Node3D>("sink_left_drawer_lower") is null,
                "sink underbay has no legacy drawer nodes");
            Require(neutral.FindChildren("*", string.Empty, true, false)
                    .All(node => !node.Name.ToString().StartsWith("sink_under_", StringComparison.Ordinal)),
                "sink underbay contains no storage or cabinet remnants");

            VerifyRoomContainment(main);
            VerifyStoredItemContainment(main, neutral);
            VerifyFrontDrawerStaticClearance(reality, neutral);
            VerifyStorageMotion(main, neutral);
            GD.Print("BAR_RUNTIME_GEOMETRY_PASS");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void VerifyRoomContainment(Node3D main)
    {
        var outerRoom = FromCenterSize(
            new Vector3(0f, (BarLayoutDefinition.RoomHeight - 0.10f) * 0.5f, 0f),
            new Vector3(
                BarLayoutDefinition.RoomWidth + BarLayoutDefinition.WallThickness * 1.1f,
                BarLayoutDefinition.RoomHeight + 0.50f,
                BarLayoutDefinition.RoomDepth + BarLayoutDefinition.WallThickness * 1.1f));
        foreach (var mesh in DescendantMeshes(main))
        {
            if (mesh.Mesh is null || !mesh.IsVisibleInTree())
                continue;
            var bounds = TransformAabb(mesh.GetAabb(), mesh.GlobalTransform);
            Require(Contains(outerRoom, bounds, 0.03f),
                $"ItemId={mesh.Name}; StorageId=room; Intersects=outside_room; Sample=closed; Bounds={bounds}");
        }
    }

    private static void VerifyStoredItemContainment(Node3D main, Node3D neutral)
    {
        var layout = BarLayoutDefinition.Prototype;
        foreach (var assignment in layout.ItemStorageAssignments)
        {
            var storage = layout.Storages.Single(item => item.Id == assignment.StorageId);
            var host = neutral.FindChild(assignment.StorageId + "_host", true, false) as Node3D ??
                       throw new InvalidOperationException($"Storage host not found: {assignment.StorageId}");
            var item = main.GetTree().GetNodesInGroup("interactable")
                .OfType<Node3D>()
                .Single(node => node.Name == assignment.ItemId);
            var itemBounds = MergeBounds(DescendantMeshes(item));
            var hostBounds = FromCenterSize(host.GlobalPosition, storage.HostSize);
            Require(Contains(hostBounds, itemBounds, 0.015f),
                $"ItemId={assignment.ItemId}; StorageId={assignment.StorageId}; " +
                $"Intersects=host_envelope; Sample=closed; Item={itemBounds}; Host={hostBounds}");
        }
    }

    private static void VerifyStorageMotion(Node3D main, Node3D neutral)
    {
        var cabinets = main.GetTree().GetNodesInGroup("cabinet_storage")
            .OfType<CabinetInteractable>()
            .ToArray();
        foreach (var cabinet in cabinets)
        {
            foreach (var other in cabinets)
                other.SetOpen(false, false);
            var closedPosition = cabinet.Position;
            var closedRotation = cabinet.Rotation.Y;
            var movingLeaf = cabinet.GetNodeOrNull<Node3D>("MovingLeaf");
            var closedLeafPosition = movingLeaf?.Position ?? Vector3.Zero;
            cabinet.SetOpen(true, false);
            var openPosition = cabinet.Position;
            var openRotation = cabinet.Rotation.Y;
            var openLeafPosition = movingLeaf?.Position ?? Vector3.Zero;

            foreach (var sample in new[] { 0f, 0.25f, 0.50f, 0.75f, 1f })
            {
                cabinet.Position = closedPosition.Lerp(openPosition, sample);
                cabinet.Rotation = new Vector3(
                    0f,
                    Mathf.LerpAngle(closedRotation, openRotation, sample),
                    0f);
                if (movingLeaf is not null)
                    movingLeaf.Position = closedLeafPosition.Lerp(openLeafPosition, sample);
                var currentBounds = MergeBounds(DescendantMeshes(cabinet));
                foreach (var other in cabinets.Where(other => other != cabinet))
                {
                    var otherBounds = MergeBounds(DescendantMeshes(other));
                    Require(!Overlaps(currentBounds, otherBounds, 0.01f),
                        $"ItemId={cabinet.Name}; StorageId={cabinet.Name}; " +
                        $"Intersects={other.Name}; Sample={sample:0.##}");
                }
            }
            cabinet.Position = closedPosition;
            cabinet.Rotation = new Vector3(0f, closedRotation, 0f);
            if (movingLeaf is not null)
                movingLeaf.Position = closedLeafPosition;
        }

        foreach (var cabinet in cabinets)
            cabinet.SetOpen(false, false);
    }

    private static void VerifyFrontDrawerStaticClearance(Node3D reality, Node3D neutral)
    {
        var staticMeshes = new System.Collections.Generic.List<MeshInstance3D>
        {
            reality.GetNode<MeshInstance3D>("FrontBarBody")
        };
        var carcass = reality.GetNodeOrNull<Node3D>("FrontStaticCarcass");
        if (carcass is not null)
            staticMeshes.AddRange(DescendantMeshes(carcass));

        var drawers = neutral.GetChildren().OfType<CabinetInteractable>()
            .Where(cabinet => cabinet.Name.ToString().StartsWith("front_drawer_", StringComparison.Ordinal))
            .ToArray();
        foreach (var drawer in drawers)
        {
            drawer.SetOpen(false, false);
            var closed = drawer.Position;
            drawer.SetOpen(true, false);
            var open = drawer.Position;
            foreach (var sample in new[] { 0f, 0.25f, 0.50f, 0.75f, 1f })
            {
                drawer.Position = closed.Lerp(open, sample);
                var drawerBounds = MergeBounds(DescendantMeshes(drawer));
                foreach (var mesh in staticMeshes)
                {
                    var staticBounds = TransformAabb(mesh.GetAabb(), mesh.GlobalTransform);
                    Require(!Overlaps(drawerBounds, staticBounds, 0.005f),
                        $"ItemId={drawer.Name}; StorageId={drawer.Name}; Intersects={mesh.Name}; Sample={sample:0.##}");
                }
            }
            drawer.Position = closed;
        }
    }

    private static Aabb MergeBounds(System.Collections.Generic.IEnumerable<MeshInstance3D> meshes)
    {
        var hasBounds = false;
        var merged = new Aabb();
        foreach (var mesh in meshes)
        {
            if (mesh.Mesh is null)
                continue;
            var bounds = TransformAabb(mesh.GetAabb(), mesh.GlobalTransform);
            merged = hasBounds ? merged.Merge(bounds) : bounds;
            hasBounds = true;
        }
        if (!hasBounds)
            throw new InvalidOperationException("Runtime node has no mesh bounds.");
        return merged;
    }

    private static System.Collections.Generic.IEnumerable<MeshInstance3D> DescendantMeshes(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is MeshInstance3D mesh)
                yield return mesh;
            foreach (var descendant in DescendantMeshes(child))
                yield return descendant;
        }
    }

    private static Aabb TransformAabb(Aabb local, Transform3D transform)
    {
        var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        for (var x = 0; x <= 1; x++)
        for (var y = 0; y <= 1; y++)
        for (var z = 0; z <= 1; z++)
        {
            var corner = local.Position + new Vector3(
                local.Size.X * x,
                local.Size.Y * y,
                local.Size.Z * z);
            var world = transform * corner;
            min = new Vector3(
                Math.Min(min.X, world.X),
                Math.Min(min.Y, world.Y),
                Math.Min(min.Z, world.Z));
            max = new Vector3(
                Math.Max(max.X, world.X),
                Math.Max(max.Y, world.Y),
                Math.Max(max.Z, world.Z));
        }
        return new Aabb(min, max - min);
    }

    private static Aabb FromCenterSize(Vector3 center, Vector3 size) =>
        new(center - size * 0.5f, size);

    private static bool Contains(Aabb outer, Aabb inner, float epsilon)
    {
        var outerEnd = outer.Position + outer.Size;
        var innerEnd = inner.Position + inner.Size;
        return inner.Position.X >= outer.Position.X - epsilon &&
               inner.Position.Y >= outer.Position.Y - epsilon &&
               inner.Position.Z >= outer.Position.Z - epsilon &&
               innerEnd.X <= outerEnd.X + epsilon &&
               innerEnd.Y <= outerEnd.Y + epsilon &&
               innerEnd.Z <= outerEnd.Z + epsilon;
    }

    private static bool Overlaps(Aabb first, Aabb second, float epsilon)
    {
        var firstEnd = first.Position + first.Size;
        var secondEnd = second.Position + second.Size;
        return first.Position.X < secondEnd.X - epsilon && firstEnd.X > second.Position.X + epsilon &&
               first.Position.Y < secondEnd.Y - epsilon && firstEnd.Y > second.Position.Y + epsilon &&
               first.Position.Z < secondEnd.Z - epsilon && firstEnd.Z > second.Position.Z + epsilon;
    }
}
