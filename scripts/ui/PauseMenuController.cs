using Godot;

namespace GlassesBar;

public partial class PauseMenuController : CanvasLayer
{
    [Signal] public delegate void RestartDayRequestedEventHandler();
    [Signal] public delegate void ReturnToMainMenuRequestedEventHandler();

    private Control _backdrop = null!;
    private Control _pausePanel = null!;
    private Control _settingsPanel = null!;
    private HSlider _masterVolume = null!;
    private HSlider _mouseSensitivity = null!;
    private Label _volumeValue = null!;
    private Label _sensitivityValue = null!;
    private PlayerController _player = null!;

    public bool IsOpen => _backdrop.Visible;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _backdrop = GetNode<Control>("Backdrop");
        _pausePanel = GetNode<Control>("Backdrop/PausePanel");
        _settingsPanel = GetNode<Control>("Backdrop/SettingsPanel");
        _masterVolume = GetNode<HSlider>("Backdrop/SettingsPanel/Margin/Stack/VolumeRow/MasterVolume");
        _mouseSensitivity = GetNode<HSlider>("Backdrop/SettingsPanel/Margin/Stack/SensitivityRow/MouseSensitivity");
        _volumeValue = GetNode<Label>("Backdrop/SettingsPanel/Margin/Stack/VolumeValue");
        _sensitivityValue = GetNode<Label>("Backdrop/SettingsPanel/Margin/Stack/SensitivityValue");
        _player = GetNode<PlayerController>("../Player");

        GetNode<Button>("Backdrop/PausePanel/Margin/Stack/Continue").Pressed += Resume;
        GetNode<Button>("Backdrop/PausePanel/Margin/Stack/RestartDay").Pressed += RestartDay;
        GetNode<Button>("Backdrop/PausePanel/Margin/Stack/Settings").Pressed += ShowSettings;
        GetNode<Button>("Backdrop/PausePanel/Margin/Stack/ReturnMain").Pressed += ReturnToMain;
        GetNode<Button>("Backdrop/SettingsPanel/Margin/Stack/Back").Pressed += ShowPausePanel;
        _masterVolume.ValueChanged += ApplyVolume;
        _mouseSensitivity.ValueChanged += ApplyMouseSensitivity;
        _masterVolume.Value = Mathf.DbToLinear(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Master"))) * 100d;
        _mouseSensitivity.Value = _player.MouseSensitivity * 1000d;
        _backdrop.Visible = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (!@event.IsActionPressed("pause_game") || !GameSession.Instance.GameStarted || DeveloperConsole.IsOpen)
            return;
        if (_settingsPanel.Visible)
            ShowPausePanel();
        else if (IsOpen)
            Resume();
        else
            Pause();
        GetViewport().SetInputAsHandled();
    }

    public void Pause()
    {
        _backdrop.Visible = true;
        ShowPausePanel();
        GetTree().Paused = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetNode<Button>("Backdrop/PausePanel/Margin/Stack/Continue").GrabFocus();
    }

    public void Resume()
    {
        _backdrop.Visible = false;
        _settingsPanel.Visible = false;
        _pausePanel.Visible = true;
        GetTree().Paused = false;
        if (GameSession.Instance.CanMove)
            Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private void RestartDay()
    {
        Resume();
        EmitSignal(SignalName.RestartDayRequested);
    }

    private void ReturnToMain()
    {
        Resume();
        EmitSignal(SignalName.ReturnToMainMenuRequested);
    }

    private void ShowSettings()
    {
        _pausePanel.Visible = false;
        _settingsPanel.Visible = true;
        _masterVolume.GrabFocus();
    }

    private void ShowPausePanel()
    {
        _pausePanel.Visible = true;
        _settingsPanel.Visible = false;
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
