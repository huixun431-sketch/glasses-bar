using System;
using System.Collections.Generic;
using System.Linq;

namespace GlassesBar.Domain;

public enum ProcessExecutionKind
{
    Completed,
    Failed,
    InsufficientAction,
    RepeatRecovery,
    NonDestructiveBlock
}

public enum ProcessBlockReason
{
    None,
    KettleEmpty,
    MissingMeasuredWater,
    RepeatActionIncomplete
}

public sealed class ProcessExecutionOutcome
{
    public required OperationSpec Operation { get; init; }
    public required ProcessAttemptResult Attempt { get; init; }
    public ProcessExecutionKind Kind { get; init; }
    public ProcessBlockReason BlockReason { get; init; }
    public double OutputCompletion { get; init; }
    public bool FullRecovery { get; init; }
    public ToolInstanceState? RecoveryTarget { get; init; }
}

public sealed class ProcessTransitionHint
{
    public required OperationSpec Operation { get; init; }
    public IReadOnlyList<string> MissingPlacementToolIds { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Selects and commits crafting operations against authoritative tool and drink state.
/// The service has no Godot, node, signal, animation, IK, skeleton, or UI dependency.
/// </summary>
public sealed class ProcessExecutionService
{
    private const double QuantityEpsilon = 0.000001d;

    private readonly ToolInventoryService _inventory;
    private readonly DrinkAssemblyState _assembly;
    private readonly List<OperationSpec> _operations = new();
    private readonly Dictionary<string, int> _repeatRecoveryCounts = new(StringComparer.Ordinal);

    public ProcessExecutionService(ToolInventoryService inventory, DrinkAssemblyState assembly)
    {
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
    }

    public IReadOnlyList<OperationSpec> Operations => _operations;
    public IReadOnlyDictionary<string, int> RepeatRecoveryCounts => _repeatRecoveryCounts;

    public void ConfigureOperations(IEnumerable<OperationSpec> operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        _operations.Clear();
        _operations.AddRange(operations);
    }

    public bool CanUseSimpleOperation =>
        !string.IsNullOrEmpty(_inventory.LeftHandToolId) &&
        !string.IsNullOrEmpty(_inventory.RightHandToolId) &&
        _inventory.GetRequiredTool(_inventory.RightHandToolId).Contents.Count > 0 &&
        _operations.Any(operation =>
            operation.ResolveComplexity() == OperationComplexity.Simple &&
            operation.IsEnabledBy(new HashSet<string>(
                new[] { _inventory.LeftHandToolId },
                StringComparer.Ordinal)));

    public OperationSpec? SelectSimpleOperation()
    {
        if (!CanUseSimpleOperation)
            return null;

        var placementIds = new HashSet<string>(
            new[] { _inventory.LeftHandToolId },
            StringComparer.Ordinal);
        var carrier = _inventory.GetRequiredTool(_inventory.RightHandToolId);
        return SelectBestOperation(
            _operations.Where(candidate =>
                candidate.ResolveComplexity() == OperationComplexity.Simple &&
                candidate.IsEnabledBy(placementIds)),
            carrier.Contents);
    }

    public ProcessExecutionOutcome? ExecuteSimpleOperation(
        Func<double> nextRoll,
        double successProbabilityPenalty)
    {
        ArgumentNullException.ThrowIfNull(nextRoll);

        var operation = SelectSimpleOperation();
        if (operation is null)
            return null;

        var carrier = _inventory.GetRequiredTool(_inventory.RightHandToolId);
        var attempt = operation.TransferActualInputAmounts
            ? EvaluateDirectTransfer(operation, _inventory.RightHandToolId, carrier.Contents)
            : ProcessRules.Evaluate(
                operation,
                _inventory.RightHandToolId,
                carrier.Contents,
                1d,
                nextRoll(),
                successProbabilityPenalty);
        return ApplyAttempt(operation, attempt, new[] { carrier });
    }

    public IReadOnlyList<OperationSpec> GetBoardCapabilities()
    {
        var ids = new HashSet<string>(_inventory.BoardToolIds, StringComparer.Ordinal);
        return _operations
            .Where(operation =>
                operation.ResolveComplexity() != OperationComplexity.Simple &&
                operation.IsEnabledBy(ids))
            .ToArray();
    }

    public OperationSpec? SelectBoardOperation()
    {
        var candidates = GetBoardCapabilities();
        if (candidates.Count == 0 ||
            !_inventory.BoardToolIds.Any(id => _inventory.GetRequiredTool(id).Contents.Count > 0))
            return null;

        var best = SelectBestOperation(candidates, null);
        if (best is not null && OperationInputsMatch(best))
            return best;

        var recovery = candidates.FirstOrDefault(operation => TryGetRepeatRecoveryTarget(operation, out _));
        return recovery ?? best;
    }

    public bool TryGetRepeatRecoveryTarget(OperationSpec operation, out ToolInstanceState target)
    {
        ArgumentNullException.ThrowIfNull(operation);
        target = null!;
        if (string.IsNullOrEmpty(operation.RepeatRecoveryInputIngredientId) ||
            !_inventory.Tools.TryGetValue(operation.ResultTargetToolId, out var candidate) ||
            !_inventory.BoardToolIds.Contains(operation.ResultTargetToolId) ||
            candidate.ContentsAreWaste ||
            candidate.ContentCompletionRatio >= operation.RepeatRecoveryCap - QuantityEpsilon ||
            candidate.Contents.Count != 1 ||
            !candidate.Contents.ContainsKey(operation.RepeatRecoveryInputIngredientId))
            return false;

        target = candidate;
        return true;
    }

    public ProcessTransitionHint? GetBoardTransitionHint()
    {
        if (_inventory.BoardToolIds.Count == 0)
            return null;

        var contents = MergeContents(_inventory.BoardToolIds
            .Select(_inventory.GetRequiredTool)
            .Where(state => !state.ContentsAreWaste));
        var actual = PositiveIngredientIds(contents);
        if (actual.Count == 0)
            return null;

        var placementIds = new HashSet<string>(_inventory.BoardToolIds, StringComparer.Ordinal);
        var next = _operations.FirstOrDefault(operation =>
            operation.ResolveComplexity() != OperationComplexity.Simple &&
            !operation.IsEnabledBy(placementIds) &&
            actual.SetEquals(operation.InputTargets.Keys));
        if (next is null)
            return null;

        return new ProcessTransitionHint
        {
            Operation = next,
            MissingPlacementToolIds = next.RequiredPlacementToolIds
                .Where(id => !placementIds.Contains(id))
                .ToArray()
        };
    }

    public IReadOnlyDictionary<string, double> GetOperationSourceContents(OperationSpec operation) =>
        MergeContents(GetOperationSourceStates(operation));

    public bool OperationInputsMatch(OperationSpec operation) =>
        PositiveIngredientIds(GetOperationSourceContents(operation))
            .SetEquals(operation.InputTargets.Keys);

    public ProcessExecutionOutcome ExecuteBoardOperation(
        OperationSpec operation,
        double action,
        Func<double> nextRoll,
        double successProbabilityPenalty,
        bool kettleHasWater)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(nextRoll);

        var sources = GetOperationSourceStates(operation);
        var ingredients = MergeContents(sources);
        if (operation.Id == "manual_extract" &&
            ingredients.ContainsKey("ground_coffee") &&
            !ingredients.ContainsKey("water"))
        {
            return NonDestructiveBlock(
                operation,
                kettleHasWater
                    ? ProcessBlockReason.MissingMeasuredWater
                    : ProcessBlockReason.KettleEmpty);
        }

        if (TryGetRepeatRecoveryTarget(operation, out var target))
        {
            return ApplyRepeatRecovery(
                operation,
                target,
                action,
                nextRoll,
                successProbabilityPenalty);
        }

        var attempt = ProcessRules.Evaluate(
            operation,
            _inventory.RightHandToolId,
            ingredients,
            action,
            nextRoll(),
            successProbabilityPenalty);
        return ApplyAttempt(operation, attempt, sources);
    }

    public void Reset() => _repeatRecoveryCounts.Clear();

    public Dictionary<string, int> CaptureRepeatRecoveryCounts() =>
        new(_repeatRecoveryCounts, StringComparer.Ordinal);

    public void RestoreRepeatRecoveryCounts(IReadOnlyDictionary<string, int> counts)
    {
        ArgumentNullException.ThrowIfNull(counts);
        _repeatRecoveryCounts.Clear();
        foreach (var recovery in counts)
            _repeatRecoveryCounts[recovery.Key] = Math.Max(0, recovery.Value);
    }

    private ProcessExecutionOutcome ApplyRepeatRecovery(
        OperationSpec operation,
        ToolInstanceState target,
        double action,
        Func<double> nextRoll,
        double successProbabilityPenalty)
    {
        if (Math.Max(0d, action) < Math.Max(0d, operation.RequiredAction))
        {
            return NonDestructiveBlock(
                operation,
                ProcessBlockReason.RepeatActionIncomplete);
        }

        if (!operation.AcceptsHandheldTool(_inventory.RightHandToolId))
        {
            var wrongTool = ProcessRules.Evaluate(
                operation,
                _inventory.RightHandToolId,
                operation.InputTargets,
                action,
                0d,
                successProbabilityPenalty);
            return ApplyAttempt(operation, wrongTool, new[] { target });
        }

        var chance = Math.Clamp(1d - successProbabilityPenalty, 0d, 1d);
        var fullRecovery = nextRoll() <= chance;
        var fraction = operation.RepeatRecoveryFraction * (fullRecovery ? 1d : 0.35d);
        var recovered = ProcessRules.RecoverCompletion(
            target.ContentCompletionRatio,
            operation.RepeatRecoveryCap,
            fraction);
        target.ContentCompletionRatio = recovered;
        _assembly.SetCraftCompletion(recovered);
        _repeatRecoveryCounts.TryGetValue(operation.Id, out var count);
        _repeatRecoveryCounts[operation.Id] = count + 1;

        return new ProcessExecutionOutcome
        {
            Operation = operation,
            Kind = ProcessExecutionKind.RepeatRecovery,
            Attempt = new ProcessAttemptResult
            {
                Completed = true,
                SuccessProbability = chance,
                CompletionRatio = recovered
            },
            OutputCompletion = recovered,
            FullRecovery = fullRecovery,
            RecoveryTarget = target
        };
    }

    private ProcessExecutionOutcome ApplyAttempt(
        OperationSpec operation,
        ProcessAttemptResult attempt,
        IEnumerable<ToolInstanceState> sourceStates)
    {
        var sources = sourceStates.Distinct().ToArray();
        var inheritedCompletion = sources
            .Where(state => state.Contents.Count > 0)
            .Select(state => state.ContentCompletionRatio)
            .DefaultIfEmpty(1d)
            .Min();
        var outputCompletion = Math.Min(inheritedCompletion, attempt.CompletionRatio);
        ProcessExecutionKind kind;

        if (attempt.Completed)
        {
            var actualOutputs = operation.TransferActualInputAmounts
                ? MergeContents(sources)
                : null;
            foreach (var source in sources)
            {
                if (source.Id == "highball_glass")
                    _assembly.EmptyGlass();
                source.ClearContents();
            }

            if (_inventory.Tools.TryGetValue(operation.ResultTargetToolId, out var target))
            {
                var outputs = operation.TransferActualInputAmounts
                    ? actualOutputs!
                    : operation.Outputs;
                foreach (var output in outputs)
                    AddOutput(target, output.Key, output.Value, outputCompletion);
            }

            _assembly.RecordCompletedOperation(operation.Id, outputCompletion);
            kind = ProcessExecutionKind.Completed;
        }
        else if (attempt.Failure == ProcessFailure.InsufficientAction)
        {
            kind = ProcessExecutionKind.InsufficientAction;
        }
        else
        {
            foreach (var source in sources.Where(state => state.Contents.Count > 0))
                source.ContentsAreWaste = true;
            _assembly.RecordFailedOperation();
            kind = ProcessExecutionKind.Failed;
        }

        return new ProcessExecutionOutcome
        {
            Operation = operation,
            Attempt = attempt,
            Kind = kind,
            OutputCompletion = outputCompletion
        };
    }

    private void AddOutput(
        ToolInstanceState target,
        string ingredientId,
        double amount,
        double completion)
    {
        var accepted = Math.Max(0d, amount);
        if (target.Id == "highball_glass")
            accepted = _assembly.AddProcessOutput(ingredientId, amount);

        target.Contents.TryGetValue(ingredientId, out var existing);
        target.Contents[ingredientId] = existing + accepted;
        target.ContentCompletionRatio = Math.Min(target.ContentCompletionRatio, completion);
    }

    private List<ToolInstanceState> GetOperationSourceStates(OperationSpec operation)
    {
        var states = _inventory.BoardToolIds
            .Select(_inventory.GetRequiredTool)
            .Where(state => state.Contents.Count > 0)
            .ToList();
        if (states.Count > 1 && states.Any(state => state.Id == operation.ResultTargetToolId))
        {
            var nonTargetHasInput = states
                .Where(state => state.Id != operation.ResultTargetToolId)
                .SelectMany(state => state.Contents.Keys)
                .Any(operation.InputTargets.ContainsKey);
            if (nonTargetHasInput)
                states.RemoveAll(state => state.Id == operation.ResultTargetToolId);
        }

        return states;
    }

    private OperationSpec? SelectBestOperation(
        IEnumerable<OperationSpec> candidates,
        IReadOnlyDictionary<string, double>? directContents) =>
        candidates
            .Select(operation =>
            {
                var contents = directContents ?? MergeContents(GetOperationSourceStates(operation));
                var actual = PositiveIngredientIds(contents);
                var expected = operation.InputTargets.Keys.ToHashSet(StringComparer.Ordinal);
                return new
                {
                    Operation = operation,
                    Exact = actual.SetEquals(expected) ? 1 : 0,
                    Overlap = actual.Count(expected.Contains),
                    PlacementCount = operation.RequiredPlacementToolIds.Count
                };
            })
            .OrderByDescending(candidate => candidate.Exact)
            .ThenByDescending(candidate => candidate.Overlap)
            .ThenByDescending(candidate => candidate.PlacementCount)
            .Select(candidate => candidate.Operation)
            .FirstOrDefault();

    private static HashSet<string> PositiveIngredientIds(
        IReadOnlyDictionary<string, double> contents) =>
        contents
            .Where(pair => pair.Value > QuantityEpsilon)
            .Select(pair => pair.Key)
            .ToHashSet(StringComparer.Ordinal);

    private static Dictionary<string, double> MergeContents(
        IEnumerable<ToolInstanceState> states)
    {
        var result = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var state in states)
        foreach (var pair in state.Contents)
        {
            result.TryGetValue(pair.Key, out var existing);
            result[pair.Key] = existing + pair.Value;
        }

        return result;
    }

    private static ProcessExecutionOutcome NonDestructiveBlock(
        OperationSpec operation,
        ProcessBlockReason reason) =>
        new()
        {
            Operation = operation,
            Kind = ProcessExecutionKind.NonDestructiveBlock,
            BlockReason = reason,
            Attempt = new ProcessAttemptResult
            {
                Failure = ProcessFailure.InsufficientAction,
                SuccessProbability = 1d,
                CompletionRatio = 0d
            }
        };

    private static ProcessAttemptResult EvaluateDirectTransfer(
        OperationSpec operation,
        string heldHandheldToolId,
        IReadOnlyDictionary<string, double> ingredients)
    {
        var actualTypes = PositiveIngredientIds(ingredients);
        if (!operation.AcceptsHandheldTool(heldHandheldToolId))
            return DirectTransferFailure(ProcessFailure.WrongHandheldTool);
        if (!actualTypes.SetEquals(operation.InputTargets.Keys))
            return DirectTransferFailure(ProcessFailure.WrongIngredients);
        return new ProcessAttemptResult
        {
            Completed = true,
            SuccessProbability = 1d,
            CompletionRatio = 1d
        };
    }

    private static ProcessAttemptResult DirectTransferFailure(ProcessFailure failure) => new()
    {
        Failure = failure,
        MaterialsBecomeWaste = true,
        SuccessProbability = 0d,
        CompletionRatio = 0d
    };
}
