using Godot;

namespace GlassesBar;

public static class StationHandlerIds
{
    public const string Customer = "customer";
    public const string IngredientSource = "ingredient_source";
    public const string HandWash = "hand_wash";
    public const string Kettle = "kettle";
    public const string WasteBin = "waste_bin";
}

/// <summary>
/// Data-only definition for one station type. Runtime rules are selected by HandlerId.
/// </summary>
[GlobalClass]
public partial class StationDefinition : Resource
{
    [Export] public StringName Id { get; set; } = new();
    [Export] public StationKind Kind { get; set; }
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public StringName HandlerId { get; set; } = new();
    [Export] public StringName IngredientId { get; set; } = new();
    [Export] public double IngredientAmount { get; set; }
    [Export(PropertyHint.MultilineText)] public string PromptTemplate { get; set; } = string.Empty;
    [Export] public bool HideUnavailablePromptInGlassesWorld { get; set; }
    [Export] public float InteractionDistance { get; set; }
}
