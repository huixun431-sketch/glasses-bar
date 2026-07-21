using Godot;

namespace GlassesBar.Tests;

public partial class FailureVisualCapture : Node
{
    public override void _Ready() => CallDeferred(MethodName.PrepareCapture);

    private void PrepareCapture()
    {
        var main = GetNode<Node3D>("Main");
        main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Start").EmitSignal(Button.SignalName.Pressed);
        GameSession.Instance.AcceptOrder();
        var player = main.GetNode<PlayerController>("Player");
        var workstation = main.GetNode<DrinkWorkstation>("NeutralGameplay/DrinkWorkstation");
        var board = main.GetNode<WorkboardInteractable>("NeutralGameplay/workboard");
        var context = new InteractionContext { Player = player, Workstation = workstation };

        main.GetNode<ToolInteractable>("NeutralGameplay/mortar").Interact(context);
        board.Interact(context);
        main.GetNode<ToolInteractable>("NeutralGameplay/bean_scoop").Interact(context);
        for (var index = 0; index < 4; index++)
            workstation.TryLoadIngredient("coffee_beans", 0.25d, out _);
        board.Interact(context);
        workstation.TryPlaceHeldToolAtPosition(new Vector3(-0.45f, 1.13f, -5.42f), out _);
        main.GetNode<ToolInteractable>("NeutralGameplay/ice_tongs").Interact(context);
        workstation.QueueAttemptRollForTests(0d);
        board.Begin(context);
        board.UpdateOperation(1d, 0.7d);
        board.Complete();
    }
}
