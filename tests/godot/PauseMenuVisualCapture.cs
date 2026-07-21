using Godot;

namespace GlassesBar.Tests;

public partial class PauseMenuVisualCapture : Node
{
    public override void _Ready() => CallDeferred(MethodName.PrepareCapture);

    private void PrepareCapture()
    {
        var main = GetNode<Node3D>("Main");
        main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Start").EmitSignal(Button.SignalName.Pressed);
        main.GetNode<PauseMenuController>("PauseMenu").Pause();
    }
}
