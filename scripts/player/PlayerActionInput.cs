using System;
using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

/// <summary>
/// Translates player input and interaction targets into unified action-pipeline requests.
/// </summary>
internal sealed class PlayerActionInput
{
    private readonly PlayerController _player;
    private readonly PlayerMotor _motor;
    private readonly InteractionSensor _sensor;
    private readonly GameplayActionPipeline _actions;
    private DrinkWorkstation? _workstation;
    private double _gestureIntensity;
    private string _lastPrompt = string.Empty;
    private bool _lastPromptAvailable;

    public PlayerActionInput(
        PlayerController player,
        PlayerMotor motor,
        InteractionSensor sensor,
        GameplayActionPipeline actions)
    {
        _player = player;
        _motor = motor;
        _sensor = sensor;
        _actions = actions;
    }

    public event Action<string, bool>? PromptChanged;
    public event Action<string, bool>? OperationChanged;
    public event Action<float>? OperationProgressChanged;

    public void BindWorkstation(DrinkWorkstation workstation) => _workstation = workstation;

    public void HandleInput(InputEvent @event, float mouseSensitivity)
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
                _gestureIntensity = Math.Clamp(Math.Abs(motion.Relative.Y) / 18d, 0d, 1d);
            else
                _motor.ApplyLook(motion.Relative, mouseSensitivity);
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

        if (@event.IsActionPressed("next_day") &&
            GameSession.Instance.Flow.Current == DayPhase.DaySummary &&
            _workstation is not null)
        {
            ExecuteInstant(
                GameplayActionDefinitions.AdvanceDay,
                "game_session",
                () => GameSession.Instance.Flow.Current == DayPhase.DaySummary,
                () =>
                {
                    _workstation.ResetForNewDay();
                    _player.ResetForNewDay();
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
        {
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
        }

        if (@event.IsActionReleased("operate") && _actions.HasActiveAction)
            CompleteOperation();

        if (@event.IsActionPressed("cancel_operation"))
            CancelOperation();
    }

    public void Update(double delta)
    {
        UpdateOperation(delta);
        UpdatePrompt();
    }

    public void BeginOperation(IManualOperation operation)
    {
        if (!_actions.AdoptContinuous(operation))
        {
            operation.Cancel();
            return;
        }

        _gestureIntensity = 0d;
        OperationChanged?.Invoke(operation.OperationPrompt, true);
    }

    public void CancelOperation()
    {
        if (!_actions.HasActiveAction)
            return;

        _actions.CancelActive();
        OperationChanged?.Invoke(string.Empty, false);
        OperationProgressChanged?.Invoke(0f);
    }

    public GameplayActionExecution TryExecuteInteraction(
        IInteractable interactable,
        InteractionContext context) =>
        _actions.TryExecute(CreateInteractionAction(interactable, context));

    private void TryInteract()
    {
        if (_actions.HasActiveAction || _workstation is null || DeveloperConsole.IsOpen)
            return;
        if (_sensor.GetFocusedInteraction() is not { } focused)
            return;

        var result = TryExecuteInteraction(
            focused.Interactable,
            CreateInteractionContext(focused.Point));
        if (!result.Accepted && !string.IsNullOrWhiteSpace(result.Feedback))
            GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, result.Feedback);
    }

    private void UpdateOperation(double delta)
    {
        var operation = _actions.ActiveOperation;
        if (operation is null)
            return;

        var held = Input.IsActionPressed("operate") ? 0.3d : 0d;
        var assist = Input.IsActionPressed("operate_assist") ? 0.8d : 0d;
        var intensity = Math.Max(Math.Max(held, assist), _gestureIntensity);
        _actions.UpdateActive(intensity, delta);
        OperationProgressChanged?.Invoke(operation.FeedbackProgress);
        _gestureIntensity = Math.Max(0d, _gestureIntensity - delta * 3d);
    }

    private void CompleteOperation()
    {
        if (!_actions.HasActiveAction)
            return;

        var result = _actions.CommitActive();
        GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, result.Feedback);
        OperationChanged?.Invoke(string.Empty, false);
        OperationProgressChanged?.Invoke(0f);
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
        else if (_workstation is not null && _sensor.GetFocusedInteraction() is { } focused)
        {
            var context = CreateInteractionContext(focused.Point);
            var decision = _actions.Inspect(CreateInteractionAction(focused.Interactable, context));
            available = decision.IsAvailable;
            prompt = decision.Prompt;
        }

        if (prompt == _lastPrompt && available == _lastPromptAvailable)
            return;

        _lastPrompt = prompt;
        _lastPromptAvailable = available;
        PromptChanged?.Invoke(prompt, available);
    }

    private InteractionContext CreateInteractionContext(Vector3 interactionPoint) => new()
    {
        Player = _player,
        Workstation = _workstation!,
        InteractionPoint = interactionPoint
    };

    private static GameplayActionRequest CreateInteractionAction(
        IInteractable interactable,
        InteractionContext context)
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
                return new GameplayActionDecision(
                    available,
                    available ? interactable.GetPrompt(context) : interactable.GetUnavailablePrompt(context));
            },
            Execute = () =>
            {
                interactable.Interact(context);
                return new GameplayActionExecution { Feedback = interactable.GetPrompt(context) };
            }
        };
    }

    private void ExecuteInstant(
        GameplayActionDefinition definition,
        string targetId,
        Func<bool> canExecute,
        Func<string> execute,
        string unavailableFeedback)
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
}
