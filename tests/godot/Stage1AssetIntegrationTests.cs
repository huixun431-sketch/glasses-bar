using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GlassesBar.Tests;

public partial class Stage1AssetIntegrationTests : Node
{
    private static readonly Dictionary<string, string[]> ExpectedAnchors = new(StringComparer.Ordinal)
    {
        ["highball_glass"] = ["Grip", "Placement", "FillOrigin"],
        ["jigger_medium"] = ["Grip", "Placement", "FillOrigin", "Spout"],
        ["mortar"] = ["Grip", "Placement", "FillOrigin", "Interaction"],
        ["pestle"] = ["Grip", "Placement", "Interaction"]
    };

    public override void _Ready()
    {
        try
        {
            Run();
            GD.Print("STAGE1_ASSET_INTEGRATION_PASS");
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
        var main = GetNode<Node3D>("Main");
        main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Start")
            .EmitSignal(Button.SignalName.Pressed);

        foreach (var (assetId, anchors) in ExpectedAnchors)
        {
            var tool = Tool(main, assetId);
            var visual = tool.GetNodeOrNull<Node3D>("AssetVisual");
            Require(visual is not null, $"{assetId} uses its hand-written asset wrapper");
            Require(visual!.GetMeta("asset_id").AsString() == assetId,
                $"{assetId} wrapper preserves the stable asset ID");
            Require(!tool.GetNode<MeshInstance3D>("Visual").Visible,
                $"{assetId} hides the graybox mesh while the approved wrapper is available");
            Require(tool.GetNode<CollisionShape3D>("CollisionShape3D").Shape is not null,
                $"{assetId} retains the gameplay-owned collision fallback");
            foreach (var anchor in anchors)
                Require(visual.FindChild(anchor, true, false) is Node3D,
                    $"{assetId} wrapper exposes {anchor}");
            var placement = (Node3D)visual.FindChild("Placement", true, false);
            var collision = (CylinderShape3D)tool.GetNode<CollisionShape3D>("CollisionShape3D").Shape;
            var expectedContactY = tool.GlobalPosition.Y - collision.Height * 0.5f;
            var storedRotation = BarLayoutDefinition.Prototype.ItemStorageAssignments
                .Single(item => item.ItemId == assetId).LocalRotationDegrees;
            Require(storedRotation.IsZeroApprox()
                    ? Mathf.Abs(placement.GlobalPosition.Y - expectedContactY) < 0.005f
                    : tool.RotationDegrees.IsEqualApprox(storedRotation),
                $"{assetId} keeps either its contact-plane pose or its explicit cabinet storage pose");
        }

        GameSession.Instance.ToggleWorld();
        foreach (var assetId in ExpectedAnchors.Keys)
        {
            var visual = Tool(main, assetId).GetNode<Node3D>("AssetVisual");
            Require(EveryMeshMatchesOverrideState(visual, true),
                $"{assetId} receives the glasses-world material override");
        }

        GameSession.Instance.ToggleWorld();
        foreach (var assetId in ExpectedAnchors.Keys)
        {
            var visual = Tool(main, assetId).GetNode<Node3D>("AssetVisual");
            Require(EveryMeshMatchesOverrideState(visual, false),
                $"{assetId} restores imported reality-world materials");
        }

        var player = main.GetNode<PlayerController>("Player");
        var workstation = main.GetNode<DrinkWorkstation>("NeutralGameplay/DrinkWorkstation");
        var context = new InteractionContext { Player = player, Workstation = workstation };
        main.GetNode<CabinetInteractable>("NeutralGameplay/front_drawer_1_lower").SetOpen(true, false);
        Tool(main, "mortar").Interact(context);
        Tool(main, "pestle").Interact(context);

        var leftAnchor = player.GetNode<Node3D>("Head/Camera3D/LeftHandAnchor");
        var rightAnchor = player.GetNode<Node3D>("Head/Camera3D/RightHandAnchor");
        var leftHeld = leftAnchor.GetNode<Node3D>("HeldAssetVisual");
        var rightHeld = rightAnchor.GetNode<Node3D>("HeldAssetVisual");
        Require(leftHeld.Visible && leftHeld.GetMeta("asset_id").AsString() == "mortar",
            "left hand uses the approved mortar wrapper");
        Require(rightHeld.Visible && rightHeld.GetMeta("asset_id").AsString() == "pestle",
            "right hand uses the approved pestle wrapper");
        Require(!leftAnchor.GetNode<MeshInstance3D>("HeldTool").Visible &&
                !rightAnchor.GetNode<MeshInstance3D>("HeldTool").Visible,
            "approved held assets hide both first-person graybox meshes");
        Require(workstation.LeftHandToolId == "mortar" && workstation.RightHandToolId == "pestle",
            "presentation replacement preserves authoritative hand state");

        workstation.ResetForNewDay();
        player.ResetForNewDay();
        Require(!leftHeld.Visible && !rightHeld.Visible,
            "reset hides both first-person asset wrappers");
    }

    private static bool EveryMeshMatchesOverrideState(Node node, bool expectedOverride)
    {
        var sawMesh = false;
        foreach (var child in node.FindChildren("*", "MeshInstance3D", true, false))
        {
            sawMesh = true;
            if ((((MeshInstance3D)child).MaterialOverride is not null) != expectedOverride)
                return false;
        }
        return sawMesh;
    }

    private static ToolInteractable Tool(Node3D main, string id) =>
        main.GetTree().GetNodesInGroup("movable_tool")
            .OfType<ToolInteractable>()
            .Single(tool => tool.ToolId == id);

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
