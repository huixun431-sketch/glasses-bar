using System;
using Godot;
using GlassesBar.Domain;

namespace GlassesBar.Tests;

public partial class InputIntegrationTests : Node
{
    public override void _Ready() => CallDeferred(MethodName.RunDeferred);

    private async void RunDeferred()
    {
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            var main = GetNode<Node3D>("Main");
            var player = main.GetNode<PlayerController>("Player");
            var ray = player.GetNode<RayCast3D>("Head/Camera3D/InteractionRay");

            foreach (var action in new[] { "move_forward", "interact", "toggle_glasses", "operate" })
            {
                Require(InputMap.HasAction(action), $"input action exists: {action}");
                Require(InputMap.ActionGetEvents(action).Count > 0, $"input action has binding: {action}");
            }

            ray.ForceRaycastUpdate();
            Require(ray.IsColliding(), "interaction ray reaches customer");
            Require(ray.GetCollider() is StationInteractable { Kind: StationKind.Customer }, "ray targets customer");

            SendPlayerAction(player, "interact", true);
            SendPlayerAction(player, "interact", false);
            Require(GameSession.Instance.Flow.Current == DayPhase.OrderReceived, "E action reaches PlayerController and accepts order");

            SendPlayerAction(player, "toggle_glasses", true);
            SendPlayerAction(player, "toggle_glasses", false);
            Require(GameSession.Instance.WorldMode == WorldMode.Glasses, "G action enters glasses world");
            Require(GameSession.Instance.CanMove, "movement remains enabled in glasses world");

            var beforeMove = player.GlobalPosition;
            Input.ActionPress("move_right");
            for (var frame = 0; frame < 4; frame++)
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            Input.ActionRelease("move_right");
            var horizontalMove = new Vector2(
                player.GlobalPosition.X - beforeMove.X,
                player.GlobalPosition.Z - beforeMove.Z).Length();
            Require(horizontalMove > 0.05f, "movement input changes player position in glasses world");

            SendPlayerAction(player, "toggle_glasses", true);
            SendPlayerAction(player, "toggle_glasses", false);
            Require(GameSession.Instance.Flow.Current == DayPhase.Preparation, "returning to reality enters preparation");
            Require(GameSession.Instance.CanMove, "movement gate is enabled in reality world");

            var workstation = main.GetNode<DrinkWorkstation>("NeutralGameplay/DrinkWorkstation");
            var glassPickup = main.GetNode<StationInteractable>("NeutralGameplay/highball_glass");
            var context = new InteractionContext { Player = player, Workstation = workstation };
            Require(glassPickup.CanInteract(context), "glass pickup is available during reality preparation");

            SendPlayerAction(player, "toggle_glasses", true);
            SendPlayerAction(player, "toggle_glasses", false);
            Require(GameSession.Instance.CanMove, "movement remains enabled when glasses are worn during preparation");
            Require(!glassPickup.CanInteract(context), "ingredient interaction remains blocked in glasses world");

            SendPlayerAction(player, "toggle_glasses", true);
            SendPlayerAction(player, "toggle_glasses", false);

            GD.Print("INPUT_INTEGRATION_PASS");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private static void SendPlayerAction(PlayerController player, string action, bool pressed)
    {
        player._UnhandledInput(new InputEventAction { Action = action, Pressed = pressed, Strength = pressed ? 1f : 0f });
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
