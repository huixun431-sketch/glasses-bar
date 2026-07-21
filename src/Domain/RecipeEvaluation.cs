using System;
using System.Collections.Generic;
using System.Linq;

namespace GlassesBar.Domain;

public sealed class DrinkSnapshot
{
    public HashSet<string> CompletedSteps { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, double> IngredientAmounts { get; } = new(StringComparer.Ordinal);
    public double ElapsedSeconds { get; set; }
    public double WastedAmount { get; set; }
    public double SpilledAmount { get; set; }
    public double CraftCompletionRatio { get; set; } = 1d;
    public int FailedOperations { get; set; }
}

public sealed class RecipeTargets
{
    public bool IsPrototype { get; init; } = true;
    public HashSet<string> RequiredSteps { get; } = new(StringComparer.Ordinal);
    public HashSet<string> RequiredIngredients { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, double> TargetAmounts { get; } = new(StringComparer.Ordinal);
    public double AmountToleranceRatio { get; init; } = 0.1d;
}

public sealed class DrinkEvaluation
{
    public bool Passed { get; init; }
    public double StepCompletionRatio { get; init; }
    public double IngredientCompletionRatio { get; init; }
    public double QuantityAccuracyRatio { get; init; }
    public double ElapsedSeconds { get; init; }
    public double WastedAmount { get; init; }
    public double SpilledAmount { get; init; }
    public double CraftCompletionRatio { get; init; }
    public int FailedOperations { get; init; }
    public IReadOnlyList<string> MissingSteps { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MissingIngredients { get; init; } = Array.Empty<string>();
}

public static class RecipeEvaluator
{
    public static DrinkEvaluation Evaluate(RecipeTargets targets, DrinkSnapshot drink)
    {
        var missingSteps = targets.RequiredSteps.Where(step => !drink.CompletedSteps.Contains(step)).ToArray();
        var missingIngredients = targets.RequiredIngredients
            .Where(id => !drink.IngredientAmounts.TryGetValue(id, out var amount) || amount <= 0d)
            .ToArray();

        var stepRatio = Ratio(targets.RequiredSteps.Count - missingSteps.Length, targets.RequiredSteps.Count);
        var ingredientRatio = Ratio(targets.RequiredIngredients.Count - missingIngredients.Length, targets.RequiredIngredients.Count);
        var quantityRatio = targets.IsPrototype ? 0d : EvaluateAmounts(targets, drink);

        return new DrinkEvaluation
        {
            Passed = missingSteps.Length == 0 && missingIngredients.Length == 0 &&
                     (targets.IsPrototype || quantityRatio >= 1d),
            StepCompletionRatio = stepRatio,
            IngredientCompletionRatio = ingredientRatio,
            QuantityAccuracyRatio = quantityRatio,
            ElapsedSeconds = drink.ElapsedSeconds,
            WastedAmount = drink.WastedAmount,
            SpilledAmount = drink.SpilledAmount,
            CraftCompletionRatio = Math.Clamp(drink.CraftCompletionRatio, 0d, 1d),
            FailedOperations = drink.FailedOperations,
            MissingSteps = missingSteps,
            MissingIngredients = missingIngredients
        };
    }

    private static double EvaluateAmounts(RecipeTargets targets, DrinkSnapshot drink)
    {
        if (targets.TargetAmounts.Count == 0)
            return 1d;

        var valid = 0;
        foreach (var target in targets.TargetAmounts)
        {
            drink.IngredientAmounts.TryGetValue(target.Key, out var actual);
            var tolerance = Math.Abs(target.Value) * Math.Max(0d, targets.AmountToleranceRatio);
            if (Math.Abs(actual - target.Value) <= tolerance)
                valid++;
        }

        return Ratio(valid, targets.TargetAmounts.Count);
    }

    private static double Ratio(int count, int total) => total == 0 ? 1d : (double)count / total;
}
