using System;
using Godot;
using Godot.Collections;
using IngredientIndex = System.Collections.Generic.Dictionary<string, GlassesBar.IngredientDefinition>;

namespace GlassesBar;

[GlobalClass]
public partial class IngredientCatalogDefinition : Resource
{
    [Export] public Array<IngredientDefinition> Ingredients { get; set; } = new();

    public IngredientIndex BuildValidatedIndex()
    {
        var result = new IngredientIndex(StringComparer.Ordinal);
        foreach (var ingredient in Ingredients)
        {
            var id = ingredient.Id.ToString();
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("Formal ingredient definitions require stable IDs.");
            if (ingredient.IsPrototype)
                throw new InvalidOperationException($"Formal ingredient '{id}' cannot remain a prototype.");
            if (!result.TryAdd(id, ingredient))
                throw new InvalidOperationException($"Duplicate formal ingredient ID: {id}");
        }
        return result;
    }
}
