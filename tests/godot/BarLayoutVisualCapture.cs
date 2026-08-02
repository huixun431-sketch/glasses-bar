using Godot;

namespace GlassesBar.Tests;

public partial class BarLayoutVisualCapture : Node
{
    public override void _Ready() => CallDeferred(MethodName.PrepareCapture);

    private void PrepareCapture()
    {
        var main = GetNode<Node3D>("Main");
        main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Start").EmitSignal(Button.SignalName.Pressed);
        main.GetNode<HudController>("HUD").Visible = false;
        main.GetNode<MyopiaEffectController>("MyopiaEffectController").SetMyopiaDegrees(0f, false);
        main.GetNode<CabinetInteractable>("NeutralGameplay/front_drawer_2_upper").SetOpen(true, false);

        var camera = new Camera3D { Name = "LayoutReviewCamera", Current = true, Fov = 72f };
        main.AddChild(camera);
        camera.LookAtFromPosition(new Vector3(5.15f, 3.0f, 2.75f), new Vector3(-0.6f, 1.1f, -1.8f), Vector3.Up);
    }
}
