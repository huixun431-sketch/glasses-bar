using Godot;

namespace GlassesBar.Tests;

public partial class SettingsVisualCapture : Node
{
    [Export] public bool ShowPauseSettings { get; set; }

    public override void _Ready() => CallDeferred(MethodName.PrepareCapture);

    private void PrepareCapture()
    {
        var main = GetNode<Node3D>("Main");
        if (ShowPauseSettings)
        {
            main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Start")
                .EmitSignal(Button.SignalName.Pressed);
            main.GetNode<PauseMenuController>("PauseMenu").Pause();
            main.GetNode<Button>("PauseMenu/Backdrop/PausePanel/Margin/Stack/Settings")
                .EmitSignal(Button.SignalName.Pressed);
            return;
        }

        main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Settings")
            .EmitSignal(Button.SignalName.Pressed);
    }
}
