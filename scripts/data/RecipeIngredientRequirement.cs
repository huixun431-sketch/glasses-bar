using System;
using Godot;

namespace GlassesBar;

[GlobalClass]
public partial class RecipeIngredientRequirement : Resource
{
    [Export] public StringName IngredientId { get; set; } = new();
    [Export] public double Amount { get; set; }
    [Export] public double MinimumAmount { get; set; }
    [Export] public double MaximumAmount { get; set; }
    [Export] public IngredientUnit Unit { get; set; } = IngredientUnit.PrototypeUnit;
    [Export] public string QuantityText { get; set; } = string.Empty;

    public void Validate()
    {
        var id = IngredientId.ToString();
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException("Recipe ingredient requirements require stable ingredient IDs.");
        if (Amount < 0d || MinimumAmount < 0d || MaximumAmount < 0d)
            throw new InvalidOperationException($"Recipe ingredient '{id}' cannot use negative quantities.");

        var hasExact = Amount > 0d;
        var hasMinimum = MinimumAmount > 0d;
        var hasMaximum = MaximumAmount > 0d;
        if (hasMinimum != hasMaximum)
            throw new InvalidOperationException($"Recipe ingredient '{id}' requires both range endpoints.");
        if (hasMinimum && MinimumAmount > MaximumAmount)
            throw new InvalidOperationException($"Recipe ingredient '{id}' has an inverted quantity range.");
        if (hasExact && hasMinimum)
            throw new InvalidOperationException($"Recipe ingredient '{id}' cannot use exact and ranged quantities together.");
        if (!hasExact && !hasMinimum && string.IsNullOrWhiteSpace(QuantityText))
            throw new InvalidOperationException($"Recipe ingredient '{id}' requires a quantity or source wording.");
        if ((hasExact || hasMinimum) && Unit == IngredientUnit.PrototypeUnit)
            throw new InvalidOperationException($"Recipe ingredient '{id}' requires a formal measurement unit.");
    }
}
