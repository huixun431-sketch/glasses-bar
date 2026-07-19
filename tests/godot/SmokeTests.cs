using Godot;
using GlassesBar.Domain;

namespace GlassesBar.Tests;

public partial class SmokeTests : Node
{
    public override void _Ready()
    {
        try
        {
            Run();
            GD.Print("SMOKE_TESTS_PASS");
            GetTree().Quit(0);
        }
        catch (System.Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private static void Run()
    {
        var flow = new DayFlow();
        Require(flow.TryAdvance(DayPhase.OrderReceived), "order transition");
        Require(flow.TryAdvance(DayPhase.RecipeObservation), "observation transition");
        Require(flow.TryAdvance(DayPhase.Preparation), "preparation transition");

        var container = new LiquidContainer(1d);
        Require(container.Add("water", 2d) == 1d, "capacity clamp");
        Require(container.SpilledAmount == 1d, "spill tracking");

        var mode = WorldMode.Reality;
        for (var index = 0; index < 100; index++)
            mode = mode == WorldMode.Reality ? WorldMode.Glasses : WorldMode.Reality;
        Require(mode == WorldMode.Reality, "100 world toggles return to reality");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new System.InvalidOperationException(message);
    }
}

