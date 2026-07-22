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
    private Vector3 _outwardDirection = Vector3.Back;
    private string _contentsDescription = "当前为空";
    private MeshInstance3D _panel = null!;
    private StandardMaterial3D _material = null!;
    private Tween? _tween;

    public bool IsOpen { get; private set; }
    public Vector3 ClosedPosition => _closedPosition;
    public Vector3 OpenPosition => _openPosition;
    public float OpenRotationY => _openRotationY;
    public Vector3 OutwardDirection => _outwardDirection;
    public Vector3 PanelSize { get; private set; }
    public float OpenTravelDistance => _closedPosition.DistanceTo(_openPosition);

    public void Configure(string id, CabinetPartKind kind, Vector3 center, Vector3 size, bool hingeOnLeft,
        Vector3 outwardDirection, float storageDepth = 0.72f)
    {
        Name = id;
        _kind = kind;
        PanelSize = size;
        CollisionLayer = 1;
        AddToGroup("interactable");
        AddToGroup("cabinet_storage");
        _outwardDirection = outwardDirection.Normalized();

        var localCenter = Vector3.Zero;
        if (kind == CabinetPartKind.Door)
        {
            Position = center + new Vector3(hingeOnLeft ? -size.X * 0.5f : size.X * 0.5f, 0f, 0f);
            localCenter = new Vector3(hingeOnLeft ? size.X * 0.5f : -size.X * 0.5f, 0f, 0f);
            var opensTowardPositiveZ = _outwardDirection.Z >= 0f;
            _openRotationY = opensTowardPositiveZ
                ? hingeOnLeft ? -1.48f : 1.48f
                : hingeOnLeft ? 1.48f : -1.48f;
        }
        else
        {
            Position = center;
            // Deep trays stay mostly supported by the cabinet carcass. Limiting the pull-out
            // distance keeps the deliberately single-person aisle passable while still exposing
            // the useful front section of the drawer.
            var openTravel = Mathf.Clamp(storageDepth * 0.46f, 0.3f, 0.34f);
            _openPosition = center + _outwardDirection * openTravel;
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
            AddDrawerTray(size, storageDepth);
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
                _outwardDirection.Z * size.Z * 0.62f),
            Mesh = new BoxMesh { Size = kind == CabinetPartKind.Door ? new Vector3(0.06f, 0.2f, 0.06f) : new Vector3(0.32f, 0.06f, 0.06f) },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("c79b58"), Metallic = 0.65f, Roughness = 0.28f }
        };
        AddChild(handle);
        GameSession.Instance.WorldModeChanged += OnWorldModeChanged;
    }

    public string GetPrompt(InteractionContext context) =>
        $"[E] {(IsOpen ? "关闭" : "打开")}{(_kind == CabinetPartKind.Drawer ? "抽屉" : "柜门")}（{_contentsDescription}）";

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

    public void SetContentsDescription(string description) =>
        _contentsDescription = string.IsNullOrWhiteSpace(description) ? "当前为空" : description;

    public void SetOpen(bool open, bool animate)
    {
        if (open)
        {
            // The work aisle is intentionally narrow. Only one storage front may project into it
            // at a time, matching a safe real-world bar workflow and preserving a walking lane.
            foreach (var node in GetTree().GetNodesInGroup("cabinet_storage"))
                if (node is CabinetInteractable other && other != this && other.IsOpen)
                    other.SetOpen(false, animate);
        }
        IsOpen = open;
        _tween?.Kill();
        if (!animate)
        {
            Position = _kind == CabinetPartKind.Drawer && open ? _openPosition : _closedPosition;
            Rotation = _kind == CabinetPartKind.Door && open
                ? new Vector3(0f, _openRotationY, 0f)
                : Vector3.Zero;
            return;
        }

        _tween = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        if (_kind == CabinetPartKind.Drawer)
            _tween.TweenProperty(this, "position", open ? _openPosition : _closedPosition, 0.28d);
        else
            _tween.TweenProperty(this, "rotation:y", open ? _openRotationY : 0f, 0.28d);
        GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage,
            $"已{(open ? "打开" : "关闭")}{(_kind == CabinetPartKind.Drawer ? "抽屉" : "柜门")}；{_contentsDescription}。 ");
    }

    private void OnWorldModeChanged(int mode)
    {
        _material.AlbedoColor = (WorldMode)mode == WorldMode.Glasses
            ? new Color("096075")
            : new Color("70452f");
        _material.EmissionEnabled = (WorldMode)mode == WorldMode.Glasses;
        _material.Emission = new Color("063d49");
    }

    private void AddDrawerTray(Vector3 frontSize, float storageDepth)
    {
        var inwardZ = -_outwardDirection.Z;
        var trayCenterZ = inwardZ * storageDepth * 0.5f;
        var trayMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color("4b2d23"),
            Roughness = 0.86f
        };
        AddChild(new MeshInstance3D
        {
            Name = "TrayBottom",
            Position = new Vector3(0f, -frontSize.Y * 0.38f, trayCenterZ),
            Mesh = new BoxMesh { Size = new Vector3(frontSize.X * 0.9f, 0.035f, storageDepth) },
            MaterialOverride = trayMaterial
        });
        foreach (var side in new[] { -1f, 1f })
            AddChild(new MeshInstance3D
            {
                Name = side < 0 ? "TrayLeft" : "TrayRight",
                Position = new Vector3(side * frontSize.X * 0.43f, -frontSize.Y * 0.08f, trayCenterZ),
                Mesh = new BoxMesh { Size = new Vector3(0.045f, frontSize.Y * 0.58f, storageDepth) },
                MaterialOverride = trayMaterial
            });
        AddChild(new MeshInstance3D
        {
            Name = "TrayBack",
            Position = new Vector3(0f, -frontSize.Y * 0.08f, inwardZ * storageDepth),
            Mesh = new BoxMesh { Size = new Vector3(frontSize.X * 0.9f, frontSize.Y * 0.58f, 0.035f) },
            MaterialOverride = trayMaterial
        });
    }
}
