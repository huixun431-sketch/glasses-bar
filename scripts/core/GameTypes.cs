using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

public sealed class IngredientAmount
{
    public StringName IngredientId { get; init; } = new();
    public double Amount { get; set; }
    public IngredientUnit Unit { get; init; } = IngredientUnit.PrototypeUnit;
}

public sealed class ContainerState
{
    public double Capacity { get; set; }
    public double CurrentAmount { get; set; }
    public double SpilledAmount { get; set; }
}

public sealed class OperationResult
{
    public bool Completed { get; init; }
    public double Intensity { get; init; }
    public double DurationSeconds { get; init; }
    public string Feedback { get; init; } = string.Empty;
}

public sealed class InteractionContext
{
    public required PlayerController Player { get; init; }
    public required DrinkWorkstation Workstation { get; init; }
}

