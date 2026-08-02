using System;
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
