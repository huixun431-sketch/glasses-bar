using System;
using System.Collections.Generic;
using System.Linq;

namespace GlassesBar.Domain;

public sealed class GameplayCatalogSpecs
{
    public required IReadOnlyDictionary<string, ToolSpec> Tools { get; init; }
    public required IReadOnlyList<OperationSpec> Operations { get; init; }
}

public sealed class GameplayCatalogValidationException : Exception
{
    public GameplayCatalogValidationException(IEnumerable<string> errors)
        : base("Gameplay catalog is invalid: " + string.Join(" | ", errors))
    {
        Errors = errors.ToArray();
    }

    public IReadOnlyList<string> Errors { get; }
}

public static class GameplayCatalogValidator
{
    public static GameplayCatalogSpecs Validate(
        IReadOnlyDictionary<string, ToolSpec> tools,
        IReadOnlyList<OperationSpec> operations)
    {
        var errors = new List<string>();
        if (tools.Count == 0)
            errors.Add("At least one tool definition is required.");
        if (operations.Count == 0)
            errors.Add("At least one operation definition is required.");

        foreach (var tool in tools.Values)
        {
            if (string.IsNullOrWhiteSpace(tool.DisplayName))
                errors.Add($"Tool '{tool.Id}' requires a display name.");
            if (tool.FootprintRadius <= 0d)
                errors.Add($"Tool '{tool.Id}' requires a positive footprint radius.");
            if ((tool.SmallMeasureAmount > 0d || tool.LargeMeasureAmount > 0d) && !tool.HasDualMeasure)
                errors.Add($"Tool '{tool.Id}' must define a valid small/large measure pair.");
        }

        var operationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var operation in operations)
        {
            if (string.IsNullOrWhiteSpace(operation.Id) || !operationIds.Add(operation.Id))
                errors.Add($"Operation ID '{operation.Id}' must be non-empty and unique.");
            if (string.IsNullOrWhiteSpace(operation.DisplayName))
                errors.Add($"Operation '{operation.Id}' requires a display name.");
            if (operation.RequiredPlacementToolIds.Count == 0)
                errors.Add($"Operation '{operation.Id}' requires at least one placement tool.");
            foreach (var toolId in operation.RequiredPlacementToolIds)
            {
                if (!tools.TryGetValue(toolId, out var tool))
                    errors.Add($"Operation '{operation.Id}' references unknown placement tool '{toolId}'.");
                else if (tool.ResolveCategory() != ToolCategory.Placement)
                    errors.Add($"Operation '{operation.Id}' placement tool '{toolId}' is not placement-category.");
            }

            IEnumerable<string> handheldIds = operation.AllowedHandheldToolIds.Count > 0
                ? operation.AllowedHandheldToolIds
                : string.IsNullOrWhiteSpace(operation.RequiredHandheldToolId)
                    ? Array.Empty<string>()
                    : new[] { operation.RequiredHandheldToolId };
            foreach (var toolId in handheldIds)
            {
                if (!tools.TryGetValue(toolId, out var tool))
                    errors.Add($"Operation '{operation.Id}' references unknown handheld tool '{toolId}'.");
                else if (tool.ResolveCategory() != ToolCategory.Handheld)
                    errors.Add($"Operation '{operation.Id}' handheld tool '{toolId}' is not handheld-category.");
            }

            if (string.IsNullOrWhiteSpace(operation.ResultTargetToolId) ||
                !tools.TryGetValue(operation.ResultTargetToolId, out var resultTarget))
                errors.Add($"Operation '{operation.Id}' references an unknown result target.");
            else if (!resultTarget.CanContainIngredients)
                errors.Add($"Operation '{operation.Id}' result target '{resultTarget.Id}' cannot contain ingredients.");
            if (operation.InputTargets.Count == 0 || operation.InputTargets.Any(input => input.Value <= 0d))
                errors.Add($"Operation '{operation.Id}' requires positive input targets.");
            if (operation.Outputs.Count == 0 || operation.Outputs.Any(output => output.Value <= 0d))
                errors.Add($"Operation '{operation.Id}' requires positive outputs.");
            _ = operation.ResolveComplexity();
        }

        if (errors.Count > 0)
            throw new GameplayCatalogValidationException(errors);
        return new GameplayCatalogSpecs { Tools = tools, Operations = operations };
    }

    public static void ValidateRecipeCompatibility(RecipeTargets recipe, IReadOnlyList<OperationSpec> operations)
    {
        var operationIds = operations.Select(operation => operation.Id).ToHashSet(StringComparer.Ordinal);
        var outputIngredientIds = operations.SelectMany(operation => operation.Outputs.Keys)
            .ToHashSet(StringComparer.Ordinal);
        var errors = (string.IsNullOrWhiteSpace(recipe.Id)
                ? new[] { "Recipe requires a stable ID." }
                : Array.Empty<string>())
            .Concat(recipe.RequiredSteps.Where(step => !operationIds.Contains(step))
            .Select(step => $"Recipe references unknown operation '{step}'.")
            .Concat(recipe.RequiredIngredients.Where(ingredient => !outputIngredientIds.Contains(ingredient))
                .Select(ingredient => $"Recipe requires ingredient '{ingredient}' that no operation outputs.")))
            .ToArray();
        if (errors.Length > 0)
            throw new GameplayCatalogValidationException(errors);
    }
}
