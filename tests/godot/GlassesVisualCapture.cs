using Godot;

namespace GlassesBar.Tests;

public partial class GlassesVisualCapture : Node
{
    public override void _Ready() => CallDeferred(MethodName.ActivateGlasses);

    private void ActivateGlasses()
    {
        GameSession.Instance.AcceptOrder();
        GameSession.Instance.ToggleWorld();
    }
}

