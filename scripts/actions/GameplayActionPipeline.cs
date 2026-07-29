using System;
using GlassesBar.Domain;

namespace GlassesBar;

public sealed class GameplayActionExecution
{
    public bool Accepted { get; init; } = true;
    public string Feedback { get; init; } = string.Empty;
}

public sealed class GameplayActionRequest
{
    public required GameplayActionDefinition Definition { get; init; }
    public required string TargetId { get; init; }
    public required Func<GameplayActionDecision> Evaluate { get; init; }
    public required Func<GameplayActionExecution> Execute { get; init; }
}

/// <summary>
/// The single runtime route for player-issued gameplay commands.
/// Inspection is side-effect free; instant commands commit in Execute, while continuous
/// commands only mutate authoritative gameplay state when their operation completes.
/// </summary>
public sealed class GameplayActionPipeline
{
    private IManualOperation? _activeOperation;
    private GameplayActionDefinition? _activeDefinition;
    private string _activeTargetId = string.Empty;
    private GameplayActionRequest? _executingRequest;

    public event Action<GameplayActionTrace>? Transitioned;

    public bool HasActiveAction => _activeOperation is not null;
    public IManualOperation? ActiveOperation => _activeOperation;
    public GameplayActionTrace? LastTrace { get; private set; }

    public GameplayActionDecision Inspect(GameplayActionRequest request) => request.Evaluate();

    public GameplayActionExecution TryExecute(GameplayActionRequest request)
    {
        if (HasActiveAction)
            return Reject(request, "已有动作正在进行；先完成或取消当前动作。");

        var decision = Inspect(request);
        if (!decision.IsAvailable)
            return Reject(request, decision.Prompt);

        _executingRequest = request;
        GameplayActionExecution execution;
        try
        {
            execution = request.Execute();
        }
        finally
        {
            _executingRequest = null;
        }

        if (!execution.Accepted)
            return Reject(request, execution.Feedback);

        if (HasActiveAction)
        {
            Trace(request.Definition, request.TargetId, GameplayActionPhase.Started, execution.Feedback);
            return execution;
        }

        if (request.Definition.Mode == GameplayActionMode.Continuous)
            return Reject(request, "连续动作处理器没有启动动作过程。");

        Trace(request.Definition, request.TargetId, GameplayActionPhase.Committed, execution.Feedback);
        return execution;
    }

    public bool AdoptContinuous(IManualOperation operation)
    {
        if (HasActiveAction || !operation.IsRunning)
            return false;

        var definition = _executingRequest?.Definition ??
                         new GameplayActionDefinition("process.manual", GameplayActionMode.Continuous);
        if (definition.Mode != GameplayActionMode.Continuous)
            return false;

        _activeOperation = operation;
        _activeDefinition = definition;
        _activeTargetId = _executingRequest?.TargetId ?? operation.GetType().Name;
        if (_executingRequest is null)
            Trace(_activeDefinition, _activeTargetId, GameplayActionPhase.Started, operation.OperationPrompt);
        return true;
    }

    public void UpdateActive(double intensity, double deltaSeconds)
    {
        _activeOperation?.UpdateOperation(intensity, deltaSeconds);
    }

    public OperationResult CommitActive()
    {
        if (_activeOperation is null || _activeDefinition is null)
            return new OperationResult { Feedback = "没有正在进行的动作。" };

        var result = _activeOperation.Complete();
        Trace(_activeDefinition, _activeTargetId, GameplayActionPhase.Committed, result.Feedback);
        ClearActive();
        return result;
    }

    public void CancelActive()
    {
        if (_activeOperation is null || _activeDefinition is null)
            return;

        _activeOperation.Cancel();
        Trace(_activeDefinition, _activeTargetId, GameplayActionPhase.Cancelled, "动作已取消；未提交玩法状态。");
        ClearActive();
    }

    private GameplayActionExecution Reject(GameplayActionRequest request, string feedback)
    {
        Trace(request.Definition, request.TargetId, GameplayActionPhase.Rejected, feedback);
        return new GameplayActionExecution { Accepted = false, Feedback = feedback };
    }

    private void Trace(GameplayActionDefinition definition, string targetId, GameplayActionPhase phase, string feedback)
    {
        LastTrace = new GameplayActionTrace(definition.Id, targetId, phase, feedback);
        Transitioned?.Invoke(LastTrace);
    }

    private void ClearActive()
    {
        _activeOperation = null;
        _activeDefinition = null;
        _activeTargetId = string.Empty;
    }
}
