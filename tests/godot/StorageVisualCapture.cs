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

        var camera = new Camera3D { Name = "StorageReviewCamera", Current = true, Fov = 76f };
        main.AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0f, 2.85f, -1.25f), new Vector3(-1.8f, 0.95f, -0.25f), Vector3.Up);
        await ToSignal(GetTree().CreateTimer(2.1d), SceneTreeTimer.SignalName.Timeout);
        main.GetNode<CabinetInteractable>("NeutralGameplay/back_cabinet_2_left").SetOpen(true, true);
        camera.LookAtFromPosition(new Vector3(0f, 2.3f, -0.85f), new Vector3(0f, 3.85f, -2.85f), Vector3.Up);
    }
}
