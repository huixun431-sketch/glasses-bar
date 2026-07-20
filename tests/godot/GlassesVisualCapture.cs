using Godot;

namespace GlassesBar.Tests;

public partial class GlassesVisualCapture : Node
{
    public override void _Ready() => CallDeferred(MethodName.ActivateGlasses);

    private void ActivateGlasses()
    {
        GetNode<Button>("Main/OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Start").EmitSignal(Button.SignalName.Pressed);
        GameSession.Instance.AcceptOrder();
        GameSession.Instance.ToggleWorld();
    }
}
