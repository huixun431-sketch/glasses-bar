using Godot;
using Godot.Collections;
using GlassesBar.Domain;

namespace GlassesBar;

[GlobalClass]
public partial class ToolDefinition : Resource
{
    [Export] public StringName Id { get; set; } = new();
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public InteractionKind Interaction { get; set; } = InteractionKind.Operate;
    [Export] public bool RequiresContinuousInput { get; set; } = true;
    [Export] public ToolCategory Category { get; set; } = ToolCategory.Automatic;
    [Export] public bool CanContainIngredients { get; set; }
    [Export] public bool CanCarryIngredients { get; set; }
    [Export] public bool UsedInHand { get; set; }
    [Export] public string BoardConflictGroup { get; set; } = string.Empty;
    [Export(PropertyHint.Range, "0.05,0.6,0.01")] public double FootprintRadius { get; set; } = 0.18d;
    [Export(PropertyHint.Range, "0,200,1")] public double SmallMeasureAmount { get; set; }
    [Export(PropertyHint.Range, "0,200,1")] public double LargeMeasureAmount { get; set; }
    [Export] public Array<StringName> AllowedIngredientIds { get; set; } = new();

    public ToolSpec BuildSpec()
    {
        var spec = new ToolSpec
        {
            Id = Id.ToString(),
            DisplayName = DisplayName,
            Category = Category,
            CanContainIngredients = CanContainIngredients,
            CanCarryIngredients = CanCarryIngredients,
            UsedInHand = UsedInHand,
            BoardConflictGroup = BoardConflictGroup,
            FootprintRadius = FootprintRadius,
            SmallMeasureAmount = SmallMeasureAmount,
            LargeMeasureAmount = LargeMeasureAmount
        };
        foreach (var ingredientId in AllowedIngredientIds)
            spec.AllowedIngredientIds.Add(ingredientId.ToString());
        return spec;
    }
}
