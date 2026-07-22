using Godot;

namespace GlassesBar.Tests;

public partial class SideManualVisualCapture : Node
{
    public override void _Ready() => CallDeferred(MethodName.PrepareCapture);

    private void PrepareCapture()
    {
        var main = GetNode<Node3D>("Main");
        main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Start").EmitSignal(Button.SignalName.Pressed);
        main.GetNode<HudController>("HUD").Visible = false;
        GameSession.Instance.ToggleWorld();

        var camera = new Camera3D { Name = "SideManualReviewCamera", Current = true, Fov = 70f };
        main.AddChild(camera);
        camera.LookAtFromPosition(new Vector3(-3.55f, 2.02f, -1.82f), new Vector3(-5.18f, 1.55f, -1.02f), Vector3.Up);
    }
}
