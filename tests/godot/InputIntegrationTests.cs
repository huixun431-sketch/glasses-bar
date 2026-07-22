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
            var menu = main.GetNode<OpeningMenuController>("OpeningMenu");
            var pauseMenu = main.GetNode<PauseMenuController>("PauseMenu");

            Require(menu.Visible && !GameSession.Instance.GameStarted, "opening menu is visible before play begins");
            Require(!GameSession.Instance.CanMove, "opening menu gates player movement");
            var startButton = main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Start");
            var settingsButton = main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Settings");
            var selector = main.GetNode<TextureRect>("OpeningMenu/Backdrop/Selector");
            Require(main.GetNode<TextureRect>("OpeningMenu/Backdrop/Background").Texture is not null &&
                    main.GetNode<TextureRect>("OpeningMenu/Backdrop/TitleArtwork").Texture is not null && selector.Texture is not null,
                "approved artwork is split into no-text background, independent title and movable selector layers");
            Require(GetViewport().GuiGetFocusOwner() == startButton && !menu.IsSelectorVisible,
                "start may own initial keyboard focus while mouse mode shows no permanent highlight");
            menu.ActivateKeyboardNavigationForTests(startButton);
            var startSelectorY = selector.Position.Y;
            Require(menu.IsSelectorVisible && startButton.GetThemeFontSize("font_size") > settingsButton.GetThemeFontSize("font_size"),
                "keyboard navigation reveals the focused live Godot menu control");
            menu.ActivateKeyboardNavigationForTests(settingsButton);
            Require(selector.Position.Y > startSelectorY && settingsButton.GetThemeFontSize("font_size") > startButton.GetThemeFontSize("font_size"),
                "selector and highlight move instead of staying fixed on start");
            menu.ActivateMouseNavigationForTests();
            Require(!menu.IsSelectorVisible && startButton.GetThemeFontSize("font_size") == settingsButton.GetThemeFontSize("font_size"),
                "mouse mode outside buttons removes every focus highlight");
            main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Settings").EmitSignal(Button.SignalName.Pressed);
            Require(main.GetNode<Control>("OpeningMenu/Backdrop/SettingsPanel").Visible,
                "main menu settings button opens an interactive settings panel");
            main.GetNode<Button>("OpeningMenu/Backdrop/SettingsPanel/Margin/Stack/Back").EmitSignal(Button.SignalName.Pressed);
            main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Start").EmitSignal(Button.SignalName.Pressed);
            Require(GameSession.Instance.GameStarted && !menu.Visible, "start button enters a new game and hides the menu");
            Require(GameSession.Instance.CanMove && main.GetNode<HudController>("HUD").Visible,
                "starting the game enables movement and gameplay HUD");
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            Require(Math.Abs(player.GlobalPosition.Z - (-1.2f)) < 0.01f, "player starts centered inside the two-person bartender aisle");
            Require(Math.Abs(player.GetNode<Node3D>("Head").GlobalPosition.Y - GrayboxLevelBuilder.PlayerEyeHeight) < 0.01f,
                "player starts with the coordinated two-metre eye height");
            player.GlobalPosition = new Vector3(4.8f, 1.045f, -1.2f);
            Input.ActionPress("move_left");
            for (var frame = 0; frame < 30; frame++)
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            Input.ActionRelease("move_left");
            Require(player.GlobalPosition.X < 4.95f,
                "side return collision encloses the bartender work area and prevents walking out");
            player.ResetForNewDay();
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            var workstation = main.GetNode<DrinkWorkstation>("NeutralGameplay/DrinkWorkstation");
            var glassPickup = main.GetNode<ToolInteractable>("NeutralGameplay/highball_glass");
            var context = new InteractionContext { Player = player, Workstation = workstation };
            Require(GameSession.Instance.Flow.Current == DayPhase.WaitingForOrder && glassPickup.CanInteract(context),
                "tools and crafting interactions are available before accepting an order");
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

            foreach (var action in new[] { "move_forward", "interact", "toggle_glasses", "operate", "use_held_tool", "toggle_jigger_side", "next_day", "pause_game" })
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
            Require(GameSession.Instance.Flow.Current == DayPhase.Preparation && !GameSession.Instance.RecipeObserved,
                "E accepts the order and immediately allows crafting without glasses");
            Require(main.GetNode<PanelContainer>("HUD/FeedbackPanel").Visible, "interaction produces immediate UI feedback");

            pauseMenu._Input(new InputEventAction { Action = "pause_game", Pressed = true, Strength = 1f });
            Require(pauseMenu.IsOpen && GetTree().Paused && main.GetNode<Control>("PauseMenu/Backdrop").Visible,
                "Escape opens the real in-game pause menu and pauses the scene tree");
            main.GetNode<Button>("PauseMenu/Backdrop/PausePanel/Margin/Stack/Settings").EmitSignal(Button.SignalName.Pressed);
            Require(main.GetNode<Control>("PauseMenu/Backdrop/SettingsPanel").Visible,
                "pause menu settings can be opened without leaving the day");
            var sensitivity = main.GetNode<HSlider>("PauseMenu/Backdrop/SettingsPanel/Margin/Stack/SensitivityRow/MouseSensitivity");
            sensitivity.Value = 3.4d;
            Require(Math.Abs(player.MouseSensitivity - 0.0034f) < 0.00001f,
                "pause settings modify runtime mouse sensitivity");
            main.GetNode<Button>("PauseMenu/Backdrop/SettingsPanel/Margin/Stack/Back").EmitSignal(Button.SignalName.Pressed);
            main.GetNode<Button>("PauseMenu/Backdrop/PausePanel/Margin/Stack/Continue").EmitSignal(Button.SignalName.Pressed);
            Require(!pauseMenu.IsOpen && !GetTree().Paused, "continue closes pause menu and resumes gameplay");
            pauseMenu.Pause();
            main.GetNode<Button>("PauseMenu/Backdrop/PausePanel/Margin/Stack/RestartDay").EmitSignal(Button.SignalName.Pressed);
            Require(!GetTree().Paused && GameSession.Instance.Flow.Current == DayPhase.WaitingForOrder &&
                    GameSession.Instance.CurrentDay == 1,
                "restart-day option resets the current day without advancing the campaign");
            GameSession.Instance.AcceptOrder();

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
            Require(GameSession.Instance.Flow.Current == DayPhase.Preparation, "returning to reality preserves preparation");
            Require(GameSession.Instance.CanMove, "movement gate is enabled in reality world");

            Require(glassPickup.CanInteract(context), "glass pickup is available during reality preparation");

            SendPlayerAction(player, "toggle_glasses", true);
            SendPlayerAction(player, "toggle_glasses", false);
            Require(GameSession.Instance.CanMove, "movement remains enabled when glasses are worn during preparation");
            Require(!glassPickup.CanInteract(context), "ingredient interaction remains blocked in glasses world");

            SendPlayerAction(player, "toggle_glasses", true);
            SendPlayerAction(player, "toggle_glasses", false);

            pauseMenu.Pause();
            main.GetNode<Button>("PauseMenu/Backdrop/PausePanel/Margin/Stack/ReturnMain").EmitSignal(Button.SignalName.Pressed);
            Require(!GameSession.Instance.GameStarted && menu.Visible && !GetTree().Paused,
                "return-to-main option leaves gameplay and restores the startup menu");

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
