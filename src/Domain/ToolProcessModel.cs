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
    public HashSet<string> AllowedIngredientIds { get; } = new(StringComparer.Ordinal);

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
    public double RequiredAction { get; init; } = 0.5d;
    public HashSet<string> RequiredPlacementToolIds { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, double> InputTargets { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, double> Outputs { get; } = new(StringComparer.Ordinal);

    public OperationComplexity ResolveComplexity()
    {
        if (Complexity is OperationComplexity.Simple or OperationComplexity.Normal or OperationComplexity.Complex)
            return Complexity;
        if (CanRunOffBoard)
            return OperationComplexity.Simple;
        return string.IsNullOrWhiteSpace(RequiredHandheldToolId)
            ? OperationComplexity.Normal
            : OperationComplexity.Complex;
    }

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
        double randomRoll)
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

        if (!string.IsNullOrWhiteSpace(operation.RequiredHandheldToolId) &&
            !string.Equals(operation.RequiredHandheldToolId, heldHandheldToolId, StringComparison.Ordinal))
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
        var probability = averageDeviation <= 0.0001d ? 1d : completion;
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

    private static ProcessAttemptResult Failed(ProcessFailure failure) => new()
    {
        Failure = failure,
        MaterialsBecomeWaste = true,
        SuccessProbability = 0d,
        CompletionRatio = 0d
    };
}
