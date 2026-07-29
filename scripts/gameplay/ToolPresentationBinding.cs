using Godot;

namespace GlassesBar;

/// <summary>
/// Presentation binding for a tool instance. Authoritative gameplay data lives in
/// Domain.ToolInstanceState; this binding may be rebuilt without changing that state.
/// </summary>
public sealed class ToolPresentationBinding
{
    public required ToolInteractable Node { get; init; }
}
