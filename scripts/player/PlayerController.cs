using System;
using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

public partial class PlayerController : CharacterBody3D
{
    [Signal] public delegate void PromptChangedEventHandler(string prompt);
    [Signal] public delegate void PromptStateChangedEventHandler(string prompt, bool available);
    [Signal] public delegate void OperationChangedEventHandler(string prompt, bool active);
    [Signal] public delegate void OperationProgressChangedEventHandler(float progress);
    [Signal] public delegate void ActionPhaseChangedEventHandler(string actionId, int phase, string targetId);

    [Export] public float MoveSpeed { get; set; } = 4.2f;
    [Export] public float MouseSensitivity { get; set; } = 0.0022f;
    [Export] public float Gravity { get; set; } = 18f;

    private Node3D _head = null!;
    private RayCast3D _ray = null!;
    private ShapeCast3D _probe = null!;
    private MeshInstance3D _leftHandVisual = null!;
    private MeshInstance3D _rightHandVisual = null!;
    private Label3D _leftHandLabel = null!;
    private Label3D _rightHandLabel = null!;
    private DrinkWorkstation? _workstation;
    private readonly GameplayActionPipeline _actions = new();
    private double _gestureIntensity;
    private string _lastPrompt = string.Empty;
    private bool _lastPromptAvailable;
    private Transform3D _dayStartTransform;
    private Vector3 _dayStartHeadRotation;
    private Vector3 _focusedInteractionPoint;

    public GameplayActionPipeline Actions => _actions;

    public override void _Ready()
    {
        _head = GetNode<Node3D>("Head");
        _ray = GetNode<RayCast3D>("Head/Camera3D/InteractionRay");
        _probe = GetNode<ShapeCast3D>("Head/Camera3D/InteractionProbe");
        _leftHandVisual = GetNode<MeshInstance3D>("Head/Camera3D/LeftHandAnchor/HeldTool");
        _rightHandVisual = GetNode<MeshInstance3D>("Head/Camera3D/RightHandAnchor/HeldTool");
        _leftHandLabel = GetNode<Label3D>("Head/Camera3D/LeftHandAnchor/Label");
        _rightHandLabel = GetNode<Label3D>("Head/Camera3D/RightHandAnchor/Label");
        _dayStartTransform = Transform;
        _dayStartHeadRotation = _head.Rotation;
        _actions.Transitioned += trace =>
            EmitSignal(SignalName.ActionPhaseChanged, trace.ActionId, (int)trace.Phase, trace.TargetId);
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("release_mouse"))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            return;
        }

        if (@event is InputEventMouseButton && Input.MouseMode == Input.MouseModeEnum.Visible)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            return;
        }

        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            if (_actions.HasActiveAction)
            {
                _gestureIntensity = Math.Clamp(Math.Abs(motion.Relative.Y) / 18d, 0d, 1d);
            }
            else
            {
                RotateY(-motion.Relative.X * MouseSensitivity);
                _head.RotateX(-motion.Relative.Y * MouseSensitivity);
                var rotation = _head.Rotation;
                rotation.X = Mathf.Clamp(rotation.X, -1.45f, 1.45f);
                _head.Rotation = rotation;
            }
        }

        if (@event.IsActionPressed("toggle_glasses"))
        {
            CancelOperation();
            ExecuteInstant(
                GameplayActionDefinitions.ToggleWorld,
                "game_session",
                () => GameSession.Instance.GameStarted && GameSession.Instance.Flow.Current != DayPhase.DaySummary,
                () =>
                {
                    GameSession.Instance.ToggleWorld();
                    return string.Empty;
                },
                "当前阶段无法切换眼镜世界。");
        }

        if (@event.IsActionPressed("next_day") && GameSession.Instance.Flow.Current == GlassesBar.Domain.DayPhase.DaySummary && _workstation is not null)
        {
            ExecuteInstant(
                GameplayActionDefinitions.AdvanceDay,
                "game_session",
                () => GameSession.Instance.Flow.Current == DayPhase.DaySummary,
                () =>
                {
                    _workstation.ResetForNewDay();
                    ResetForNewDay();
                    GameSession.Instance.AdvanceToNextDay();
                    return string.Empty;
                },
                "只有日结阶段可以进入下一天。");
            return;
        }

        if (@event.IsActionPressed("interact"))
            TryInteract();

        if (@event.IsActionPressed("use_held_tool") && _workstation is not null && !_actions.HasActiveAction)
        {
            ExecuteInstant(
                GameplayActionDefinitions.UseHeldTool,
                "held_tools",
                () => _workstation.CanUseSimpleOperation,
                () => _workstation.TryUseSimpleOperation().Feedback,
                "当前双手组合无法进行简易工序；左手需持放置类工具，右手工具需携带原材料。");
        }

        if (@event.IsActionPressed("toggle_jigger_side") && _workstation is not null && !_actions.HasActiveAction)
            ExecuteInstant(
                GameplayActionDefinitions.ToggleMeasureSide,
                _workstation.RightHandToolId,
                () => _workstation.RightHandHasDualMeasure,
                () =>
                {
                    _workstation.ToggleRightHandMeasureSide(out var feedback);
                    return feedback;
                },
                "右手需要拿着一种双头量酒器才能切换量杯端。");

        if (@event.IsActionReleased("operate") && _actions.HasActiveAction)
            CompleteOperation();

        if (@event.IsActionPressed("cancel_operation"))
            CancelOperation();
    }

    public override void _PhysicsProcess(double delta)
    {
        var velocity = Velocity;
        if (!IsOnFloor())
            velocity.Y -= Gravity * (float)delta;
        else if (velocity.Y < 0f)
            velocity.Y = 0f;

        var input = GameSession.Instance.CanMove && !_actions.HasActiveAction && !DeveloperConsole.IsOpen
            ? Input.GetVector("move_left", "move_right", "move_forward", "move_back")
            : Vector2.Zero;
        var direction = (Transform.Basis * new Vector3(input.X, 0f, input.Y)).Normalized();
        velocity.X = direction.X * MoveSpeed;
        velocity.Z = direction.Z * MoveSpeed;
        Velocity = velocity;
        MoveAndSlide();

        UpdateOperation(delta);
        UpdatePrompt();
    }

    public void BindWorkstation(DrinkWorkstation workstation)
    {
        _workstation = workstation;
        _workstation.HandsChanged += UpdateHeldVisuals;
        _workstation.HandToolIdsChanged += UpdateHeldToolMeshes;
        UpdateHeldVisuals(workstation.LeftHandDisplayName, workstation.RightHandDisplayName);
        UpdateHeldToolMeshes(workstation.LeftHandToolId, workstation.RightHandToolId);
    }

    public void BeginOperation(IManualOperation operation)
    {
        if (!_actions.AdoptContinuous(operation))
        {
            operation.Cancel();
            return;
        }
        _gestureIntensity = 0d;
        EmitSignal(SignalName.OperationChanged, operation.OperationPrompt, true);
    }

    public void ResetForNewDay()
    {
        CancelOperation();
        Transform = _dayStartTransform;
        _head.Rotation = _dayStartHeadRotation;
        Velocity = Vector3.Zero;
    }

    public PlayerSnapshot CaptureState() => new()
    {
        Position = ToSpatialPosition(Position),
        BodyRotation = ToSpatialPosition(Rotation),
        HeadRotation = ToSpatialPosition(_head.Rotation)
    };

    public void RestoreState(PlayerSnapshot snapshot)
    {
        CancelOperation();
        Position = ToVector3(snapshot.Position);
        Rotation = ToVector3(snapshot.BodyRotation);
        _head.Rotation = ToVector3(snapshot.HeadRotation);
        Velocity = Vector3.Zero;
    }

    private void TryInteract()
    {
        if (_actions.HasActiveAction || _workstation is null || DeveloperConsole.IsOpen)
            return;
        if (GetFocusedInteractable() is not { } interactable)
            return;

        var result = TryExecuteInteraction(interactable, CreateInteractionContext());
        if (!result.Accepted && !string.IsNullOrWhiteSpace(result.Feedback))
            GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, result.Feedback);
    }

    public GameplayActionExecution TryExecuteInteraction(IInteractable interactable, InteractionContext context) =>
        _actions.TryExecute(CreateInteractionAction(interactable, context));

    private void UpdateOperation(double delta)
    {
        var operation = _actions.ActiveOperation;
        if (operation is null)
            return;

        var held = Input.IsActionPressed("operate") ? 0.3d : 0d;
        var assist = Input.IsActionPressed("operate_assist") ? 0.8d : 0d;
        var intensity = Math.Max(Math.Max(held, assist), _gestureIntensity);
        _actions.UpdateActive(intensity, delta);
        EmitSignal(SignalName.OperationProgressChanged, operation.FeedbackProgress);
        _gestureIntensity = Math.Max(0d, _gestureIntensity - delta * 3d);
    }

    private void CompleteOperation()
    {
        if (!_actions.HasActiveAction)
            return;
        var result = _actions.CommitActive();
        GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, result.Feedback);
        EmitSignal(SignalName.OperationChanged, string.Empty, false);
        EmitSignal(SignalName.OperationProgressChanged, 0f);
    }

    private void CancelOperation()
    {
        if (!_actions.HasActiveAction)
            return;
        _actions.CancelActive();
        EmitSignal(SignalName.OperationChanged, string.Empty, false);
        EmitSignal(SignalName.OperationProgressChanged, 0f);
    }

    private void UpdatePrompt()
    {
        var prompt = string.Empty;
        var available = false;
        if (_actions.ActiveOperation is { } operation)
        {
            prompt = operation.OperationPrompt;
            available = true;
        }
        else if (_workstation is not null && GetFocusedInteractable() is { } interactable)
        {
            var context = CreateInteractionContext();
            var decision = _actions.Inspect(CreateInteractionAction(interactable, context));
            available = decision.IsAvailable;
            prompt = decision.Prompt;
        }

        if (prompt == _lastPrompt && available == _lastPromptAvailable)
            return;
        _lastPrompt = prompt;
        _lastPromptAvailable = available;
        EmitSignal(SignalName.PromptChanged, prompt);
        EmitSignal(SignalName.PromptStateChanged, prompt, available);
    }

    private IInteractable? GetFocusedInteractable()
    {
        if (_ray.IsColliding() && _ray.GetCollider() is IInteractable direct)
        {
            _focusedInteractionPoint = _ray.GetCollisionPoint();
            return direct;
        }

        _probe.ForceShapecastUpdate();
        for (var index = 0; index < _probe.GetCollisionCount(); index++)
        {
            if (_probe.GetCollider(index) is IInteractable nearby)
            {
                _focusedInteractionPoint = _probe.GetCollisionPoint(index);
                return nearby;
            }
        }
        return null;
    }

    private InteractionContext CreateInteractionContext() => new()
    {
        Player = this,
        Workstation = _workstation!,
        InteractionPoint = _focusedInteractionPoint
    };

    private static GameplayActionRequest CreateInteractionAction(IInteractable interactable, InteractionContext context)
    {
        var definition = interactable.GetActionDefinition(context);
        var targetId = interactable is Node node ? node.Name.ToString() : interactable.GetType().Name;
        return new GameplayActionRequest
        {
            Definition = definition,
            TargetId = targetId,
            Evaluate = () =>
            {
                var available = interactable.CanInteract(context);
                return new GameplayActionDecision(available,
                    available ? interactable.GetPrompt(context) : interactable.GetUnavailablePrompt(context));
            },
            Execute = () =>
            {
                interactable.Interact(context);
                return new GameplayActionExecution { Feedback = interactable.GetPrompt(context) };
            }
        };
    }

    private void ExecuteInstant(GameplayActionDefinition definition, string targetId, Func<bool> canExecute,
        Func<string> execute, string unavailableFeedback)
    {
        var result = _actions.TryExecute(new GameplayActionRequest
        {
            Definition = definition,
            TargetId = targetId,
            Evaluate = () => new GameplayActionDecision(canExecute(), unavailableFeedback),
            Execute = () => new GameplayActionExecution { Feedback = execute() }
        });
        if (!result.Accepted && !string.IsNullOrWhiteSpace(result.Feedback))
            GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, result.Feedback);
    }

    private void UpdateHeldVisuals(string leftHand, string rightHand)
    {
        var hasLeft = !string.IsNullOrWhiteSpace(leftHand) && leftHand != "空";
        var hasRight = !string.IsNullOrWhiteSpace(rightHand) && rightHand != "空";
        _leftHandVisual.Visible = hasLeft;
        _rightHandVisual.Visible = hasRight;
        _leftHandLabel.Visible = false;
        _rightHandLabel.Visible = false;
        _leftHandLabel.Text = leftHand;
        _rightHandLabel.Text = rightHand;
    }

    private void UpdateHeldToolMeshes(string leftToolId, string rightToolId)
    {
        _leftHandVisual.Mesh = leftToolId switch
        {
            "mortar" => new CylinderMesh { TopRadius = 0.135f, BottomRadius = 0.165f, Height = 0.16f },
            "traditional_filter" => new CylinderMesh { TopRadius = 0.12f, BottomRadius = 0.07f, Height = 0.22f },
            _ => new CylinderMesh { TopRadius = 0.075f, BottomRadius = 0.06f, Height = 0.22f }
        };
        _rightHandVisual.Mesh = rightToolId switch
        {
            "pestle" => new CylinderMesh { TopRadius = 0.034f, BottomRadius = 0.049f, Height = 0.3f },
            "jigger_small" => new CylinderMesh { TopRadius = 0.055f, BottomRadius = 0.055f, Height = 0.15f },
            "jigger_medium" => new CylinderMesh { TopRadius = 0.065f, BottomRadius = 0.065f, Height = 0.18f },
            "jigger_large" => new CylinderMesh { TopRadius = 0.075f, BottomRadius = 0.075f, Height = 0.21f },
            "ice_tongs" => new BoxMesh { Size = new Vector3(0.08f, 0.06f, 0.4f) },
            _ => new BoxMesh { Size = new Vector3(0.14f, 0.08f, 0.3f) }
        };
    }

    private static SpatialPosition ToSpatialPosition(Vector3 value) => new(value.X, value.Y, value.Z);

    private static Vector3 ToVector3(SpatialPosition value) => new((float)value.X, (float)value.Y, (float)value.Z);
}
