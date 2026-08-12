using System;
using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

public enum CabinetPartKind
{
    Door,
    Drawer,
    SlidingDoor
}

public partial class CabinetInteractable : StaticBody3D, IInteractable
{
    [Signal]
    public delegate void OpenStateChangedEventHandler(bool open);

    private CabinetPartKind _kind;
    private Vector3 _closedPosition;
    private Vector3 _openPosition;
    private float _openRotationY;
    private Vector3 _outwardDirection = Vector3.Back;
    private string _contentsDescription = "当前为空";
    private MeshInstance3D _panel = null!;
    private MeshInstance3D? _movingLeaf;
    private CollisionShape3D? _movingLeafCollision;
    private Vector3 _movingLeafClosedPosition;
    private Vector3 _movingLeafOpenPosition;
    private StandardMaterial3D _material = null!;
    private Node3D? _productionVisual;
    private Node3D? _productionMovingVisual;
    private Vector3 _productionMovingClosedPosition;
    private Vector3 _productionMovingOpenPosition;
    private Tween? _tween;

    public bool IsOpen { get; private set; }
    public CabinetPartKind Kind => _kind;
    public Vector3 ClosedPosition => _closedPosition;
    public Vector3 OpenPosition => _openPosition;
    public float OpenRotationY => _openRotationY;
    public Vector3 OutwardDirection => _outwardDirection;
    public Vector3 PanelSize { get; private set; }
    public float OpenTravelDistance => _kind == CabinetPartKind.SlidingDoor
        ? _movingLeafClosedPosition.DistanceTo(_movingLeafOpenPosition)
        : _closedPosition.DistanceTo(_openPosition);

    public void Configure(string id, CabinetPartKind kind, Vector3 center, Vector3 size, bool hingeOnLeft,
        Vector3 outwardDirection, float storageDepth, float openTravelDistance,
        float doorOpenAngleRadians = 1.48f)
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
            if (doorOpenAngleRadians <= 0f || doorOpenAngleRadians >= Mathf.Pi)
                throw new System.ArgumentOutOfRangeException(nameof(doorOpenAngleRadians));
            _openRotationY = opensTowardPositiveZ
                ? hingeOnLeft ? -doorOpenAngleRadians : doorOpenAngleRadians
                : hingeOnLeft ? doorOpenAngleRadians : -doorOpenAngleRadians;
        }
        else if (kind == CabinetPartKind.Drawer)
        {
            if (openTravelDistance <= 0f)
                throw new System.ArgumentOutOfRangeException(nameof(openTravelDistance),
                    "Drawer travel must be positive.");
            Position = center;
            _openPosition = center + _outwardDirection * openTravelDistance;
        }
        else
        {
            if (openTravelDistance <= 0f || openTravelDistance > size.X * 0.55f)
                throw new System.ArgumentOutOfRangeException(nameof(openTravelDistance),
                    "Sliding-door travel must move one half-width leaf inside its own bay.");
            Position = center;
            _openPosition = center;
        }
        _closedPosition = Position;

        _material = new StandardMaterial3D
        {
            AlbedoColor = new Color("70452f"),
            Roughness = 0.78f,
            Metallic = 0.03f
        };
        if (kind == CabinetPartKind.SlidingDoor)
        {
            AddSlidingDoorLeaves(size, openTravelDistance);
            GameSession.Instance.WorldModeChanged += OnWorldModeChanged;
            return;
        }

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
        $"[E] {(IsOpen ? "关闭" : "打开")}{PartLabel()}（{_contentsDescription}）";

    public GameplayActionDefinition GetActionDefinition(InteractionContext context) =>
        GameplayActionDefinitions.ToggleStorage;

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

    public void SetProductionVisual(Node3D visual)
    {
        ArgumentNullException.ThrowIfNull(visual);
        if (_productionVisual is not null)
            throw new InvalidOperationException($"Production visual is already bound to {Name}.");
        if (visual.GetParent() != this)
            throw new InvalidOperationException($"Production visual for {Name} must be parented to the authoritative cabinet.");

        _productionVisual = visual;
        _productionVisual.Name = "ProductionVisual";
        _panel.Visible = false;
        if (_movingLeaf is not null)
            _movingLeaf.Visible = false;
        foreach (var grayboxName in new[] { "Handle", "TrayBottom", "TrayLeft", "TrayRight", "TrayBack" })
            if (GetNodeOrNull<GeometryInstance3D>(grayboxName) is { } graybox)
                graybox.Visible = false;

        if (_kind != CabinetPartKind.SlidingDoor)
            return;
        _productionMovingVisual = visual.GetNodeOrNull<Node3D>("MovingProductionVisual") ??
            throw new InvalidOperationException($"Sliding cabinet {Name} has no moving production leaf.");
        _productionMovingClosedPosition = _productionMovingVisual.Position;
        _productionMovingOpenPosition = _productionMovingClosedPosition +
            (_movingLeafOpenPosition - _movingLeafClosedPosition);
    }

    public void SetOpen(bool open, bool animate)
    {
        if (open)
        {
            // Only one storage front may project into the work aisle at a time.
            foreach (var node in GetTree().GetNodesInGroup("cabinet_storage"))
                if (node is CabinetInteractable other && other != this && other.IsOpen)
                    other.SetOpen(false, animate);
        }
        IsOpen = open;
        EmitSignal(SignalName.OpenStateChanged, open);
        _tween?.Kill();
        if (!animate)
        {
            Position = _kind == CabinetPartKind.Drawer && open ? _openPosition : _closedPosition;
            Rotation = _kind == CabinetPartKind.Door && open
                ? new Vector3(0f, _openRotationY, 0f)
                : Vector3.Zero;
            if (_kind == CabinetPartKind.SlidingDoor)
                SetSlidingLeafPosition(open ? _movingLeafOpenPosition : _movingLeafClosedPosition);
            return;
        }

        _tween = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        if (_kind == CabinetPartKind.Drawer)
            _tween.TweenProperty(this, "position", open ? _openPosition : _closedPosition, 0.28d);
        else if (_kind == CabinetPartKind.Door)
            _tween.TweenProperty(this, "rotation:y", open ? _openRotationY : 0f, 0.28d);
        else
        {
            var target = open ? _movingLeafOpenPosition : _movingLeafClosedPosition;
            _tween.SetParallel();
            _tween.TweenProperty(_movingLeaf, "position", target, 0.28d);
            _tween.TweenProperty(_movingLeafCollision, "position", target, 0.28d);
            if (_productionMovingVisual is not null)
                _tween.TweenProperty(_productionMovingVisual, "position",
                    open ? _productionMovingOpenPosition : _productionMovingClosedPosition, 0.28d);
        }
        GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage,
            $"已{(open ? "打开" : "关闭")}{PartLabel()}；{_contentsDescription}。 ");
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

    private void AddSlidingDoorLeaves(Vector3 size, float openTravelDistance)
    {
        var leafSize = new Vector3(size.X * 0.5f, size.Y, size.Z);
        var fixedPosition = new Vector3(-size.X * 0.25f, 0f, 0f);
        var behindOffset = -_outwardDirection * 0.022f;
        _movingLeafClosedPosition = new Vector3(size.X * 0.25f, 0f, 0f) + behindOffset;
        _movingLeafOpenPosition = _movingLeafClosedPosition + Vector3.Left * openTravelDistance;

        _panel = CreateSlidingLeaf("FixedLeaf", fixedPosition, leafSize, false);
        _movingLeaf = CreateSlidingLeaf("MovingLeaf", _movingLeafClosedPosition, leafSize, true);
        AddChild(new CollisionShape3D
        {
            Name = "FixedLeafCollision",
            Position = fixedPosition,
            Shape = new BoxShape3D { Size = leafSize }
        });
        _movingLeafCollision = new CollisionShape3D
        {
            Name = "MovingLeafCollision",
            Position = _movingLeafClosedPosition,
            Shape = new BoxShape3D { Size = leafSize }
        };
        AddChild(_movingLeafCollision);
    }

    private MeshInstance3D CreateSlidingLeaf(string name, Vector3 position, Vector3 size, bool handleOnRight)
    {
        var leaf = new MeshInstance3D
        {
            Name = name,
            Position = position,
            Mesh = new BoxMesh { Size = size },
            MaterialOverride = _material
        };
        leaf.AddChild(new MeshInstance3D
        {
            Name = "Handle",
            Position = new Vector3(handleOnRight ? -size.X * 0.32f : size.X * 0.32f,
                size.Y * 0.25f, _outwardDirection.Z * size.Z * 0.62f),
            Mesh = new BoxMesh { Size = new Vector3(0.06f, 0.20f, 0.06f) },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color("c79b58"), Metallic = 0.65f, Roughness = 0.28f
            }
        });
        AddChild(leaf);
        return leaf;
    }

    private void SetSlidingLeafPosition(Vector3 position)
    {
        if (_movingLeaf is not null)
            _movingLeaf.Position = position;
        if (_movingLeafCollision is not null)
            _movingLeafCollision.Position = position;
        if (_productionMovingVisual is not null)
            _productionMovingVisual.Position = position == _movingLeafOpenPosition
                ? _productionMovingOpenPosition
                : _productionMovingClosedPosition;
    }

    private string PartLabel() => _kind switch
    {
        CabinetPartKind.Drawer => "抽屉",
        CabinetPartKind.SlidingDoor => "推拉门",
        _ => "柜门"
    };
}
