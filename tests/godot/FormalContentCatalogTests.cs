using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GlassesBar.Tests;

public partial class FormalContentCatalogTests : Node
{
    public override void _Ready()
    {
        try
        {
            RejectsDuplicateIngredientIds();
            RejectsPrototypeCatalogEntries();
            RejectsUnknownRecipeIngredients();
            RejectsInvalidQuantityRanges();
            LoadsApprovedStageOneCatalogs();
            UsesApprovedJiggerCapacities();
            GD.Print("FORMAL_CONTENT_CATALOG_PASS");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private static void UsesApprovedJiggerCapacities()
    {
        var catalog = ResourceLoader.Load<GameplayCatalogDefinition>(
            "res://data/gameplay/prototype_gameplay_catalog.tres");
        Require(catalog is not null, "the gameplay catalog loads for jigger verification");
        var tools = catalog!.BuildToolSpecs();
        Require(tools["jigger_small"].SmallMeasureAmount == 10d &&
                tools["jigger_small"].LargeMeasureAmount == 20d,
            "small jigger exposes 10/20 ml measures");
        Require(tools["jigger_medium"].SmallMeasureAmount == 15d &&
                tools["jigger_medium"].LargeMeasureAmount == 30d,
            "medium jigger exposes 15/30 ml measures");
        Require(tools["jigger_large"].SmallMeasureAmount == 25d &&
                tools["jigger_large"].LargeMeasureAmount == 50d,
            "large jigger exposes 25/50 ml measures");
    }

    private static void LoadsApprovedStageOneCatalogs()
    {
        var ingredientCatalog = ResourceLoader.Load<IngredientCatalogDefinition>(
            "res://data/ingredients/stage1_ingredients.tres");
        Require(ingredientCatalog is not null, "the stage-one ingredient catalog loads");
        var ingredientIndex = ingredientCatalog!.BuildValidatedIndex();
        Require(ingredientIndex.Count == 26, "the formal catalog contains 26 ingredient forms");

        var recipeCatalog = ResourceLoader.Load<RecipeCatalogDefinition>(
            "res://data/recipes/stage1_recipe_catalog.tres");
        Require(recipeCatalog is not null, "the stage-one recipe catalog loads");
        var recipes = recipeCatalog!.BuildValidatedIndex(ingredientIndex.Keys);
        Require(recipes.Count == 9, "the formal catalog contains exactly nine recipes");

        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["iced_americano"] = "crushed_ice=150:g|water=100:ml|coffee_beans=18:g",
            ["gin_and_tonic"] = "gin=50:ml|tonic_water=150:ml|lime_slice=1:piece|ice_cube=4-5:piece",
            ["old_fashioned"] = "whiskey=60:ml|aromatic_bitters=3:drop|sugar_cube=1:piece|orange_peel=1:piece|water=少量",
            ["mojito"] = "white_rum=50:ml|lime_juice=25:ml|soda_water=75:ml|simple_syrup=2:spoon|mint_leaves=未注明|crushed_ice=150:g",
            ["margarita"] = "tequila=50:ml|orange_liqueur=25:ml|lime_juice=25:ml|salt=少量|crushed_ice=120:g",
            ["moscow_mule"] = "vodka=50:ml|lime_juice=15:ml|ginger_beer=125:ml|crushed_ice=150:g|lime_wedge=1:piece",
            ["daiquiri"] = "white_rum=60:ml|lime_juice=25:ml|simple_syrup=20:ml|crushed_ice=120:g",
            ["martini"] = "gin=60:ml|dry_vermouth=10:ml|olive=1:piece|crushed_ice=100:g",
            ["whiskey_sour"] = "whiskey=60:ml|lemon_juice=30:ml|simple_syrup=2:spoon|egg_white=15:ml|aromatic_bitters=2-3:drop|crushed_ice=120:g"
        };
        foreach (var (id, signature) in expected)
            Require(RequirementSignature(recipes[id]) == signature, $"{id} matches the approved recipe text");

        Require(recipes["iced_americano"].ImplementationStatus == RecipeImplementationStatus.Partial,
            "iced americano is explicitly partial until the formal workflow is accepted");
        Require(recipes.Where(pair => pair.Key != "iced_americano")
                .All(pair => pair.Value.ImplementationStatus == RecipeImplementationStatus.CatalogOnly),
            "the other eight recipes remain catalog-only");
    }

    private static string RequirementSignature(RecipeDefinition recipe) =>
        string.Join("|", recipe.Ingredients.Select(requirement =>
        {
            var id = requirement.IngredientId.ToString();
            if (!string.IsNullOrWhiteSpace(requirement.QuantityText))
                return $"{id}={requirement.QuantityText}";
            var amount = requirement.Amount > 0d
                ? $"{requirement.Amount:0.###}"
                : $"{requirement.MinimumAmount:0.###}-{requirement.MaximumAmount:0.###}";
            return $"{id}={amount}:{UnitSuffix(requirement.Unit)}";
        }));

    private static string UnitSuffix(IngredientUnit unit) => unit switch
    {
        IngredientUnit.Milliliter => "ml",
        IngredientUnit.Gram => "g",
        IngredientUnit.Piece => "piece",
        IngredientUnit.Drop => "drop",
        IngredientUnit.Spoon => "spoon",
        _ => throw new InvalidOperationException($"Unexpected formal ingredient unit: {unit}")
    };

    private static void RejectsDuplicateIngredientIds()
    {
        var catalog = new IngredientCatalogDefinition();
        catalog.Ingredients.Add(new IngredientDefinition { Id = "water", IsPrototype = false });
        catalog.Ingredients.Add(new IngredientDefinition { Id = "water", IsPrototype = false });
        RequireThrows<InvalidOperationException>(() => catalog.BuildValidatedIndex(),
            "duplicate formal ingredient IDs are rejected");
    }

    private static void RejectsPrototypeCatalogEntries()
    {
        var ingredients = new IngredientCatalogDefinition();
        ingredients.Ingredients.Add(new IngredientDefinition { Id = "water", IsPrototype = true });
        RequireThrows<InvalidOperationException>(() => ingredients.BuildValidatedIndex(),
            "prototype ingredients are rejected from the formal catalog");

        var recipes = new RecipeCatalogDefinition();
        var recipe = new RecipeDefinition { Id = "prototype_recipe", IsPrototype = true };
        recipe.Ingredients.Add(new RecipeIngredientRequirement
        {
            IngredientId = "water",
            Unit = IngredientUnit.Milliliter,
            Amount = 10d
        });
        recipes.Recipes.Add(recipe);
        RequireThrows<InvalidOperationException>(
            () => recipes.BuildValidatedIndex(new[] { "water" }),
            "prototype recipes are rejected from the formal catalog");
    }

    private static void RejectsUnknownRecipeIngredients()
    {
        var ingredients = new IngredientCatalogDefinition();
        ingredients.Ingredients.Add(new IngredientDefinition { Id = "water", IsPrototype = false });
        var recipes = new RecipeCatalogDefinition();
        var recipe = new RecipeDefinition { Id = "broken", IsPrototype = false };
        recipe.Ingredients.Add(new RecipeIngredientRequirement
        {
            IngredientId = "missing",
            Unit = IngredientUnit.Milliliter,
            Amount = 10d
        });
        recipes.Recipes.Add(recipe);
        RequireThrows<InvalidOperationException>(
            () => recipes.BuildValidatedIndex(ingredients.BuildValidatedIndex().Keys),
            "unknown formal ingredient references are rejected");
    }

    private static void RejectsInvalidQuantityRanges()
    {
        var requirement = new RecipeIngredientRequirement
        {
            IngredientId = "ice_cube",
            Unit = IngredientUnit.Piece,
            MinimumAmount = 5d,
            MaximumAmount = 4d
        };
        RequireThrows<InvalidOperationException>(requirement.Validate,
            "an inverted formal quantity range is rejected");
    }

    private static void RequireThrows<T>(Action action, string message) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException(message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
