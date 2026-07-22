using Godot;

namespace GlassesBar.Tests;

public partial class StorageVisualCapture : Node
{
    public override void _Ready() => CallDeferred(MethodName.PrepareCapture);

    private async void PrepareCapture()
    {
        var main = GetNode<Node3D>("Main");
        main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Start").EmitSignal(Button.SignalName.Pressed);
        main.GetNode<HudController>("HUD").Visible = false;
        main.GetNode<MyopiaEffectController>("MyopiaEffectController").SetMyopiaDegrees(0f, false);
        main.GetNode<CabinetInteractable>("NeutralGameplay/front_drawer_2_upper").SetOpen(true, true);
        main.GetNode<CabinetInteractable>("NeutralGameplay/front_drawer_4_lower").SetOpen(true, true);
        main.GetNode<CabinetInteractable>("NeutralGameplay/back_cabinet_3_left").SetOpen(true, true);

        var camera = new Camera3D { Name = "StorageReviewCamera", Current = true, Fov = 84f };
        main.AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0f, 2.8f, -1.22f), new Vector3(0f, 0.58f, -0.18f), Vector3.Up);
        await ToSignal(GetTree().CreateTimer(2.1d), SceneTreeTimer.SignalName.Timeout);
        main.GetNode<CabinetInteractable>("NeutralGameplay/sink_left_drawer_upper").SetOpen(true, true);
        camera.LookAtFromPosition(new Vector3(0f, 2.8f, -0.9f), new Vector3(0f, 0.68f, -2.24f), Vector3.Up);
    }
}
