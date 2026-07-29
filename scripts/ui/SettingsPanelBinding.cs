using System;
using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

/// <summary>
/// Presentation-only binding shared by the opening and pause settings panels.
/// </summary>
internal sealed class SettingsPanelBinding : IDisposable
{
    private readonly SettingsService _service;
    private readonly HSlider _masterVolume;
    private readonly HSlider _mouseSensitivity;
    private readonly Label _volumeValue;
    private readonly Label _sensitivityValue;

    public SettingsPanelBinding(
        SettingsService service,
        HSlider masterVolume,
        HSlider mouseSensitivity,
        Label volumeValue,
        Label sensitivityValue)
    {
        _service = service;
        _masterVolume = masterVolume;
        _mouseSensitivity = mouseSensitivity;
        _volumeValue = volumeValue;
        _sensitivityValue = sensitivityValue;

        _masterVolume.ValueChanged += OnMasterVolumeChanged;
        _mouseSensitivity.ValueChanged += OnMouseSensitivityChanged;
        _service.Changed += Synchronize;
        Synchronize(_service.State);
    }

    public void Dispose()
    {
        _masterVolume.ValueChanged -= OnMasterVolumeChanged;
        _mouseSensitivity.ValueChanged -= OnMouseSensitivityChanged;
        _service.Changed -= Synchronize;
    }

    private void OnMasterVolumeChanged(double value) =>
        _service.SetMasterVolumePercent(value);

    private void OnMouseSensitivityChanged(double value) =>
        _service.SetMouseSensitivitySliderValue(value);

    private void Synchronize(SettingsState state)
    {
        _masterVolume.SetValueNoSignal(state.MasterVolumePercent);
        _mouseSensitivity.SetValueNoSignal(state.MouseSensitivitySliderValue);
        _volumeValue.Text = $"{state.MasterVolumePercent:0}%";
        _sensitivityValue.Text = $"{state.MouseSensitivitySliderValue:0.0}";
    }
}
