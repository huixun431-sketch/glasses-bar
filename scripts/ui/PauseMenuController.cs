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
    private SettingsPanelBinding _settingsBinding = null!;

    public bool IsOpen => _backdrop.Visible;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _backdrop = GetNode<Control>("Backdrop");
        _pausePanel = GetNode<Control>("Backdrop/PausePanel");
        _settingsPanel = GetNode<Control>("Backdrop/SettingsPanel");
        _masterVolume = GetNode<HSlider>("Backdrop/SettingsPanel/Margin/Stack/VolumeRow/MasterVolume");
        _settingsBinding = new SettingsPanelBinding(
            GetNode<SettingsService>("../SettingsService"),
            _masterVolume,
            GetNode<HSlider>("Backdrop/SettingsPanel/Margin/Stack/SensitivityRow/MouseSensitivity"),
            GetNode<Label>("Backdrop/SettingsPanel/Margin/Stack/VolumeValue"),
            GetNode<Label>("Backdrop/SettingsPanel/Margin/Stack/SensitivityValue"));

        GetNode<Button>("Backdrop/PausePanel/Margin/Stack/Continue").Pressed += Resume;
        GetNode<Button>("Backdrop/PausePanel/Margin/Stack/RestartDay").Pressed += RestartDay;
        GetNode<Button>("Backdrop/PausePanel/Margin/Stack/Settings").Pressed += ShowSettings;
        GetNode<Button>("Backdrop/PausePanel/Margin/Stack/ReturnMain").Pressed += ReturnToMain;
        GetNode<Button>("Backdrop/SettingsPanel/Margin/Stack/Back").Pressed += ShowPausePanel;
        _backdrop.Visible = false;
    }

    public override void _ExitTree() => _settingsBinding.Dispose();

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

}
