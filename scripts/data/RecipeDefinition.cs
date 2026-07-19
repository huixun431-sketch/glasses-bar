using Godot;
using Godot.Collections;
using GlassesBar.Domain;

namespace GlassesBar;

[GlobalClass]
public partial class RecipeDefinition : Resource
{
    [Export] public StringName Id { get; set; } = new();
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public bool IsPrototype { get; set; } = true;
    [Export] public Array<RecipeStep> Steps { get; set; } = new();
    [Export] public ToleranceProfile? Tolerance { get; set; }

    public RecipeTargets BuildTargets()
    {
        var targets = new RecipeTargets
        {
            IsPrototype = IsPrototype || Tolerance?.EnableQuantityScoring != true,
            AmountToleranceRatio = Tolerance?.AmountToleranceRatio ?? 0.1
        };

        foreach (var step in Steps)
        {
            if (!step.Required)
                continue;

            targets.RequiredSteps.Add(step.Id.ToString());
            if (step.IngredientId.ToString().Length > 0)
            {
                targets.RequiredIngredients.Add(step.IngredientId.ToString());
                if (step.TargetAmount > 0)
                    targets.TargetAmounts[step.IngredientId.ToString()] = step.TargetAmount;
            }
        }

        return targets;
    }
}
