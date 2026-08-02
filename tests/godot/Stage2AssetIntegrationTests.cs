using System;
using System.Collections.Generic;
using Godot;

namespace GlassesBar.Tests;

public partial class Stage2AssetIntegrationTests : Node
{
    private static readonly Dictionary<string, string[]> ExpectedAnchors = new(StringComparer.Ordinal)
    {
        ["traditional_filter"] = ["Grip", "Placement", "Spout", "Interaction"],
        ["bean_scoop"] = ["Grip", "Placement", "FillOrigin"],
        ["ice_tongs"] = ["Grip", "Placement", "Interaction"],
        ["jigger_small"] = ["Grip", "Placement", "FillOrigin", "Spout"],
        ["jigger_large"] = ["Grip", "Placement", "FillOrigin", "Spout"]
    };

    private static readonly string[] RightHandAssetIds =
        ["bean_scoop", "ice_tongs", "jigger_small", "jigger_large"];

    public override void _Ready()
    {
        try
        {
            Run();
            GD.Print("STAGE2_ASSET_INTEGRATION_PASS");
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
            var tool = main.GetNode<ToolInteractable>($"NeutralGameplay/{assetId}");
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
        }

        var player = main.GetNode<PlayerController>("Player");
        var workstation = main.GetNode<DrinkWorkstation>("NeutralGameplay/DrinkWorkstation");
        var context = new InteractionContext { Player = player, Workstation = workstation };

        GameSession.Instance.ToggleWorld();
        foreach (var assetId in ExpectedAnchors.Keys)
        {
            var tool = main.GetNode<ToolInteractable>($"NeutralGameplay/{assetId}");
            var visual = tool.GetNode<Node3D>("AssetVisual");
            Require(EveryMeshMatchesOverrideState(visual, true),
                $"{assetId} receives the glasses-world material override");
            Require(!tool.CanInteract(context),
                $"{assetId} remains observation-only in the glasses world");
        }

        GameSession.Instance.ToggleWorld();
        foreach (var assetId in ExpectedAnchors.Keys)
        {
            var visual = main.GetNode<Node3D>($"NeutralGameplay/{assetId}/AssetVisual");
            Require(EveryMeshMatchesOverrideState(visual, false),
                $"{assetId} restores imported reality-world materials");
        }

        var leftAnchor = player.GetNode<Node3D>("Head/Camera3D/LeftHandAnchor");
        var rightAnchor = player.GetNode<Node3D>("Head/Camera3D/RightHandAnchor");
        var leftGraybox = leftAnchor.GetNode<MeshInstance3D>("HeldTool");
        var rightGraybox = rightAnchor.GetNode<MeshInstance3D>("HeldTool");

        main.GetNode<ToolInteractable>("NeutralGameplay/traditional_filter").Interact(context);
        var leftHeld = leftAnchor.GetNode<Node3D>("HeldAssetVisual");
        Require(leftHeld.Visible && leftHeld.GetMeta("asset_id").AsString() == "traditional_filter",
            "left hand uses the traditional-filter wrapper");
        Require(!leftGraybox.Visible && workstation.LeftHandToolId == "traditional_filter",
            "left-hand visual replacement preserves the authoritative filter ID");
        ResetHands(workstation, player);
        Require(!leftHeld.Visible && !leftGraybox.Visible,
            "reset hides the left-hand stage-two wrapper and graybox");

        foreach (var assetId in RightHandAssetIds)
        {
            main.GetNode<ToolInteractable>($"NeutralGameplay/{assetId}").Interact(context);
            var rightHeld = rightAnchor.GetNode<Node3D>("HeldAssetVisual");
            Require(rightHeld.Visible && rightHeld.GetMeta("asset_id").AsString() == assetId,
                $"right hand uses the {assetId} wrapper");
            Require(!rightGraybox.Visible && workstation.RightHandToolId == assetId,
                $"right-hand visual replacement preserves the authoritative {assetId} ID");
            ResetHands(workstation, player);
            Require(!rightHeld.Visible && !rightGraybox.Visible,
                $"reset hides the right-hand {assetId} wrapper and graybox");
        }
    }

    private static void ResetHands(DrinkWorkstation workstation, PlayerController player)
    {
        workstation.ResetForNewDay();
        player.ResetForNewDay();
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

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
