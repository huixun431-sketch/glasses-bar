using System;
using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

/// <summary>
/// The single runtime owner that applies normalized settings to engine and player adapters.
/// </summary>
public partial class SettingsService : Node
{
    private PlayerController _player = null!;
    private int _masterBusIndex;

    public event Action<SettingsState>? Changed;

    public SettingsState State { get; private set; } = SettingsState.Create(100d, 0.0022d);

    public override void _Ready()
    {
        _player = GetNode<PlayerController>("../Player");
        _masterBusIndex = AudioServer.GetBusIndex("Master");
        State = SettingsState.Create(
            Mathf.DbToLinear(AudioServer.GetBusVolumeDb(_masterBusIndex)) * 100d,
            _player.MouseSensitivity);
        ApplyRuntimeState();
    }

    public void SetMasterVolumePercent(double value) =>
        Update(State.WithMasterVolumePercent(value));

    public void SetMouseSensitivitySliderValue(double value) =>
        Update(State.WithMouseSensitivity(value / 1000d));

    private void Update(SettingsState next)
    {
        if (next == State)
            return;

        State = next;
        ApplyRuntimeState();
        Changed?.Invoke(State);
    }

    private void ApplyRuntimeState()
    {
        var linear = Mathf.Clamp(
            (float)State.MasterVolumePercent / 100f,
            0.001f,
            1f);
        AudioServer.SetBusVolumeDb(_masterBusIndex, Mathf.LinearToDb(linear));
        _player.MouseSensitivity = (float)State.MouseSensitivity;
    }
}
