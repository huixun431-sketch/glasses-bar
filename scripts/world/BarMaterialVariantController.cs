using System;
using Godot;

namespace GlassesBar;

/// <summary>
/// Owns production-only presentation changes that follow world mode without owning
/// gameplay state. Task 12 extends this boundary with material variants.
/// </summary>
public partial class BarMaterialVariantController : Node
{
    private Node3D? _wearOverlays;

    public void Configure(Node3D wearOverlays)
    {
        if (IsInsideTree())
            throw new InvalidOperationException("Configure material variants before adding the controller to the tree.");
        _wearOverlays = wearOverlays ?? throw new ArgumentNullException(nameof(wearOverlays));
    }

    public override void _Ready()
    {
        if (_wearOverlays is null)
            throw new InvalidOperationException("Production wear overlays were not configured.");
        GameSession.Instance.WorldModeChanged += OnWorldModeChanged;
        ApplyMode(GameSession.Instance.WorldMode);
    }

    public override void _ExitTree()
    {
        if (GameSession.Instance is not null)
            GameSession.Instance.WorldModeChanged -= OnWorldModeChanged;
    }

    public void ApplyMode(WorldMode mode)
    {
        if (_wearOverlays is not null)
            _wearOverlays.Visible = mode == WorldMode.Glasses;
    }

    private void OnWorldModeChanged(int mode) => ApplyMode((WorldMode)mode);
}
