using System.Collections.Generic;
using GlassesBar.Domain;
using NUnit.Framework;

namespace GlassesBar.Tests;

public sealed class DomainTests
{
    [Test]
    public void DayFlow_OnlyAcceptsApprovedSequence()
    {
        var flow = new DayFlow();
        Assert.That(flow.TryAdvance(DayPhase.Preparation), Is.False);
        Assert.That(flow.TryAdvance(DayPhase.OrderReceived), Is.True);
        Assert.That(flow.TryAdvance(DayPhase.RecipeObservation), Is.True);
        Assert.That(flow.TryAdvance(DayPhase.Preparation), Is.True);
        Assert.That(flow.TryAdvance(DayPhase.Delivery), Is.True);
        Assert.That(flow.TryAdvance(DayPhase.Evaluation), Is.True);
        Assert.That(flow.TryAdvance(DayPhase.DaySummary), Is.True);
    }

    [Test]
    public void LiquidTransfer_ConservesAmountAndTracksSpill()
    {
        var result = LiquidMath.Transfer(5d, 2d, 4d, 3d);
        Assert.That(result.SourceAfter, Is.EqualTo(2d));
        Assert.That(result.DestinationAfter, Is.EqualTo(4d));
        Assert.That(result.Transferred, Is.EqualTo(2d));
        Assert.That(result.Spilled, Is.EqualTo(1d));
        Assert.That(result.SourceAfter + result.DestinationAfter + result.Spilled, Is.EqualTo(7d));
    }

    [Test]
    public void PrototypeRecipe_PassesOnRequiredStepsAndIngredientsWithoutQuantityGate()
    {
        var targets = new RecipeTargets { IsPrototype = true };
        targets.RequiredSteps.UnionWith(new[] { "take_glass", "add_water" });
        targets.RequiredIngredients.Add("water");
        targets.TargetAmounts["water"] = 1000d;

        var drink = new DrinkSnapshot();
        drink.CompletedSteps.UnionWith(new[] { "take_glass", "add_water" });
        drink.IngredientAmounts["water"] = 0.1d;

        var evaluation = RecipeEvaluator.Evaluate(targets, drink);
        Assert.That(evaluation.Passed, Is.True);
        Assert.That(evaluation.QuantityAccuracyRatio, Is.Zero);
    }

    [Test]
    public void FormalRecipe_UsesToleranceGate()
    {
        var targets = new RecipeTargets { IsPrototype = false, AmountToleranceRatio = 0.1d };
        targets.RequiredSteps.Add("pour");
        targets.RequiredIngredients.Add("water");
        targets.TargetAmounts["water"] = 100d;
        var drink = new DrinkSnapshot();
        drink.CompletedSteps.Add("pour");
        drink.IngredientAmounts["water"] = 120d;

        Assert.That(RecipeEvaluator.Evaluate(targets, drink).Passed, Is.False);
        drink.IngredientAmounts["water"] = 105d;
        Assert.That(RecipeEvaluator.Evaluate(targets, drink).Passed, Is.True);
    }
}

