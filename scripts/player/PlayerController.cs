using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

/// <summary>
/// Godot lifecycle and compatibility facade for the composed player subsystems.
/// </summary>
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

    private readonly GameplayActionPipeline _actions = new();
    private PlayerMotor _motor = null!;
    private PlayerActionInput _actionInput = null!;
    private HeldToolPresenter _heldToolPresenter = null!;

    public GameplayActionPipeline Actions => _actions;

    public override void _Ready()
    {
        var head = GetNode<Node3D>("Head");
        _motor = new PlayerMotor(this, head);
        var sensor = new InteractionSensor(
            GetNode<RayCast3D>("Head/Camera3D/InteractionRay"),
            GetNode<ShapeCast3D>("Head/Camera3D/InteractionProbe"));
        _actionInput = new PlayerActionInput(this, _motor, sensor, _actions);
        _heldToolPresenter = new HeldToolPresenter(
            GetNode<MeshInstance3D>("Head/Camera3D/LeftHandAnchor/HeldTool"),
            GetNode<MeshInstance3D>("Head/Camera3D/RightHandAnchor/HeldTool"),
            GetNode<Label3D>("Head/Camera3D/LeftHandAnchor/Label"),
            GetNode<Label3D>("Head/Camera3D/RightHandAnchor/Label"));

        _actionInput.PromptChanged += OnPromptChanged;
        _actionInput.OperationChanged += (prompt, active) =>
            EmitSignal(SignalName.OperationChanged, prompt, active);
        _actionInput.OperationProgressChanged += progress =>
            EmitSignal(SignalName.OperationProgressChanged, progress);
        _actions.Transitioned += trace =>
            EmitSignal(SignalName.ActionPhaseChanged, trace.ActionId, (int)trace.Phase, trace.TargetId);
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent @event) =>
        _actionInput.HandleInput(@event, MouseSensitivity);

    public override void _PhysicsProcess(double delta)
    {
        _motor.Move(
            delta,
            MoveSpeed,
            Gravity,
            GameSession.Instance.CanMove && !_actions.HasActiveAction && !DeveloperConsole.IsOpen);
        _actionInput.Update(delta);
    }

    public void BindWorkstation(DrinkWorkstation workstation)
    {
        _actionInput.BindWorkstation(workstation);
        _heldToolPresenter.Bind(workstation);
    }

    public void BeginOperation(IManualOperation operation) =>
        _actionInput.BeginOperation(operation);

    public void ResetForNewDay()
    {
        _actionInput.CancelOperation();
        _motor.ResetForNewDay();
    }

    public PlayerSnapshot CaptureState() => _motor.CaptureState();

    public void RestoreState(PlayerSnapshot snapshot)
    {
        _actionInput.CancelOperation();
        _motor.RestoreState(snapshot);
    }

    public GameplayActionExecution TryExecuteInteraction(
        IInteractable interactable,
        InteractionContext context) =>
        _actionInput.TryExecuteInteraction(interactable, context);

    private void OnPromptChanged(string prompt, bool available)
    {
        EmitSignal(SignalName.PromptChanged, prompt);
        EmitSignal(SignalName.PromptStateChanged, prompt, available);
    }
}
