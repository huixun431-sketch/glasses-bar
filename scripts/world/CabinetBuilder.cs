using Godot;

namespace GlassesBar;

/// <summary>
/// Assembles cabinet interaction nodes from layout data. Cabinet state remains on
/// CabinetInteractable; this builder owns no gameplay or presentation state.
/// </summary>
public sealed class CabinetBuilder
{
    private readonly BarLayoutDefinition _layout;
    private readonly Node3D _neutral;

    public CabinetBuilder(BarLayoutDefinition layout, Node3D neutral)
    {
        _layout = layout;
        _neutral = neutral;
    }

    public void Build()
    {
        foreach (var layout in _layout.Cabinets)
        {
            if (layout.Cavity is { } cavity)
                GrayboxArchitectureBuilder.CreateBox(
                    _neutral,
                    cavity,
                    new Color("171112"));

            var cabinet = new CabinetInteractable();
            cabinet.Configure(
                layout.Id,
                layout.Kind,
                layout.Center,
                layout.Size,
                layout.HingeOnLeft,
                layout.OutwardDirection,
                layout.StorageDepth);
            _neutral.AddChild(cabinet);

            if (layout.ContainsIceBucket)
                AddIceBucket(cabinet);
        }
    }

    public void ResetAll()
    {
        foreach (var node in _neutral.GetTree().GetNodesInGroup("cabinet_storage"))
            if (node is CabinetInteractable cabinet)
                cabinet.ResetClosed();
    }

    private void AddIceBucket(CabinetInteractable drawer)
    {
        drawer.SetContentsDescription("内置冰桶");
        var station = GameplaySceneComposer.CreateGameplayStation(
            drawer,
            _layout.IceBucket.Id,
            _layout.IceBucket.Kind,
            _layout.IceBucket.Position,
            _layout.IceBucket.Size);
        var visual = new MeshInstance3D
        {
            Name = "Visual",
            Mesh = new CylinderMesh
            {
                TopRadius = 0.26f,
                BottomRadius = 0.23f,
                Height = 0.24f
            },
            MaterialOverride = GrayboxArchitectureBuilder.MakeMaterial(new Color("8da4b8"))
        };
        station.AddChild(visual);
        GameSession.Instance.WorldModeChanged +=
            mode => visual.Visible = (WorldMode)mode == WorldMode.Reality;
    }
}
