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
            var reality = main.GetNode<Node3D>("RealityWorld");
            var glasses = main.GetNode<Node3D>("GlassesWorld");
            var realityChildren = reality.GetChildCount();
            var glassesChildren = glasses.GetChildCount();
            GameSession.Instance.EvaluationFinished += (passed, _) => _evaluationPassed = passed;

            GameSession.Instance.AcceptOrder();
            GameSession.Instance.ToggleWorld();
            Require(GameSession.Instance.WorldMode == WorldMode.Glasses, "glasses mode entered");
            Require(!reality.Visible && glasses.Visible, "parallel presentation visibility");
            GameSession.Instance.ToggleWorld();
            Require(GameSession.Instance.Flow.Current == DayPhase.Preparation, "preparation phase entered");

            workstation.TakeGlass();
            workstation.AddIce();
            workstation.AddLiquid("water", 4d);
            workstation.MarkWaterComplete();
            workstation.MarkGroundCoffee();
            workstation.AddLiquid("espresso", 0.2d);
            workstation.MarkEspressoComplete();
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
            workstation.MarkGroundCoffee();
            workstation.AddLiquid("espresso", 0.2d);
            workstation.MarkEspressoComplete();
            workstation.EvaluateAndFinish();

            Require(_evaluationPassed, "valid prototype drink passes");
            Require(GameSession.Instance.Flow.Current == DayPhase.DaySummary, "flow reaches day summary");
            GD.Print("FLOW_INTEGRATION_PASS");
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

