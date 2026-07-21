using Godot;

namespace GlassesBar.Tests;

public partial class ToolHandsVisualCapture : Node
{
    public override void _Ready() => CallDeferred(MethodName.PrepareCapture);

    private void PrepareCapture()
    {
        var main = GetNode<Node3D>("Main");
        main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Start").EmitSignal(Button.SignalName.Pressed);
        GameSession.Instance.AcceptOrder();
        var player = main.GetNode<PlayerController>("Player");
        var workstation = main.GetNode<DrinkWorkstation>("NeutralGameplay/DrinkWorkstation");
        var context = new InteractionContext { Player = player, Workstation = workstation };
        main.GetNode<ToolInteractable>("NeutralGameplay/mortar").Interact(context);
        main.GetNode<ToolInteractable>("NeutralGameplay/pestle").Interact(context);
    }
}
