using System;
using System.Collections.Generic;
using Godot;

namespace GlassesBar;

[GlobalClass]
public partial class StationCatalogDefinition : Resource
{
    [Export] public Godot.Collections.Array<StationDefinition> Stations { get; set; } = new();

    public IReadOnlyDictionary<string, StationDefinition> BuildValidatedIndex()
    {
        var byId = new Dictionary<string, StationDefinition>(StringComparer.Ordinal);
        var kinds = new HashSet<StationKind>();
        foreach (var definition in Stations)
        {
            var id = definition.Id.ToString();
            var handlerId = definition.HandlerId.ToString();
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("Station definitions require stable IDs.");
            if (string.IsNullOrWhiteSpace(definition.DisplayName))
                throw new InvalidOperationException($"Station '{id}' requires a display name.");
            if (string.IsNullOrWhiteSpace(handlerId))
                throw new InvalidOperationException($"Station '{id}' requires a handler ID.");
            if (!byId.TryAdd(id, definition))
                throw new InvalidOperationException($"Duplicate station ID '{id}'.");
            if (!kinds.Add(definition.Kind))
                throw new InvalidOperationException($"Duplicate station kind '{definition.Kind}'.");

            if (handlerId == StationHandlerIds.IngredientSource &&
                (string.IsNullOrWhiteSpace(definition.IngredientId.ToString()) ||
                 definition.IngredientAmount <= 0d ||
                 string.IsNullOrWhiteSpace(definition.PromptTemplate)))
            {
                throw new InvalidOperationException(
                    $"Ingredient station '{id}' requires an ingredient, positive amount and prompt template.");
            }

            if (handlerId == StationHandlerIds.Customer && definition.InteractionDistance <= 0f)
                throw new InvalidOperationException(
                    $"Customer station '{id}' requires a positive interaction distance.");
        }

        return byId;
    }
}
