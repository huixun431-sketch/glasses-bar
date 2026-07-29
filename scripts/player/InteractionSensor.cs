using Godot;

namespace GlassesBar;

internal readonly record struct FocusedInteraction(IInteractable Interactable, Vector3 Point);

/// <summary>
/// Resolves the current interaction target without evaluating or executing gameplay rules.
/// </summary>
internal sealed class InteractionSensor
{
    private readonly RayCast3D _ray;
    private readonly ShapeCast3D _probe;

    public InteractionSensor(RayCast3D ray, ShapeCast3D probe)
    {
        _ray = ray;
        _probe = probe;
    }

    public FocusedInteraction? GetFocusedInteraction()
    {
        if (_ray.IsColliding() && _ray.GetCollider() is IInteractable direct)
            return new FocusedInteraction(direct, _ray.GetCollisionPoint());

        _probe.ForceShapecastUpdate();
        for (var index = 0; index < _probe.GetCollisionCount(); index++)
        {
            if (_probe.GetCollider(index) is IInteractable nearby)
                return new FocusedInteraction(nearby, _probe.GetCollisionPoint(index));
        }

        return null;
    }
}
