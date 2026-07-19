using Godot;

namespace GlassesBar;

public partial class WorldLayerController : Node, IWorldPresenter
{
    private Node3D _reality = null!;
    private Node3D _glasses = null!;
    private CanvasItem _realityBlur = null!;
    private CanvasItem _glassesOverlay = null!;

    public override void _Ready()
    {
        _reality = GetNode<Node3D>("../RealityWorld");
        _glasses = GetNode<Node3D>("../GlassesWorld");
        _realityBlur = GetNode<CanvasItem>("../RealityEffects/RealityBlur");
        _glassesOverlay = GetNode<CanvasItem>("../GlassesInfo/GlassesOverlay");
        GameSession.Instance.WorldModeChanged += mode => SetWorldMode((WorldMode)mode);
        SetWorldMode(GameSession.Instance.WorldMode);
    }

    public void SetWorldMode(WorldMode mode)
    {
        _reality.Visible = mode == WorldMode.Reality;
        _glasses.Visible = mode == WorldMode.Glasses;
        _realityBlur.Visible = mode == WorldMode.Reality;
        _glassesOverlay.Visible = mode == WorldMode.Glasses;
    }

    public bool HasEntity(string entityId) =>
        _reality.HasNode(entityId) || _glasses.HasNode(entityId);
}

