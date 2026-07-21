using Godot;

namespace GlassesBar.Tests;

public partial class CabinetVisualCapture : Node
{
    public override void _Ready() => CallDeferred(MethodName.PrepareCapture);

    private void PrepareCapture()
    {
        var main = GetNode<Node3D>("Main");
        main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Start").EmitSignal(Button.SignalName.Pressed);
        var player = main.GetNode<PlayerController>("Player");
        player.GlobalPosition = new Vector3(-3.75f, 0.9f, -2.75f);
        player.Rotation = new Vector3(0f, Mathf.Pi, 0f);
        player.GetNode<Node3D>("Head").Rotation = new Vector3(-0.34f, 0f, 0f);
        var workstation = main.GetNode<DrinkWorkstation>("NeutralGameplay/DrinkWorkstation");
        var context = new InteractionContext { Player = player, Workstation = workstation };
        main.GetNode<CabinetInteractable>("NeutralGameplay/front_drawer_1").Interact(context);
        main.GetNode<CabinetInteractable>("NeutralGameplay/front_cabinet_2_left").Interact(context);
    }
}
