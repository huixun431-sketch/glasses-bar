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
            Require(neutral.GetNodeOrNull<Node3D>("sink_left_drawer_upper") is null &&
                    neutral.GetNodeOrNull<Node3D>("sink_left_drawer_lower") is null,
                "sink underbay has no legacy drawer nodes");
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
}
