using Godot;

namespace GlassesBar;

public enum CabinetPartKind
{
    Door,
    Drawer
}

public partial class CabinetInteractable : StaticBody3D, IInteractable
{
    private CabinetPartKind _kind;
    private Vector3 _closedPosition;
    private Vector3 _openPosition;
    private float _openRotationY;
    private MeshInstance3D _panel = null!;
    private StandardMaterial3D _material = null!;
    private Tween? _tween;

    public bool IsOpen { get; private set; }

    public void Configure(string id, CabinetPartKind kind, Vector3 center, Vector3 size, bool hingeOnLeft)
    {
        Name = id;
        _kind = kind;
        CollisionLayer = 1;
        AddToGroup("interactable");
        AddToGroup("cabinet_storage");

        var localCenter = Vector3.Zero;
        if (kind == CabinetPartKind.Door)
        {
            Position = center + new Vector3(hingeOnLeft ? -size.X * 0.5f : size.X * 0.5f, 0f, 0f);
            localCenter = new Vector3(hingeOnLeft ? size.X * 0.5f : -size.X * 0.5f, 0f, 0f);
            _openRotationY = hingeOnLeft ? -1.48f : 1.48f;
        }
        else
        {
            Position = center;
            _openPosition = center + new Vector3(0f, 0f, -0.48f);
        }
        _closedPosition = Position;

        _material = new StandardMaterial3D
        {
            AlbedoColor = new Color("70452f"),
            Roughness = 0.78f,
            Metallic = 0.03f
        };
        _panel = new MeshInstance3D
        {
            Name = "Panel",
            Position = localCenter,
            Mesh = new BoxMesh { Size = size },
            MaterialOverride = _material
        };
        AddChild(_panel);
        if (kind == CabinetPartKind.Drawer)
            AddDrawerTray(size);
        AddChild(new CollisionShape3D
        {
            Name = "CollisionShape3D",
            Position = localCenter,
            Shape = new BoxShape3D { Size = size }
        });

        var handle = new MeshInstance3D
        {
            Name = "Handle",
            Position = localCenter + new Vector3(
                kind == CabinetPartKind.Door ? (hingeOnLeft ? size.X * 0.28f : -size.X * 0.28f) : 0f,
                kind == CabinetPartKind.Door ? size.Y * 0.28f : 0f,
                -size.Z * 0.62f),
            Mesh = new BoxMesh { Size = kind == CabinetPartKind.Door ? new Vector3(0.06f, 0.2f, 0.06f) : new Vector3(0.32f, 0.06f, 0.06f) },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("c79b58"), Metallic = 0.65f, Roughness = 0.28f }
        };
        AddChild(handle);
        GameSession.Instance.WorldModeChanged += OnWorldModeChanged;
    }

    public string GetPrompt(InteractionContext context) =>
        $"[E] {(IsOpen ? "关闭" : "打开")}{(_kind == CabinetPartKind.Drawer ? "抽屉" : "柜门")}（当前为空）";

    public string GetUnavailablePrompt(InteractionContext context) =>
        GameSession.Instance.WorldMode == WorldMode.Glasses ? "[G] 摘下眼镜后操作柜体" : string.Empty;

    public bool CanInteract(InteractionContext context) =>
        GameSession.Instance.GameStarted && GameSession.Instance.WorldMode == WorldMode.Reality &&
        GameSession.Instance.Flow.Current is GlassesBar.Domain.DayPhase.WaitingForOrder or GlassesBar.Domain.DayPhase.Preparation;

    public void Interact(InteractionContext context)
    {
        if (!CanInteract(context))
            return;
        SetOpen(!IsOpen, true);
    }

    public void ResetClosed() => SetOpen(false, false);

    private void SetOpen(bool open, bool animate)
    {
        IsOpen = open;
        _tween?.Kill();
        if (!animate)
        {
            Position = _closedPosition;
            Rotation = Vector3.Zero;
            return;
        }

        _tween = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        if (_kind == CabinetPartKind.Drawer)
            _tween.TweenProperty(this, "position", open ? _openPosition : _closedPosition, 0.28d);
        else
            _tween.TweenProperty(this, "rotation:y", open ? _openRotationY : 0f, 0.28d);
        GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage,
            $"已{(open ? "打开" : "关闭")}{(_kind == CabinetPartKind.Drawer ? "抽屉" : "柜门")}；内部暂为空。 ");
    }

    private void OnWorldModeChanged(int mode)
    {
        _material.AlbedoColor = (WorldMode)mode == WorldMode.Glasses
            ? new Color("096075")
            : new Color("70452f");
        _material.EmissionEnabled = (WorldMode)mode == WorldMode.Glasses;
        _material.Emission = new Color("063d49");
    }

    private void AddDrawerTray(Vector3 frontSize)
    {
        var trayMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color("4b2d23"),
            Roughness = 0.86f
        };
        AddChild(new MeshInstance3D
        {
            Name = "TrayBottom",
            Position = new Vector3(0f, -frontSize.Y * 0.38f, 0.23f),
            Mesh = new BoxMesh { Size = new Vector3(frontSize.X * 0.9f, 0.035f, 0.46f) },
            MaterialOverride = trayMaterial
        });
        foreach (var side in new[] { -1f, 1f })
            AddChild(new MeshInstance3D
            {
                Name = side < 0 ? "TrayLeft" : "TrayRight",
                Position = new Vector3(side * frontSize.X * 0.43f, -frontSize.Y * 0.08f, 0.23f),
                Mesh = new BoxMesh { Size = new Vector3(0.045f, frontSize.Y * 0.58f, 0.46f) },
                MaterialOverride = trayMaterial
            });
        AddChild(new MeshInstance3D
        {
            Name = "TrayBack",
            Position = new Vector3(0f, -frontSize.Y * 0.08f, 0.45f),
            Mesh = new BoxMesh { Size = new Vector3(frontSize.X * 0.9f, frontSize.Y * 0.58f, 0.035f) },
            MaterialOverride = trayMaterial
        });
    }
}
