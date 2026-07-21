using Godot;

namespace GlassesBar;

public partial class CounterSurfaceInteractable : StaticBody3D, IInteractable
{
    private DrinkWorkstation _workstation = null!;
    private float _placementHeight;

    public void Configure(DrinkWorkstation workstation, string id, Vector3 position, Vector3 size)
    {
        _workstation = workstation;
        Name = id;
        Position = position;
        _placementHeight = position.Y + size.Y * 0.5f + 0.18f;
        CollisionLayer = 1;
        AddToGroup("interactable");
        AddToGroup("placement_surface");
        AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
    }

    public string GetPrompt(InteractionContext context)
    {
        var tool = _workstation.CounterPlacementDisplayName;
        return $"[E] 将{tool}放在瞄准的吧台空位";
    }

    public string GetUnavailablePrompt(InteractionContext context)
    {
        if (GameSession.Instance.WorldMode == WorldMode.Glasses)
            return _workstation.HasHeldTool ? "[G] 摘下眼镜后摆放手中工具" : string.Empty;
        if (!_workstation.HasHeldTool)
            return string.Empty;
        _workstation.CanPlaceHeldToolAtPosition(PlacementPoint(context), out var reason);
        return reason;
    }

    public bool CanInteract(InteractionContext context) =>
        GameSession.Instance.CanCraft && GameSession.Instance.WorldMode == WorldMode.Reality &&
        _workstation.CanPlaceHeldToolAtPosition(PlacementPoint(context), out _);

    public void Interact(InteractionContext context)
    {
        var point = PlacementPoint(context);
        if (!_workstation.TryPlaceHeldToolAtPosition(point, out var feedback))
            GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, feedback);
    }

    private Vector3 PlacementPoint(InteractionContext context) =>
        new(context.InteractionPoint.X, _placementHeight, context.InteractionPoint.Z);
}
