using System;

namespace GlassesBar.Domain;

/// <summary>
/// Normalized application settings values without engine or UI dependencies.
/// </summary>
public sealed record SettingsState
{
    public const double MinimumMasterVolumePercent = 0d;
    public const double MaximumMasterVolumePercent = 100d;
    public const double MinimumMouseSensitivity = 0.001d;
    public const double MaximumMouseSensitivity = 0.006d;

    private SettingsState(double masterVolumePercent, double mouseSensitivity)
    {
        MasterVolumePercent = masterVolumePercent;
        MouseSensitivity = mouseSensitivity;
    }

    public double MasterVolumePercent { get; }
    public double MouseSensitivity { get; }
    public double MouseSensitivitySliderValue => MouseSensitivity * 1000d;

    public static SettingsState Create(double masterVolumePercent, double mouseSensitivity) =>
        new(
            Math.Clamp(
                masterVolumePercent,
                MinimumMasterVolumePercent,
                MaximumMasterVolumePercent),
            Math.Clamp(
                mouseSensitivity,
                MinimumMouseSensitivity,
                MaximumMouseSensitivity));

    public SettingsState WithMasterVolumePercent(double value) =>
        Create(value, MouseSensitivity);

    public SettingsState WithMouseSensitivity(double value) =>
        Create(MasterVolumePercent, value);
}
