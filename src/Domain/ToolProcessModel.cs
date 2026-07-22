using System;
using System.Collections.Generic;
using System.Linq;

namespace GlassesBar.Domain;

public enum ToolCategory
{
    Automatic,
    Placement,
    Handheld
}

public enum OperationComplexity
{
    Automatic,
    Simple,
    Normal,
    Complex
}

public enum ProcessFailure
{
    None,
    InsufficientAction,
    WrongHandheldTool,
    WrongIngredients,
    ProportionCheckFailed
}

public sealed class ToolSpec
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public ToolCategory Category { get; init; } = ToolCategory.Automatic;
    public bool CanContainIngredients { get; init; }
    public bool CanCarryIngredients { get; init; }
    public bool UsedInHand { get; init; }
    public string BoardConflictGroup { get; init; } = string.Empty;
    public double FootprintRadius { get; init; } = 0.18d;
    public double SmallMeasureAmount { get; init; }
    public double LargeMeasureAmount { get; init; }
    public HashSet<string> AllowedIngredientIds { get; } = new(StringComparer.Ordinal);

    public bool HasDualMeasure => SmallMeasureAmount > 0d && LargeMeasureAmount > SmallMeasureAmount;

    public ToolCategory ResolveCategory()
    {
        if (Category is ToolCategory.Placement or ToolCategory.Handheld)
            return Category;
        return CanCarryIngredients || UsedInHand ? ToolCategory.Handheld : ToolCategory.Placement;
    }

    public bool CanCarry(string ingredientId) =>
        CanCarryIngredients && (AllowedIngredientIds.Count == 0 || AllowedIngredientIds.Contains(ingredientId));
}

public sealed class OperationSpec
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public OperationComplexity Complexity { get; init; } = OperationComplexity.Automatic;
    public bool CanRunOffBoard { get; init; }
    public bool IsPrototype { get; init; } = true;
    public string RequiredHandheldToolId { get; init; } = string.Empty;
    public string ResultTargetToolId { get; init; } = string.Empty;
    public string RepeatRecoveryInputIngredientId { get; init; } = string.Empty;
    public double RepeatRecoveryCap { get; init; } = 0.96d;
    public double RepeatRecoveryFraction { get; init; } = 0.42d;
    public double RequiredAction { get; init; } = 0.5d;
    public HashSet<string> RequiredPlacementToolIds { get; } = new(StringComparer.Ordinal);
    public HashSet<string> AllowedHandheldToolIds { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, double> InputTargets { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, double> Outputs { get; } = new(StringComparer.Ordinal);

    public OperationComplexity ResolveComplexity()
    {
        if (Complexity is OperationComplexity.Simple or OperationComplexity.Normal or OperationComplexity.Complex)
            return Complexity;
        if (CanRunOffBoard)
            return OperationComplexity.Simple;
        return !RequiresHandheldTool
            ? OperationComplexity.Normal
            : OperationComplexity.Complex;
    }

    public bool RequiresHandheldTool =>
        !string.IsNullOrWhiteSpace(RequiredHandheldToolId) || AllowedHandheldToolIds.Count > 0;

    public bool AcceptsHandheldTool(string toolId) => AllowedHandheldToolIds.Count > 0
        ? AllowedHandheldToolIds.Contains(toolId)
        : string.IsNullOrWhiteSpace(RequiredHandheldToolId) ||
          string.Equals(RequiredHandheldToolId, toolId, StringComparison.Ordinal);

    public bool IsEnabledBy(ISet<string> placementToolIds) =>
        RequiredPlacementToolIds.Count > 0 && RequiredPlacementToolIds.All(placementToolIds.Contains);
}

public sealed class ProcessAttemptResult
{
    public bool Completed { get; init; }
    public bool MaterialsBecomeWaste { get; init; }
    public ProcessFailure Failure { get; init; }
    public double SuccessProbability { get; init; }
    public double CompletionRatio { get; init; }
}

public static class ProcessRules
{
    public static bool ToolsConflict(ToolSpec first, ToolSpec second) =>
        first.Id != second.Id &&
        !string.IsNullOrWhiteSpace(first.BoardConflictGroup) &&
        string.Equals(first.BoardConflictGroup, second.BoardConflictGroup, StringComparison.Ordinal);

    public static ProcessAttemptResult Evaluate(
        OperationSpec operation,
        string heldHandheldToolId,
        IReadOnlyDictionary<string, double> ingredients,
        double action,
        double randomRoll,
        double successProbabilityPenalty = 0d)
    {
        if (Math.Max(0d, action) < Math.Max(0d, operation.RequiredAction))
        {
            return new ProcessAttemptResult
            {
                Failure = ProcessFailure.InsufficientAction,
                SuccessProbability = 1d,
                CompletionRatio = 0d
            };
        }

        if (!operation.AcceptsHandheldTool(heldHandheldToolId))
        {
            return Failed(ProcessFailure.WrongHandheldTool);
        }

        var actualTypes = ingredients.Where(pair => pair.Value > 0.000001d).Select(pair => pair.Key).ToHashSet(StringComparer.Ordinal);
        var requiredTypes = operation.InputTargets.Where(pair => pair.Value > 0.000001d).Select(pair => pair.Key).ToHashSet(StringComparer.Ordinal);
        if (!actualTypes.SetEquals(requiredTypes))
            return Failed(ProcessFailure.WrongIngredients);

        var deviations = new List<double>();
        foreach (var target in operation.InputTargets)
        {
            ingredients.TryGetValue(target.Key, out var actual);
            var denominator = Math.Max(Math.Abs(target.Value), 0.000001d);
            deviations.Add(Math.Abs(actual - target.Value) / denominator);
        }

        var averageDeviation = deviations.Count == 0 ? 0d : deviations.Average();
        var completion = Math.Clamp(1d - averageDeviation, 0d, 1d);
        // Exact prototype amounts can arrive through continuous input as tiny floating-point
        // deviations. Treat numerical noise as exact so a normal operation never fails at random.
        var probability = Math.Clamp((averageDeviation <= 0.0001d ? 1d : completion) -
                                     Math.Max(0d, successProbabilityPenalty), 0d, 1d);
        if (Math.Clamp(randomRoll, 0d, 1d) > probability)
        {
            return new ProcessAttemptResult
            {
                Failure = ProcessFailure.ProportionCheckFailed,
                MaterialsBecomeWaste = true,
                SuccessProbability = probability,
                CompletionRatio = completion
            };
        }

        return new ProcessAttemptResult
        {
            Completed = true,
            SuccessProbability = probability,
            CompletionRatio = completion
        };
    }

    public static double RecoverCompletion(double current, double cap, double recoveryFraction)
    {
        var safeCurrent = Math.Clamp(current, 0d, 1d);
        var safeCap = Math.Clamp(cap, safeCurrent, 1d);
        var safeFraction = Math.Clamp(recoveryFraction, 0d, 1d);
        return Math.Min(safeCap, safeCurrent + (safeCap - safeCurrent) * safeFraction);
    }

    private static ProcessAttemptResult Failed(ProcessFailure failure) => new()
    {
        Failure = failure,
        MaterialsBecomeWaste = true,
        SuccessProbability = 0d,
        CompletionRatio = 0d
    };
}
