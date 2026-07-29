using System;
using System.Collections.Generic;

namespace GlassesBar.Domain;

/// <summary>
/// Owns the authoritative state of the current drink and its day-scoped crafting
/// metrics. This gameplay state has no dependency on Godot or presentation systems.
/// </summary>
public sealed class DrinkAssemblyState
{
    private readonly DrinkSnapshot _snapshot = new();

    public DrinkAssemblyState(double glassCapacity)
    {
        Glass = new LiquidContainer(glassCapacity);
    }

    public LiquidContainer Glass { get; private set; }
    public double ElapsedSeconds => _snapshot.ElapsedSeconds;
    public double WastedAmount => _snapshot.WastedAmount;
    public double SpilledAmount => _snapshot.SpilledAmount;
    public double CraftCompletionRatio => _snapshot.CraftCompletionRatio;
    public int FailedOperations => _snapshot.FailedOperations;
    public IReadOnlySet<string> CompletedSteps => _snapshot.CompletedSteps;
    public IReadOnlyDictionary<string, double> IngredientAmounts => _snapshot.IngredientAmounts;

    public void AdvanceElapsed(double delta) =>
        _snapshot.ElapsedSeconds += Math.Max(0d, delta);

    public double AddProcessOutput(string ingredientId, double amount)
    {
        var spillBefore = Glass.SpilledAmount;
        var accepted = Glass.Add(ingredientId, amount);
        _snapshot.SpilledAmount += Glass.SpilledAmount - spillBefore;
        _snapshot.IngredientAmounts.TryGetValue(ingredientId, out var existing);
        _snapshot.IngredientAmounts[ingredientId] = existing + accepted;
        return accepted;
    }

    public void EmptyGlass() => Glass.Empty();

    public void RecordCompletedOperation(string operationId, double outputCompletion)
    {
        _snapshot.CompletedSteps.Add(operationId);
        _snapshot.CraftCompletionRatio =
            Math.Min(_snapshot.CraftCompletionRatio, outputCompletion);
    }

    public void RecordFailedOperation() => _snapshot.FailedOperations++;

    public void SetCraftCompletion(double completion) =>
        _snapshot.CraftCompletionRatio = completion;

    public double DiscardToolContents(ToolInstanceState target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var discarded = target.ContentAmount;
        if (target.Id == "highball_glass")
            Glass.Empty();
        target.ClearContents();
        _snapshot.WastedAmount += discarded;
        return discarded;
    }

    public DrinkEvaluation Evaluate(RecipeTargets targets, double drinkCompletionRatio)
    {
        ArgumentNullException.ThrowIfNull(targets);

        // Evaluation describes the current drink instance. Waste, spills, elapsed
        // time, and failed attempts remain day metrics, but discarded liquid and
        // completion from an earlier glass must never leak into a remade drink.
        _snapshot.IngredientAmounts.Clear();
        foreach (var ingredient in Glass.Ingredients)
            _snapshot.IngredientAmounts[ingredient.Key] = ingredient.Value;
        _snapshot.CraftCompletionRatio = drinkCompletionRatio;
        return RecipeEvaluator.Evaluate(targets, _snapshot);
    }

    public LiquidSnapshot CaptureGlassSnapshot() =>
        new()
        {
            Capacity = Glass.Capacity,
            SpilledAmount = Glass.SpilledAmount,
            Ingredients = new Dictionary<string, double>(Glass.Ingredients, StringComparer.Ordinal)
        };

    public HashSet<string> CaptureCompletedSteps() =>
        new(_snapshot.CompletedSteps, StringComparer.Ordinal);

    public void Restore(
        LiquidSnapshot glass,
        double elapsedSeconds,
        double wastedAmount,
        int failedOperations,
        IReadOnlySet<string> completedSteps)
    {
        ArgumentNullException.ThrowIfNull(glass);
        ArgumentNullException.ThrowIfNull(completedSteps);

        Glass = new LiquidContainer(glass.Capacity);
        Glass.Restore(glass.Ingredients, glass.SpilledAmount);

        _snapshot.CompletedSteps.Clear();
        _snapshot.CompletedSteps.UnionWith(completedSteps);
        _snapshot.IngredientAmounts.Clear();
        _snapshot.ElapsedSeconds = Math.Max(0d, elapsedSeconds);
        _snapshot.WastedAmount = Math.Max(0d, wastedAmount);
        _snapshot.SpilledAmount = Glass.SpilledAmount;
        _snapshot.CraftCompletionRatio = 1d;
        _snapshot.FailedOperations = Math.Max(0, failedOperations);
    }

    public void ResetForNewDay(double glassCapacity)
    {
        _snapshot.CompletedSteps.Clear();
        _snapshot.IngredientAmounts.Clear();
        _snapshot.WastedAmount = 0d;
        _snapshot.SpilledAmount = 0d;
        _snapshot.ElapsedSeconds = 0d;
        _snapshot.CraftCompletionRatio = 1d;
        _snapshot.FailedOperations = 0;
        Glass = new LiquidContainer(glassCapacity);
    }
}
