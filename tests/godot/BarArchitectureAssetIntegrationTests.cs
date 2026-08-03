using System;
using System.Collections.Generic;
using Godot;

namespace GlassesBar.Tests;

// Generated integration skeleton for bar-architecture. It must exercise hand-authored
// wrappers and project-owned gameplay behavior, never imported GLB nodes directly.
public partial class BarArchitectureAssetIntegrationTests : Node
{
    private static readonly Dictionary<string, string[]> RequiredAnchors = new(StringComparer.Ordinal)
    {
        ["bar_architecture"] = new[] { "Placement" },
    };

    private static readonly Dictionary<string, string> WrapperPaths = new(StringComparer.Ordinal)
    {
        ["bar_architecture"] = "res://scenes/environment/modules/bar_architecture.tscn",
    };

    public override void _Ready()
    {
        try
        {
            Run();
            GD.Print("BarArchitecture_ASSET_INTEGRATION_PASS");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private void Run()
    {
        foreach (var pair in WrapperPaths)
        {
            var assetId = pair.Key;
            var scene = ResourceLoader.Load<PackedScene>(pair.Value);
            Require(scene is not null, "Missing hand-authored wrapper for " + assetId);
            var wrapper = scene!.Instantiate<Node3D>();
            AddChild(wrapper);
            Require(wrapper.GetMeta("asset_id").AsString() == assetId, "Stable asset ID mismatch for " + assetId);
            foreach (var anchor in RequiredAnchors[assetId])
                Require(wrapper.FindChild(anchor, true, false) is Node3D, "Missing " + anchor + " on " + assetId);
            Require(wrapper.FindChild(assetId, true, false) is Node3D,
                "Wrapper does not instance the imported visual root for " + assetId);
            Require(wrapper.FindChildren("*", "CollisionObject3D", true, false).Count == 0,
                "Imported architecture wrapper must not own gameplay collision");
            Require(wrapper.FindChildren("*", "Light3D", true, false).Count == 0,
                "Imported architecture wrapper must not contain real lights");
            var room = wrapper.FindChild("room_shell", true, false) as MeshInstance3D ??
                       throw new InvalidOperationException("Imported architecture has no room_shell mesh.");
            var size = room.GetAabb().Size;
            Require(Mathf.IsEqualApprox(size.X, 16f) &&
                    Mathf.IsEqualApprox(size.Y, 4.5f) &&
                    Mathf.IsEqualApprox(size.Z, 10f),
                $"Imported architecture axis conversion is wrong: room_shell size={size}.");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
