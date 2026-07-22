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
        main.GetNode<CabinetInteractable>("NeutralGameplay/front_drawer_2_upper").SetOpen(true, true);

        var camera = new Camera3D { Name = "LayoutReviewCamera", Current = true, Fov = 72f };
        main.AddChild(camera);
        camera.LookAtFromPosition(new Vector3(6.25f, 3.55f, 5.45f), new Vector3(0f, 1.55f, -1.05f), Vector3.Up);
    }
}
