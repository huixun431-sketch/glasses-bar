using Godot;

namespace GlassesBar.Tests;

public partial class MainMenuKeyboardVisualCapture : Node
{
    public override void _Ready() => CallDeferred(MethodName.PrepareCapture);

    private void PrepareCapture()
    {
        var main = GetNode<Node3D>("Main");
        var menu = main.GetNode<OpeningMenuController>("OpeningMenu");
        var settings = main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Settings");
        menu.ActivateKeyboardNavigationForTests(settings);
    }
}
