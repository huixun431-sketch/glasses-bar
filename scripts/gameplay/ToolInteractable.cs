using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

public partial class ToolInteractable : StaticBody3D, IInteractable
{
    private DrinkWorkstation _workstation = null!;
    private CollisionShape3D _collision = null!;
    private MeshInstance3D _visual = null!;
    private Label3D _label = null!;

    public string ToolId { get; private set; } = string.Empty;
    public ToolSpec Spec { get; private set; } = null!;

    public void Configure(DrinkWorkstation workstation, ToolSpec spec, Mesh mesh, Color color)
    {
        _workstation = workstation;
        Spec = spec;
        ToolId = spec.Id;
        Name = spec.Id;
        AddToGroup("interactable");
        AddToGroup("movable_tool");

        _collision = new CollisionShape3D
        {
            Name = "CollisionShape3D",
            Shape = new SphereShape3D { Radius = (float)Mathf.Max(0.11f, (float)spec.FootprintRadius) }
        };
        AddChild(_collision);
        _visual = new MeshInstance3D
        {
            Name = "Visual",
            Mesh = mesh,
            MaterialOverride = MakeMaterial(color)
        };
        AddChild(_visual);
        _label = new Label3D
        {
            Name = "GlassesLabel",
            Text = spec.DisplayName,
            Position = new Vector3(0f, 0.34f, 0f),
            FontSize = 24,
            OutlineSize = 7,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            Visible = GameSession.Instance.WorldMode == WorldMode.Glasses
        };
        AddChild(_label);
        GameSession.Instance.WorldModeChanged += mode =>
        {
            _label.Visible = (WorldMode)mode == WorldMode.Glasses && Visible;
            _visual.MaterialOverride = MakeMaterial((WorldMode)mode == WorldMode.Glasses ? new Color("2dd4bf") : color);
        };
    }

    public string GetPrompt(InteractionContext context) =>
        Spec.ResolveCategory() == ToolCategory.Placement
            ? $"[E] 将{Spec.DisplayName}拿到左手"
            : $"[E] 将{Spec.DisplayName}拿到右手";

    public string GetUnavailablePrompt(InteractionContext context)
    {
        if (!GameSession.Instance.GameStarted)
            return string.Empty;
        if (GameSession.Instance.WorldMode == WorldMode.Glasses)
            return $"[G] 摘下眼镜后拿取 · {Spec.DisplayName}";
        return Spec.ResolveCategory() == ToolCategory.Placement
            ? $"左手已持有{context.Workstation.LeftHandDisplayName}，先放下再拿{Spec.DisplayName}"
            : $"右手已持有{context.Workstation.RightHandDisplayName}，先放下再拿{Spec.DisplayName}";
    }

    public bool CanInteract(InteractionContext context) =>
        GameSession.Instance.CanCraft && GameSession.Instance.WorldMode == WorldMode.Reality &&
        context.Workstation.CanPickUpTool(ToolId);

    public void Interact(InteractionContext context)
    {
        if (!CanInteract(context))
        {
            GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, GetUnavailablePrompt(context));
            return;
        }
        context.Workstation.TryPickUpTool(ToolId);
    }

    public void ApplyWorldState(Vector3 position, bool visible)
    {
        GlobalPosition = position;
        Visible = visible;
        _visual.Visible = visible;
        _label.Visible = visible && GameSession.Instance.WorldMode == WorldMode.Glasses;
        _collision.SetDeferred(CollisionShape3D.PropertyName.Disabled, !visible);
    }

    private static StandardMaterial3D MakeMaterial(Color color) => new()
    {
        AlbedoColor = color,
        Roughness = 0.68f,
        Metallic = 0.08f,
        EmissionEnabled = color.G > 0.7f,
        Emission = color * 0.25f
    };
}
