using Godot;

namespace GlassesBar;

public partial class OpeningMenuController : CanvasLayer
{
    [Signal] public delegate void StartRequestedEventHandler();
    [Signal] public delegate void QuitRequestedEventHandler();

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

    public override void _Ready()
    {
        _mainPanel = GetNode<Control>("Backdrop/MenuPanel");
        _settingsPanel = GetNode<Control>("Backdrop/SettingsPanel");
        _creditsPanel = GetNode<Control>("Backdrop/CreditsPanel");
        _start = GetNode<Button>("Backdrop/MenuPanel/Margin/Stack/Start");
        _settings = GetNode<Button>("Backdrop/MenuPanel/Margin/Stack/Settings");
        _credits = GetNode<Button>("Backdrop/MenuPanel/Margin/Stack/Credits");
        _quit = GetNode<Button>("Backdrop/MenuPanel/Margin/Stack/Quit");
        _masterVolume = GetNode<HSlider>("Backdrop/SettingsPanel/Margin/Stack/VolumeRow/MasterVolume");
        _mouseSensitivity = GetNode<HSlider>("Backdrop/SettingsPanel/Margin/Stack/SensitivityRow/MouseSensitivity");
        _volumeValue = GetNode<Label>("Backdrop/SettingsPanel/Margin/Stack/VolumeValue");
        _sensitivityValue = GetNode<Label>("Backdrop/SettingsPanel/Margin/Stack/SensitivityValue");
        _player = GetNode<PlayerController>("../Player");
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
        GameSession.Instance.GameStartedChanged += OnGameStartedChanged;
        OnGameStartedChanged(GameSession.Instance.GameStarted);
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
        _start.GrabFocus();
    }

    private void ShowPanel(Control panel)
    {
        _mainPanel.Visible = false;
        _settingsPanel.Visible = panel == _settingsPanel;
        _creditsPanel.Visible = panel == _creditsPanel;
    }

    private void ShowMainPanel()
    {
        _mainPanel.Visible = true;
        _settingsPanel.Visible = false;
        _creditsPanel.Visible = false;
        if (Visible)
            _start.GrabFocus();
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
