using Godot;

namespace GlassesBar.Tests;

public partial class ConsoleVisualCapture : Node
{
    public override void _Ready() => CallDeferred(MethodName.OpenConsole);

    private void OpenConsole()
    {
        var console = GetNode<DeveloperConsole>("Main/DeveloperConsole");
        console._Input(new InputEventKey { PhysicalKeycode = Key.Quoteleft, Pressed = true });
        GetNode<LineEdit>("Main/DeveloperConsole/Panel/Margin/Stack/Input").Text = "myopia 50";
    }
}
