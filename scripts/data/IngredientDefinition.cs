using Godot;

namespace GlassesBar;

[GlobalClass]
public partial class IngredientDefinition : Resource
{
    [Export] public StringName Id { get; set; } = new();
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public IngredientUnit Unit { get; set; } = IngredientUnit.PrototypeUnit;
    [Export] public bool IsPrototype { get; set; } = true;
}

