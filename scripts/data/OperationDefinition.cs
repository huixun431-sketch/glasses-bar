using Godot;
using Godot.Collections;
using GlassesBar.Domain;

namespace GlassesBar;

[GlobalClass]
public partial class OperationDefinition : Resource
{
    [Export] public StringName Id { get; set; } = new();
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public OperationComplexity Complexity { get; set; } = OperationComplexity.Automatic;
    [Export] public bool CanRunOffBoard { get; set; }
    [Export] public bool IsPrototype { get; set; } = true;
    [Export] public Array<StringName> RequiredPlacementToolIds { get; set; } = new();
    [Export] public StringName RequiredHandheldToolId { get; set; } = new();
    [Export] public Array<StringName> InputIngredientIds { get; set; } = new();
    [Export] public Array<double> InputTargetAmounts { get; set; } = new();
    [Export] public Array<StringName> OutputIngredientIds { get; set; } = new();
    [Export] public Array<double> OutputAmounts { get; set; } = new();
    [Export] public StringName ResultTargetToolId { get; set; } = new();
    [Export(PropertyHint.Range, "0,5,0.05")] public double RequiredAction { get; set; } = 0.5d;

    public OperationSpec BuildSpec()
    {
        var spec = new OperationSpec
        {
            Id = Id.ToString(),
            DisplayName = DisplayName,
            Complexity = Complexity,
            CanRunOffBoard = CanRunOffBoard,
            IsPrototype = IsPrototype,
            RequiredHandheldToolId = RequiredHandheldToolId.ToString(),
            ResultTargetToolId = ResultTargetToolId.ToString(),
            RequiredAction = RequiredAction
        };
        foreach (var toolId in RequiredPlacementToolIds)
            spec.RequiredPlacementToolIds.Add(toolId.ToString());
        for (var index = 0; index < InputIngredientIds.Count; index++)
            spec.InputTargets[InputIngredientIds[index].ToString()] = index < InputTargetAmounts.Count ? InputTargetAmounts[index] : 1d;
        for (var index = 0; index < OutputIngredientIds.Count; index++)
            spec.Outputs[OutputIngredientIds[index].ToString()] = index < OutputAmounts.Count ? OutputAmounts[index] : 1d;
        return spec;
    }
}
