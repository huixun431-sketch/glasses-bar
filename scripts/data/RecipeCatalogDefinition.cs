using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using RecipeIndex = System.Collections.Generic.Dictionary<string, GlassesBar.RecipeDefinition>;

namespace GlassesBar;

[GlobalClass]
public partial class RecipeCatalogDefinition : Resource
{
    [Export] public Array<RecipeDefinition> Recipes { get; set; } = new();

    public RecipeIndex BuildValidatedIndex(IEnumerable<string> ingredientIds)
    {
        var knownIngredients = new HashSet<string>(ingredientIds, StringComparer.Ordinal);
        var result = new RecipeIndex(StringComparer.Ordinal);
        foreach (var recipe in Recipes)
        {
            var id = recipe.Id.ToString();
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("Formal recipe definitions require stable IDs.");
            if (recipe.IsPrototype)
                throw new InvalidOperationException($"Formal recipe '{id}' cannot remain a prototype.");
            if (recipe.Ingredients.Count == 0)
                throw new InvalidOperationException($"Formal recipe '{id}' requires at least one ingredient.");
            if (!result.TryAdd(id, recipe))
                throw new InvalidOperationException($"Duplicate formal recipe ID: {id}");

            var recipeIngredientIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var requirement in recipe.Ingredients)
            {
                requirement.Validate();
                var ingredientId = requirement.IngredientId.ToString();
                if (!knownIngredients.Contains(ingredientId))
                    throw new InvalidOperationException(
                        $"Formal recipe '{id}' references unknown ingredient '{ingredientId}'.");
                if (!recipeIngredientIds.Add(ingredientId))
                    throw new InvalidOperationException(
                        $"Formal recipe '{id}' repeats ingredient '{ingredientId}'.");
            }
        }
        return result;
    }
}
