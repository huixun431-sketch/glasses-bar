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
        player.GlobalPosition = new Vector3(-1.8f, 1.045f, -1.1f);
        player.Rotation = Vector3.Zero;
        player.GetNode<Node3D>("Head").Rotation = new Vector3(0.52f, 0f, 0f);
        var workstation = main.GetNode<DrinkWorkstation>("NeutralGameplay/DrinkWorkstation");
        var context = new InteractionContext { Player = player, Workstation = workstation };
        main.GetNode<CabinetInteractable>("NeutralGameplay/back_cabinet_3_left").Interact(context);
    }
}
