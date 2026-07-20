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
            var probe = player.GetNode<ShapeCast3D>("Head/Camera3D/InteractionProbe");
            var myopia = main.GetNode<MyopiaEffectController>("MyopiaEffectController");
            var console = main.GetNode<DeveloperConsole>("DeveloperConsole");

            Require(player.GlobalPosition.Z < -1f, "player starts inside the bartender work area");
            Require(probe.Enabled && probe.TargetPosition.Length() > 5f, "forgiving interaction probe is active");
            Require(Math.Abs(myopia.MyopiaDegrees - 50f) < 0.01f, "reality myopia defaults to 50 degrees");
            var blurMaterial = (ShaderMaterial)main.GetNode<ColorRect>("RealityEffects/RealityBlur").Material;
            myopia.SetMyopiaDegrees(125f, false);
            Require(Math.Abs(myopia.MyopiaDegrees - 125f) < 0.01f, "myopia can be adjusted at runtime");
            Require((float)blurMaterial.GetShaderParameter("blur_radius") > 3f, "runtime myopia updates blur shader");
            myopia.SetMyopiaDegrees(50f, false);
            console._Input(new InputEventKey { PhysicalKeycode = Key.Quoteleft, Pressed = true });
            Require(DeveloperConsole.IsOpen && main.GetNode<Control>("DeveloperConsole/Panel").Visible,
                "built-in developer console opens with the quote-left key");
            var consoleInput = main.GetNode<LineEdit>("DeveloperConsole/Panel/Margin/Stack/Input");
            consoleInput.EmitSignal(LineEdit.SignalName.TextSubmitted, "myopia 200");
            Require(Math.Abs(myopia.MyopiaDegrees - 200f) < 0.01f, "console command adjusts myopia degrees");
            myopia.SetMyopiaDegrees(50f, false);
            console._Input(new InputEventKey { PhysicalKeycode = Key.Quoteleft, Pressed = true });
            Require(!DeveloperConsole.IsOpen, "built-in developer console closes with the quote-left key");

            foreach (var action in new[] { "move_forward", "interact", "toggle_glasses", "operate", "next_day" })
            {
                Require(InputMap.HasAction(action), $"input action exists: {action}");
                Require(InputMap.ActionGetEvents(action).Count > 0, $"input action has binding: {action}");
            }

            ray.ForceRaycastUpdate();
            Require(ray.IsColliding(), "interaction ray reaches customer");
            Require(ray.GetCollider() is StationInteractable { Kind: StationKind.Customer }, "ray targets customer");
            Require(main.GetNode<PanelContainer>("HUD/PromptPanel").Visible, "nearby interactable shows a prompt panel");

            SendPlayerAction(player, "interact", true);
            SendPlayerAction(player, "interact", false);
            Require(GameSession.Instance.Flow.Current == DayPhase.OrderReceived, "E action reaches PlayerController and accepts order");
            Require(main.GetNode<PanelContainer>("HUD/FeedbackPanel").Visible, "interaction produces immediate UI feedback");

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
