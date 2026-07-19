using Godot;

namespace GlassesBar.Tests;

public partial class InteractionFeedbackVisualCapture : Node
{
    public override void _Ready() => CallDeferred(MethodName.TriggerInteraction);

    private void TriggerInteraction()
    {
        var player = GetNode<PlayerController>("Main/Player");
        player.GetNode<RayCast3D>("Head/Camera3D/InteractionRay").ForceRaycastUpdate();
        player._UnhandledInput(new InputEventAction { Action = "interact", Pressed = true, Strength = 1f });
    }
}
