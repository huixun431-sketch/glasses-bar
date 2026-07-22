using System;
using System.Collections.Generic;
using Godot;

namespace GlassesBar;

public partial class OpeningMenuController : CanvasLayer
{
    [Signal] public delegate void StartRequestedEventHandler();
    [Signal] public delegate void QuitRequestedEventHandler();

    private const int NormalFontSize = 36;
    private const int ActiveFontSize = 54;
    private static readonly Color NormalColor = new("c2b3a6e8");
    private static readonly Color ActiveColor = new("ffdb89");
    private static readonly Color DisabledColor = new("918783b8");

    private readonly List<Button> _menuButtons = new();
    private TextureRect _selector = null!;
    private Button _continue = null!;
    private Button _start = null!;
    private Button _settings = null!;
    private Button _credits = null!;
    private Button _quit = null!;
    private Control _mainPanel = null!;
    private Control _settingsPanel = null!;
    private Control _creditsPanel = null!;
    private HSlider _masterVolume = null!;
    private HSlider _mouseSensitivity = null!;
    private Label _volumeValue = null!;
    private Label _sensitivityValue = null!;
    private PlayerController _player = null!;
    private Button? _hoveredButton;
    private Button? _pageReturnFocus;
    private bool _keyboardNavigation;

    public bool IsSelectorVisible => _selector.Visible;
    public bool IsKeyboardNavigation => _keyboardNavigation;

    public override void _Ready()
    {
        _mainPanel = GetNode<Control>("Backdrop/MenuPanel");
        _settingsPanel = GetNode<Control>("Backdrop/SettingsPanel");
        _creditsPanel = GetNode<Control>("Backdrop/CreditsPanel");
        _selector = GetNode<TextureRect>("Backdrop/Selector");
        _continue = GetNode<Button>("Backdrop/MenuPanel/Margin/Stack/Continue");
        _start = GetNode<Button>("Backdrop/MenuPanel/Margin/Stack/Start");
        _settings = GetNode<Button>("Backdrop/MenuPanel/Margin/Stack/Settings");
        _credits = GetNode<Button>("Backdrop/MenuPanel/Margin/Stack/Credits");
        _quit = GetNode<Button>("Backdrop/MenuPanel/Margin/Stack/Quit");
        _masterVolume = GetNode<HSlider>("Backdrop/SettingsPanel/Margin/Stack/VolumeRow/MasterVolume");
        _mouseSensitivity = GetNode<HSlider>("Backdrop/SettingsPanel/Margin/Stack/SensitivityRow/MouseSensitivity");
        _volumeValue = GetNode<Label>("Backdrop/SettingsPanel/Margin/Stack/VolumeValue");
        _sensitivityValue = GetNode<Label>("Backdrop/SettingsPanel/Margin/Stack/SensitivityValue");
        _player = GetNode<PlayerController>("../Player");

        _menuButtons.AddRange(new[] { _continue, _start, _settings, _credits, _quit });
        BindMenuButtons();
        _start.Pressed += () => EmitSignal(SignalName.StartRequested);
        _settings.Pressed += () => ShowPanel(_settingsPanel);
        _credits.Pressed += () => ShowPanel(_creditsPanel);
        _quit.Pressed += () => EmitSignal(SignalName.QuitRequested);
        GetNode<Button>("Backdrop/SettingsPanel/Margin/Stack/Back").Pressed += ShowMainPanel;
        GetNode<Button>("Backdrop/CreditsPanel/Margin/Stack/Back").Pressed += ShowMainPanel;
        _masterVolume.ValueChanged += ApplyVolume;
        _mouseSensitivity.ValueChanged += ApplyMouseSensitivity;
        _masterVolume.Value = Mathf.DbToLinear(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Master"))) * 100d;
        _mouseSensitivity.Value = _player.MouseSensitivity * 1000d;
        _selector.Visible = false;
        GameSession.Instance.GameStartedChanged += OnGameStartedChanged;
        OnGameStartedChanged(GameSession.Instance.GameStarted);
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible)
            return;

        if (!_mainPanel.Visible && @event.IsActionPressed("ui_cancel"))
        {
            ShowMainPanel();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventMouseMotion)
        {
            if (_keyboardNavigation)
            {
                _keyboardNavigation = false;
                RefreshVisualState();
            }
            return;
        }

        if (!IsKeyboardNavigationEvent(@event) || !_mainPanel.Visible)
            return;
        _keyboardNavigation = true;
        if (GetViewport().GuiGetFocusOwner() is not Button focus || !_menuButtons.Contains(focus) || focus.Disabled)
            _start.GrabFocus();
        CallDeferred(nameof(RefreshVisualState));
    }

    public void ActivateKeyboardNavigationForTests(Button? button = null)
    {
        _keyboardNavigation = true;
        (button ?? _start).GrabFocus();
        RefreshVisualState();
    }

    public void ActivateMouseNavigationForTests(Button? button = null)
    {
        _keyboardNavigation = false;
        _hoveredButton = button;
        RefreshVisualState();
    }

    private void BindMenuButtons()
    {
        foreach (var button in _menuButtons)
        {
            button.MouseEntered += () => OnButtonMouseEntered(button);
            button.MouseExited += () => OnButtonMouseExited(button);
            button.FocusEntered += OnButtonFocusEntered;
        }

        var enabled = new[] { _start, _settings, _credits, _quit };
        for (var index = 0; index < enabled.Length; index++)
        {
            enabled[index].FocusNeighborTop = enabled[index].GetPathTo(enabled[(index - 1 + enabled.Length) % enabled.Length]);
            enabled[index].FocusNeighborBottom = enabled[index].GetPathTo(enabled[(index + 1) % enabled.Length]);
        }
    }

    private void OnGameStartedChanged(bool started)
    {
        Visible = !started;
        if (started)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            return;
        }

        Input.MouseMode = Input.MouseModeEnum.Visible;
        ShowMainPanel();
        CallDeferred(nameof(SetInitialFocus));
    }

    private void SetInitialFocus()
    {
        _start.GrabFocus();
        _keyboardNavigation = false;
        _hoveredButton = null;
        RefreshVisualState();
    }

    private void ShowPanel(Control panel)
    {
        _pageReturnFocus = ActiveButton();
        _mainPanel.Visible = false;
        _settingsPanel.Visible = panel == _settingsPanel;
        _creditsPanel.Visible = panel == _creditsPanel;
        _selector.Visible = false;
    }

    private void ShowMainPanel()
    {
        _mainPanel.Visible = true;
        _settingsPanel.Visible = false;
        _creditsPanel.Visible = false;
        if (!Visible)
            return;
        (_pageReturnFocus ?? _start).GrabFocus();
        RefreshVisualState();
    }

    private void OnButtonMouseEntered(Button button)
    {
        if (button.Disabled)
            return;
        _keyboardNavigation = false;
        _hoveredButton = button;
        RefreshVisualState();
    }

    private void OnButtonMouseExited(Button button)
    {
        if (_hoveredButton != button)
            return;
        _hoveredButton = null;
        RefreshVisualState();
    }

    private void OnButtonFocusEntered()
    {
        if (_keyboardNavigation)
            RefreshVisualState();
    }

    private Button? ActiveButton()
    {
        if (!_mainPanel.Visible)
            return null;
        if (!_keyboardNavigation)
            return _hoveredButton;
        return GetViewport().GuiGetFocusOwner() is Button focus && _menuButtons.Contains(focus) && !focus.Disabled
            ? focus
            : null;
    }

    private void RefreshVisualState()
    {
        var active = ActiveButton();
        foreach (var button in _menuButtons)
        {
            var isActive = button == active && !button.Disabled;
            var color = button.Disabled ? DisabledColor : isActive ? ActiveColor : NormalColor;
            button.AddThemeFontSizeOverride("font_size", isActive ? ActiveFontSize : NormalFontSize);
            button.AddThemeColorOverride("font_color", color);
            button.AddThemeColorOverride("font_hover_color", color);
            button.AddThemeColorOverride("font_focus_color", color);
            button.AddThemeColorOverride("font_pressed_color", color);
            button.AddThemeColorOverride("font_disabled_color", color);
            button.AddThemeConstantOverride("outline_size", isActive ? 2 : 0);
        }

        _selector.Visible = active is not null;
        if (active is not null)
            _selector.Position = new Vector2(53f, active.Position.Y + 20f);
    }

    private static bool IsKeyboardNavigationEvent(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false })
            return @event.IsAction("ui_up") || @event.IsAction("ui_down") || @event.IsAction("ui_left") ||
                   @event.IsAction("ui_right") || @event.IsAction("ui_accept");
        if (@event is InputEventJoypadButton { Pressed: true })
            return true;
        return @event is InputEventJoypadMotion motion && Math.Abs(motion.AxisValue) > 0.45f;
    }

    private void ApplyVolume(double value)
    {
        var linear = Mathf.Clamp((float)value / 100f, 0.001f, 1f);
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), Mathf.LinearToDb(linear));
        _volumeValue.Text = $"{value:0}%";
    }

    private void ApplyMouseSensitivity(double value)
    {
        _player.MouseSensitivity = Mathf.Clamp((float)value / 1000f, 0.001f, 0.006f);
        _sensitivityValue.Text = $"{value:0.0}";
    }
}
