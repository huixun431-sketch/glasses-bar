using System;
using Godot;
using GlassesBar.Domain;

namespace GlassesBar.Tests;

public partial class FlowIntegrationTests : Node
{
    private bool _evaluationPassed;

    public override void _Ready() => CallDeferred(MethodName.RunDeferred);

    private void RunDeferred()
    {
        try
        {
            var main = GetNode<Node3D>("Main");
            var workstation = main.GetNode<DrinkWorkstation>("NeutralGameplay/DrinkWorkstation");
            var player = main.GetNode<PlayerController>("Player");
            var customer = main.GetNode<StationInteractable>("NeutralGameplay/customer");
            var reality = main.GetNode<Node3D>("RealityWorld");
            var glasses = main.GetNode<Node3D>("GlassesWorld");
            var realityChildren = reality.GetChildCount();
            var glassesChildren = glasses.GetChildCount();
            GameSession.Instance.EvaluationFinished += (passed, _) => _evaluationPassed = passed;

            Require(!main.HasNode("NeutralGameplay/serve_counter"), "legacy delivery counter is removed");
            Require(main.HasNode("NeutralGameplay/waste_bin"), "side waste bin is available");
            Require(reality.HasNode("CuttingBoard"), "cutting board anchors the center worktop");
            Require(!reality.HasNode("OperationManual") && glasses.HasNode("OperationManual"),
                "operation manual exists only in the glasses world");
            Require(reality.HasNode("coffee_beans") && !glasses.HasNode("coffee_beans"),
                "rear-bar raw ingredients are hidden in the glasses world");

            GameSession.Instance.AcceptOrder();
            GameSession.Instance.ToggleWorld();
            Require(GameSession.Instance.WorldMode == WorldMode.Glasses, "glasses mode entered");
            Require(!reality.Visible && glasses.Visible, "parallel presentation visibility");
            GameSession.Instance.ToggleWorld();
            Require(GameSession.Instance.Flow.Current == DayPhase.Preparation, "preparation phase entered");

            var context = new InteractionContext { Player = player, Workstation = workstation };
            workstation.TakeGlass();
            workstation.AddIce();
            CompleteTraditionalCoffee(main, context, true);
            workstation.AddLiquid("water", 4d);
            workstation.MarkWaterComplete();
            Require(workstation.Glass.SpilledAmount > 0d, "overflow recorded");
            var amountBeforeToggles = workstation.Glass.CurrentAmount;

            for (var index = 0; index < 100; index++)
                GameSession.Instance.ToggleWorld();
            Require(GameSession.Instance.WorldMode == WorldMode.Reality, "100 toggles return to reality");
            Require(Math.Abs(workstation.Glass.CurrentAmount - amountBeforeToggles) < 0.000001d, "liquid state survives toggles");
            Require(reality.GetChildCount() == realityChildren && glasses.GetChildCount() == glassesChildren,
                "world switching does not duplicate presentation nodes");

            workstation.DiscardAndReset();
            Require(!workstation.HasGlass && workstation.Glass.CurrentAmount == 0d, "discard resets drink");
            Require(workstation.TotalWaste > 0d, "discard tracks waste");

            workstation.TakeGlass();
            workstation.AddIce();
            workstation.AddLiquid("water", 0.5d);
            workstation.MarkWaterComplete();
            CompleteTraditionalCoffee(main, context, false);
            Require(!customer.CanInteract(context), "finished drink cannot be submitted from the initial distant position");
            player.GlobalPosition = new Vector3(0f, 0.9f, -0.9f);
            Require(customer.CanInteract(context), "finished drink can be submitted after approaching the customer");
            customer.Interact(context);

            Require(_evaluationPassed, "valid prototype drink passes");
            Require(GameSession.Instance.Flow.Current == DayPhase.DaySummary, "flow reaches day summary");
            Require(GameSession.Instance.CurrentDay == 1, "first completed service belongs to day one");

            player._UnhandledInput(new InputEventAction { Action = "next_day", Pressed = true, Strength = 1f });
            Require(GameSession.Instance.CurrentDay == 2, "next-day action increments the day counter");
            Require(GameSession.Instance.Flow.Current == DayPhase.WaitingForOrder, "next day returns to waiting for order");
            Require(!workstation.HasGlass && !workstation.HasMortarTool && !workstation.HasFilterTool,
                "next day resets held drink and traditional tools");
            Require(Math.Abs(player.GlobalPosition.Z - (-3f)) < 0.01f, "next day restores the bartender start position");
            Require(main.GetNode<Label>("HUD/Margin/Stack/Day").Text.Contains("2"), "HUD displays the new day number");
            Require(!main.GetNode<PanelContainer>("HUD/SummaryPanel").Visible, "day summary closes when the next day starts");
            GD.Print("FLOW_INTEGRATION_PASS");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private static void CompleteTraditionalCoffee(Node3D main, InteractionContext context, bool verifyGates)
    {
        var mortar = main.GetNode<StationInteractable>("NeutralGameplay/mortar_tool");
        var beans = main.GetNode<StationInteractable>("NeutralGameplay/coffee_beans");
        var grinding = main.GetNode<StationInteractable>("NeutralGameplay/grinding_station");
        var filterTool = main.GetNode<StationInteractable>("NeutralGameplay/filter_tool");
        var extraction = main.GetNode<StationInteractable>("NeutralGameplay/extraction_station");
        var filtering = main.GetNode<StationInteractable>("NeutralGameplay/filtering_station");

        if (verifyGates)
        {
            Require(!grinding.CanInteract(context), "grinding is blocked before taking its tool and ingredient");
            Require(!extraction.CanInteract(context), "extraction is blocked before grinding and taking the filter");
        }

        mortar.Interact(context);
        beans.Interact(context);
        Require(context.Workstation.HasMortarTool && context.Workstation.CoffeeBeansPortioned,
            "mortar and coffee beans are acquired separately before grinding");
        Require(grinding.Begin(context), "traditional grinding starts on the cutting board");
        grinding.UpdateOperation(1d, 0.7d);
        Require(grinding.Complete().Completed && context.Workstation.GroundCoffeeReady, "traditional grinding completes");

        if (verifyGates)
            Require(!extraction.CanInteract(context), "extraction still requires taking the filter tool");
        filterTool.Interact(context);
        Require(context.Workstation.HasFilterTool, "filter tool is acquired separately before extraction");
        Require(extraction.Begin(context), "manual extraction starts after grinding and filter pickup");
        extraction.UpdateOperation(1d, 0.8d);
        Require(extraction.Complete().Completed && context.Workstation.ExtractedCoffeeReady, "manual extraction completes");

        Require(filtering.Begin(context), "filtering starts only after extraction with a held glass");
        filtering.UpdateOperation(1d, 0.5d);
        Require(filtering.Complete().Completed && context.Workstation.FilteredCoffeeComplete,
            "traditional filtering transfers coffee into the glass");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
