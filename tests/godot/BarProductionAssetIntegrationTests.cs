using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GlassesBar.Tests;

public partial class BarProductionAssetIntegrationTests : Node
{
    private static readonly string[] MovingStableIds =
    {
        "front_drawer_1_upper", "front_drawer_1_lower",
        "front_drawer_2_upper", "front_drawer_2_lower",
        "front_drawer_3_upper", "front_drawer_3_lower",
        "front_drawer_4_upper", "front_drawer_4_lower",
        "rear_lower_cabinet_1_moving", "rear_lower_cabinet_2_moving",
        "rear_lower_cabinet_3_moving", "rear_lower_cabinet_4_moving",
        "rear_lower_cabinet_5_moving",
        "back_cabinet_1_left", "back_cabinet_1_right",
        "back_cabinet_2_left", "back_cabinet_2_right",
        "back_cabinet_3_left", "back_cabinet_3_right",
        "back_cabinet_4_left", "back_cabinet_4_right",
        "back_cabinet_5_left", "back_cabinet_5_right"
    };

    public override void _Ready() => CallDeferred(MethodName.RunDeferred);

    private void RunDeferred()
    {
        try
        {
            VerifyCompleteProductionSet();
            VerifyAtomicFallback();
            VerifyCompositionRootProductionAndFallback();
            GD.Print("BAR_PRODUCTION_ASSET_INTEGRATION_PASS");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private void VerifyCompleteProductionSet()
    {
        var (neutral, reality, glasses) = CreateWorldRoots("Complete");
        var loader = new BarEnvironmentVisualLoader();

        Require(loader.TryInstantiate(neutral, reality, glasses, out var visualSet),
            "All six formal modules must load as one production visual set.");
        Require(visualSet.NeutralModules.Keys.Order().SequenceEqual(
                new[] { "bar_backbar", "bar_counter", "bar_wear_overlays" }),
            "NeutralGameplay must own counter, backbar, and wear modules only.");
        Require(visualSet.RealityModules.Keys.Order().SequenceEqual(
                new[] { "bar_architecture", "bar_furniture", "bar_lighting" }),
            "RealityWorld must own architecture, furniture, and fixture geometry.");
        Require(visualSet.GlassesModules.Keys.Order().SequenceEqual(
                new[] { "bar_architecture", "bar_furniture", "bar_lighting" }),
            "GlassesWorld must reuse the three world-specific module resources.");

        foreach (var root in visualSet.AllRoots)
        {
            Require(root.FindChildren("*", "CollisionObject3D", true, false).Count == 0,
                $"Imported visual root {root.Name} must not own collision.");
            Require(root.FindChildren("*", "Light3D", true, false).Count == 0,
                $"Imported visual root {root.Name} must not own real lights.");
        }

        foreach (var stableId in MovingStableIds)
            Require(CountNamedDescendants(neutral, stableId) == 1,
                $"Production neutral visuals must contain one moving node named {stableId}.");
    }

    private void VerifyAtomicFallback()
    {
        var (neutral, reality, glasses) = CreateWorldRoots("Fallback");
        var loader = new BarEnvironmentVisualLoader("bar_lighting");

        Require(!loader.TryInstantiate(neutral, reality, glasses, out var visualSet),
            "A missing required module must reject the whole production set.");
        Require(visualSet is null, "Failed loading must not return a partial visual set.");
        Require(neutral.GetChildCount() == 0 && reality.GetChildCount() == 0 && glasses.GetChildCount() == 0,
            "Failed loading must leave all target roots untouched for full graybox fallback.");
    }

    private void VerifyCompositionRootProductionAndFallback()
    {
        var packed = ResourceLoader.Load<PackedScene>("res://scenes/Main.tscn") ??
            throw new InvalidOperationException("Main scene is missing.");

        var production = packed.Instantiate<GrayboxLevelBuilder>();
        production.Name = "ProductionMain";
        AddChild(production);
        var productionReality = production.GetNode<Node3D>("RealityWorld");
        var productionGlasses = production.GetNode<Node3D>("GlassesWorld");
        var productionNeutral = production.GetNode<Node3D>("NeutralGameplay");
        Require(productionReality.HasNode("ProductionArchitecture") &&
                productionGlasses.HasNode("ProductionArchitecture") &&
                productionNeutral.HasNode("ProductionCounter"),
            "Composition root must choose the complete production visual set when all modules load.");
        Require(!productionReality.HasNode("Floor") && !productionGlasses.HasNode("Floor"),
            "Production mode must not mix graybox room visuals with formal modules.");
        VerifyProductionLightRig(productionReality);
        VerifyProductionLightRig(productionGlasses);
        var wear = productionNeutral.GetNode<Node3D>("ProductionWearOverlays");
        Require(!wear.Visible, "Glasses-only wear overlays must be hidden in reality mode.");
        var stateProbe = productionNeutral.GetNode<CabinetInteractable>("front_drawer_1_upper");
        if (!GameSession.Instance.GameStarted)
            GameSession.Instance.StartNewGame();
        stateProbe.SetOpen(true, false);
        GameSession.Instance.ToggleWorld();
        Require(wear.Visible && stateProbe.IsOpen,
            "Glasses mode must show wear overlays without resetting authoritative cabinet state.");
        GameSession.Instance.ToggleWorld();
        Require(!wear.Visible && stateProbe.IsOpen,
            "Returning to reality must hide overlays without resetting cabinet state.");
        stateProbe.ResetClosed();
        GameSession.Instance.ReturnToMainMenu();
        foreach (var storage in BarLayoutDefinition.Prototype.Storages)
        {
            var cabinet = productionNeutral.GetNode<CabinetInteractable>(storage.Front.Id);
            Require(cabinet.HasNode("ProductionVisual"),
                $"Authoritative cabinet {storage.Front.Id} must own its imported moving visual.");
            Require(CountNamedDescendants(productionNeutral, storage.Front.Id) == 1,
                $"Stable gameplay ID {storage.Front.Id} must exist exactly once after visual binding.");
        }
        production.QueueFree();

        var fallback = packed.Instantiate<GrayboxLevelBuilder>();
        fallback.Name = "FallbackMain";
        fallback.ForceProductionVisualFallback = true;
        AddChild(fallback);
        var fallbackReality = fallback.GetNode<Node3D>("RealityWorld");
        var fallbackGlasses = fallback.GetNode<Node3D>("GlassesWorld");
        Require(!fallbackReality.HasNode("ProductionArchitecture") &&
                !fallbackGlasses.HasNode("ProductionArchitecture"),
            "Forced fallback must not leave any production module behind.");
        Require(fallbackReality.HasNode("FrontBarBody") && fallbackGlasses.HasNode("FrontBarBody") &&
                fallbackReality.HasNode("PlayerWorktop") && fallbackGlasses.HasNode("PlayerWorktop"),
            "Forced fallback must build the complete reality and glasses graybox.");
        Require(fallbackReality.GetNode<Node3D>("BarLightRig")
                    .FindChildren("*", "Light3D", true, false).Count == 14 &&
                fallbackGlasses.GetNode<Node3D>("BarLightRig")
                    .FindChildren("*", "Light3D", true, false).Count == 14,
            "Complete graybox fallback must retain all fourteen approved logical lights.");
        fallback.QueueFree();
    }

    private static void VerifyProductionLightRig(Node3D world)
    {
        var rig = world.GetNode<Node3D>("BarLightRig");
        Require(rig.FindChildren("*", "Light3D", true, false).Count == 14,
            $"{world.Name} must own fourteen project-authored lights.");
        Require(rig.FindChildren("*", "MeshInstance3D", true, false).Count == 0,
            "Production light rig must not duplicate imported fixture meshes.");
        for (var index = 1; index <= 3; index++)
        {
            var pendant = rig.FindChild($"lounge_pendant_{index}", true, false) as Node3D;
            Require(pendant?.GetNodeOrNull<SpotLight3D>("Light") is not null,
                $"Lounge pendant {index} must use a real downward Godot spot light.");
        }
    }

    private (Node3D Neutral, Node3D Reality, Node3D Glasses) CreateWorldRoots(string prefix)
    {
        var root = new Node3D { Name = prefix };
        AddChild(root);
        var neutral = new Node3D { Name = "NeutralGameplay" };
        var reality = new Node3D { Name = "RealityWorld" };
        var glasses = new Node3D { Name = "GlassesWorld" };
        root.AddChild(neutral);
        root.AddChild(reality);
        root.AddChild(glasses);
        return (neutral, reality, glasses);
    }

    private static int CountNamedDescendants(Node root, string name) =>
        root.FindChildren(name, "", true, false).Count;

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
