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
        main.GetNode<CabinetInteractable>("NeutralGameplay/front_drawer_4_lower").SetOpen(true, true);
        main.GetNode<CabinetInteractable>("NeutralGameplay/back_cabinet_3_left").SetOpen(true, true);

        var camera = new Camera3D { Name = "LayoutReviewCamera", Current = true, Fov = 68f };
        main.AddChild(camera);
        camera.LookAtFromPosition(new Vector3(6.2f, 3.25f, 5.2f), new Vector3(0f, 0.95f, -0.65f), Vector3.Up);
    }
}
