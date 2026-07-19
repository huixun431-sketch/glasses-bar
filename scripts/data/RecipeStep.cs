using Godot;

namespace GlassesBar;

[GlobalClass]
public partial class RecipeStep : Resource
{
    [Export] public StringName Id { get; set; } = new();
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public StringName IngredientId { get; set; } = new();
    [Export] public double TargetAmount { get; set; }
    [Export] public IngredientUnit Unit { get; set; } = IngredientUnit.PrototypeUnit;
    [Export] public bool Required { get; set; } = true;
}

