using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

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
    public Vector3 InteractionPoint { get; init; }
}
