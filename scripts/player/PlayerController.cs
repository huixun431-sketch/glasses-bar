using System;
using Godot;

namespace GlassesBar;

public partial class PlayerController : CharacterBody3D
{
    [Signal] public delegate void PromptChangedEventHandler(string prompt);
    [Signal] public delegate void OperationChangedEventHandler(string prompt, bool active);

    [Export] public float MoveSpeed { get; set; } = 4.2f;
    [Export] public float MouseSensitivity { get; set; } = 0.0022f;
    [Export] public float Gravity { get; set; } = 18f;

    private Node3D _head = null!;
    private RayCast3D _ray = null!;
    private MeshInstance3D _heldGlass = null!;
    private DrinkWorkstation? _workstation;
    private IManualOperation? _operation;
    private double _gestureIntensity;
    private string _lastPrompt = string.Empty;

    public override void _Ready()
    {
        _head = GetNode<Node3D>("Head");
        _ray = GetNode<RayCast3D>("Head/Camera3D/InteractionRay");
        _heldGlass = GetNode<MeshInstance3D>("Head/Camera3D/HandAnchor/HeldGlass");
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
            if (_operation is not null)
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
            GameSession.Instance.ToggleWorld();
        }

        if (@event.IsActionPressed("interact"))
            TryInteract();

        if (@event.IsActionReleased("operate") && _operation is not null)
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

        var input = GameSession.Instance.CanMove && _operation is null
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
        _workstation.GlassHeldChanged += held => _heldGlass.Visible = held;
        _heldGlass.Visible = workstation.HasGlass;
    }

    public void BeginOperation(IManualOperation operation)
    {
        _operation = operation;
        _gestureIntensity = 0d;
        EmitSignal(SignalName.OperationChanged, operation.OperationPrompt, true);
    }

    private void TryInteract()
    {
        if (_operation is not null || _workstation is null || !_ray.IsColliding())
            return;
        if (_ray.GetCollider() is not IInteractable interactable)
            return;

        var context = new InteractionContext { Player = this, Workstation = _workstation };
        interactable.Interact(context);
    }

    private void UpdateOperation(double delta)
    {
        if (_operation is null)
            return;

        var held = Input.IsActionPressed("operate") ? 0.3d : 0d;
        var assist = Input.IsActionPressed("operate_assist") ? 0.8d : 0d;
        var intensity = Math.Max(Math.Max(held, assist), _gestureIntensity);
        _operation.UpdateOperation(intensity, delta);
        _gestureIntensity = Math.Max(0d, _gestureIntensity - delta * 3d);
    }

    private void CompleteOperation()
    {
        if (_operation is null)
            return;
        var result = _operation.Complete();
        GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, result.Feedback);
        _operation = null;
        EmitSignal(SignalName.OperationChanged, string.Empty, false);
    }

    private void CancelOperation()
    {
        if (_operation is null)
            return;
        _operation.Cancel();
        _operation = null;
        EmitSignal(SignalName.OperationChanged, string.Empty, false);
    }

    private void UpdatePrompt()
    {
        var prompt = string.Empty;
        if (_operation is not null)
            prompt = _operation.OperationPrompt;
        else if (_workstation is not null && _ray.IsColliding() && _ray.GetCollider() is IInteractable interactable)
        {
            var context = new InteractionContext { Player = this, Workstation = _workstation };
            prompt = interactable.CanInteract(context) ? interactable.GetPrompt(context) : string.Empty;
        }

        if (prompt == _lastPrompt)
            return;
        _lastPrompt = prompt;
        EmitSignal(SignalName.PromptChanged, prompt);
    }
}

