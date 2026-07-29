using System;
using System.Collections.Generic;
using System.Linq;

namespace GlassesBar.Domain;

public enum ToolInventoryFailure
{
    None,
    UnknownTool,
    HandOccupied,
    NoHeldTool,
    LoadedHandheldCannotBePlaced,
    CounterOverlap,
    NoLeftHandTool,
    BoardFull,
    BoardConflict,
    NoBoardContainer,
    NoRightHandTool,
    RightHandToolEmpty,
    RightHandContentsAreWaste,
    CarrierAlreadyLoaded,
    NoCollectableBoardContent,
    ToolCannotCarryIngredient,
    CarrierContainsDifferentIngredient
}

public readonly record struct ToolInventoryCheck(
    bool Allowed,
    ToolInventoryFailure Failure = ToolInventoryFailure.None,
    string ToolId = "",
    string RelatedToolId = "")
{
    public static ToolInventoryCheck Success() => new(true);

    public static ToolInventoryCheck Reject(
        ToolInventoryFailure failure,
        string toolId = "",
        string relatedToolId = "") =>
        new(false, failure, toolId, relatedToolId);
}

public readonly record struct ToolContentTransfer(
    ToolInstanceState Source,
    ToolInstanceState Target,
    string IngredientId = "");

/// <summary>
/// Owns authoritative tool instances, hand slots, workboard slots, placement, and
/// content transfer. It deliberately has no Godot node, transform, animation, IK,
/// skeleton, material, signal, or UI dependency.
/// </summary>
public sealed class ToolInventoryService
{
    public const double CounterPlacementClearance = 0.08d;
    public const int DefaultBoardCapacity = 3;

    private readonly Dictionary<string, ToolInstanceState> _tools = new(StringComparer.Ordinal);
    private readonly List<string> _boardToolIds = new();

    public IReadOnlyDictionary<string, ToolInstanceState> Tools => _tools;
    public IReadOnlyList<string> BoardToolIds => _boardToolIds;
    public string LeftHandToolId { get; private set; } = string.Empty;
    public string RightHandToolId { get; private set; } = string.Empty;
    public bool HasHeldTool => !string.IsNullOrEmpty(LeftHandToolId) || !string.IsNullOrEmpty(RightHandToolId);

    public ToolInstanceState RegisterTool(ToolSpec definition, SpatialPosition initialPosition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (string.IsNullOrWhiteSpace(definition.Id))
            throw new ArgumentException("Tool definitions require a stable ID.", nameof(definition));

        var state = new ToolInstanceState
        {
            Definition = definition,
            InitialPosition = initialPosition,
            Position = initialPosition
        };
        _tools.Add(definition.Id, state);
        return state;
    }

    public ToolInstanceState GetRequiredTool(string toolId) =>
        _tools.TryGetValue(toolId, out var state)
            ? state
            : throw new InvalidOperationException($"Unknown tool ID: {toolId}");

    public ToolInventoryCheck CheckPickUp(string toolId)
    {
        if (!_tools.TryGetValue(toolId, out var state))
            return ToolInventoryCheck.Reject(ToolInventoryFailure.UnknownTool, toolId);

        return state.Definition.ResolveCategory() switch
        {
            ToolCategory.Placement when string.IsNullOrEmpty(LeftHandToolId) => ToolInventoryCheck.Success(),
            ToolCategory.Handheld when string.IsNullOrEmpty(RightHandToolId) => ToolInventoryCheck.Success(),
            ToolCategory.Placement or ToolCategory.Handheld =>
                ToolInventoryCheck.Reject(ToolInventoryFailure.HandOccupied, toolId),
            _ => ToolInventoryCheck.Reject(ToolInventoryFailure.UnknownTool, toolId)
        };
    }

    public ToolInstanceState PickUp(string toolId)
    {
        var check = CheckPickUp(toolId);
        if (!check.Allowed)
            throw new InvalidOperationException($"Tool pickup rejected: {check.Failure}.");

        var state = _tools[toolId];
        if (state.Location == ToolLocation.Workboard)
            _boardToolIds.Remove(toolId);

        state.BoardSlot = -1;
        if (state.Definition.ResolveCategory() == ToolCategory.Placement)
        {
            LeftHandToolId = toolId;
            state.Location = ToolLocation.LeftHand;
        }
        else
        {
            RightHandToolId = toolId;
            state.Location = ToolLocation.RightHand;
        }

        return state;
    }

    public string GetCounterPlacementToolId() =>
        !string.IsNullOrEmpty(LeftHandToolId) ? LeftHandToolId : RightHandToolId;

    public ToolInventoryCheck CheckCounterPlacement(SpatialPosition position)
    {
        var toolId = GetCounterPlacementToolId();
        if (string.IsNullOrEmpty(toolId))
            return ToolInventoryCheck.Reject(ToolInventoryFailure.NoHeldTool);

        var incoming = _tools[toolId];
        if (incoming.Definition.ResolveCategory() == ToolCategory.Handheld && incoming.Contents.Count > 0)
            return ToolInventoryCheck.Reject(ToolInventoryFailure.LoadedHandheldCannotBePlaced, toolId);

        foreach (var existing in _tools.Values.Where(state =>
                     state.Location == ToolLocation.Counter && state.Id != toolId))
        {
            var deltaX = position.X - existing.Position.X;
            var deltaZ = position.Z - existing.Position.Z;
            var distance = Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
            if (distance < incoming.Definition.FootprintRadius + existing.Definition.FootprintRadius +
                CounterPlacementClearance)
            {
                return ToolInventoryCheck.Reject(
                    ToolInventoryFailure.CounterOverlap,
                    toolId,
                    existing.Id);
            }
        }

        return ToolInventoryCheck.Success();
    }

    public ToolInstanceState PlaceHeldToolAt(SpatialPosition position)
    {
        var check = CheckCounterPlacement(position);
        if (!check.Allowed)
            throw new InvalidOperationException($"Counter placement rejected: {check.Failure}.");

        var state = _tools[GetCounterPlacementToolId()];
        state.Location = ToolLocation.Counter;
        state.BoardSlot = -1;
        state.Position = position;
        ClearHand(state.Id);
        return state;
    }

    public ToolInventoryCheck CheckBoardPlacement(int capacity = DefaultBoardCapacity)
    {
        if (string.IsNullOrEmpty(LeftHandToolId))
            return ToolInventoryCheck.Reject(ToolInventoryFailure.NoLeftHandTool);
        if (_boardToolIds.Count >= Math.Max(0, capacity))
            return ToolInventoryCheck.Reject(ToolInventoryFailure.BoardFull, LeftHandToolId);

        var incoming = _tools[LeftHandToolId].Definition;
        foreach (var existingId in _boardToolIds)
        {
            if (ProcessRules.ToolsConflict(incoming, _tools[existingId].Definition))
            {
                return ToolInventoryCheck.Reject(
                    ToolInventoryFailure.BoardConflict,
                    incoming.Id,
                    existingId);
            }
        }

        return ToolInventoryCheck.Success();
    }

    public ToolInstanceState PlaceLeftHandOnBoard(IReadOnlyList<SpatialPosition> boardPositions)
    {
        ArgumentNullException.ThrowIfNull(boardPositions);
        var check = CheckBoardPlacement(boardPositions.Count);
        if (!check.Allowed)
            throw new InvalidOperationException($"Workboard placement rejected: {check.Failure}.");

        var toolId = LeftHandToolId;
        var state = _tools[toolId];
        var slot = Enumerable.Range(0, boardPositions.Count)
            .First(index => _boardToolIds.All(id => _tools[id].BoardSlot != index));
        state.Location = ToolLocation.Workboard;
        state.BoardSlot = slot;
        state.Position = boardPositions[slot];
        _boardToolIds.Add(toolId);
        LeftHandToolId = string.Empty;
        return state;
    }

    public ToolInventoryCheck CheckDepositRightHandContentsOnBoard()
    {
        if (_boardToolIds.Count == 0)
            return ToolInventoryCheck.Reject(ToolInventoryFailure.NoBoardContainer);
        if (string.IsNullOrEmpty(RightHandToolId))
            return ToolInventoryCheck.Reject(ToolInventoryFailure.NoRightHandTool);

        var carrier = _tools[RightHandToolId];
        if (carrier.Contents.Count == 0)
            return ToolInventoryCheck.Reject(ToolInventoryFailure.RightHandToolEmpty, carrier.Id);
        if (carrier.ContentsAreWaste)
            return ToolInventoryCheck.Reject(ToolInventoryFailure.RightHandContentsAreWaste, carrier.Id);

        var target = _boardToolIds
            .Select(id => _tools[id])
            .FirstOrDefault(state => state.Definition.CanContainIngredients);
        return target is null
            ? ToolInventoryCheck.Reject(ToolInventoryFailure.NoBoardContainer, carrier.Id)
            : ToolInventoryCheck.Success();
    }

    public ToolContentTransfer DepositRightHandContentsOnBoard()
    {
        var check = CheckDepositRightHandContentsOnBoard();
        if (!check.Allowed)
            throw new InvalidOperationException($"Workboard deposit rejected: {check.Failure}.");

        var carrier = _tools[RightHandToolId];
        var target = _boardToolIds
            .Select(id => _tools[id])
            .First(state => state.Definition.CanContainIngredients);
        foreach (var pair in carrier.Contents)
        {
            target.Contents.TryGetValue(pair.Key, out var existing);
            target.Contents[pair.Key] = existing + pair.Value;
        }

        target.ContentCompletionRatio = Math.Min(target.ContentCompletionRatio, carrier.ContentCompletionRatio);
        carrier.ClearContents();
        return new ToolContentTransfer(carrier, target);
    }

    public ToolInventoryCheck CheckCollectBoardContents(ISet<string>? allowedIngredientIds = null)
    {
        if (string.IsNullOrEmpty(RightHandToolId))
            return ToolInventoryCheck.Reject(ToolInventoryFailure.NoRightHandTool);

        var carrier = _tools[RightHandToolId];
        if (carrier.Contents.Count > 0)
            return ToolInventoryCheck.Reject(ToolInventoryFailure.CarrierAlreadyLoaded, carrier.Id);

        var source = FindCollectableBoardSource(carrier, allowedIngredientIds);
        return source is null
            ? ToolInventoryCheck.Reject(ToolInventoryFailure.NoCollectableBoardContent, carrier.Id)
            : ToolInventoryCheck.Success();
    }

    public ToolContentTransfer CollectBoardContents(ISet<string>? allowedIngredientIds = null)
    {
        var check = CheckCollectBoardContents(allowedIngredientIds);
        if (!check.Allowed)
            throw new InvalidOperationException($"Workboard collection rejected: {check.Failure}.");

        var carrier = _tools[RightHandToolId];
        var source = FindCollectableBoardSource(carrier, allowedIngredientIds)!;
        var pair = source.Contents.First();
        carrier.Contents[pair.Key] = pair.Value;
        carrier.ContentCompletionRatio = source.ContentCompletionRatio;
        source.ClearContents();
        return new ToolContentTransfer(source, carrier, pair.Key);
    }

    public ToolInventoryCheck CheckLoadIngredient(string ingredientId)
    {
        if (string.IsNullOrEmpty(RightHandToolId))
            return ToolInventoryCheck.Reject(ToolInventoryFailure.NoRightHandTool);

        var carrier = _tools[RightHandToolId];
        if (!carrier.Definition.CanCarry(ingredientId))
            return ToolInventoryCheck.Reject(ToolInventoryFailure.ToolCannotCarryIngredient, carrier.Id);
        if (carrier.ContentsAreWaste)
            return ToolInventoryCheck.Reject(ToolInventoryFailure.RightHandContentsAreWaste, carrier.Id);
        if (carrier.Contents.Count > 0 && !carrier.Contents.ContainsKey(ingredientId))
        {
            return ToolInventoryCheck.Reject(
                ToolInventoryFailure.CarrierContainsDifferentIngredient,
                carrier.Id);
        }

        return ToolInventoryCheck.Success();
    }

    public ToolInstanceState LoadIngredient(string ingredientId, double amount)
    {
        var check = CheckLoadIngredient(ingredientId);
        if (!check.Allowed)
            throw new InvalidOperationException($"Ingredient loading rejected: {check.Failure}.");

        var carrier = _tools[RightHandToolId];
        carrier.Contents.TryGetValue(ingredientId, out var existing);
        carrier.Contents[ingredientId] = existing + Math.Max(0d, amount);
        return carrier;
    }

    public void ResetAll()
    {
        _boardToolIds.Clear();
        LeftHandToolId = string.Empty;
        RightHandToolId = string.Empty;
        foreach (var state in _tools.Values)
        {
            state.ClearContents();
            state.UseLargeMeasureSide = true;
            state.Location = ToolLocation.Counter;
            state.BoardSlot = -1;
            state.Position = state.InitialPosition;
        }
    }

    public List<ToolInstanceSnapshot> CaptureToolSnapshots() =>
        _tools.Values.Select(tool => new ToolInstanceSnapshot
        {
            ToolId = tool.Id,
            Location = tool.Location,
            BoardSlot = tool.BoardSlot,
            Position = tool.Position,
            ContentsAreWaste = tool.ContentsAreWaste,
            ContentCompletionRatio = tool.ContentCompletionRatio,
            UseLargeMeasureSide = tool.UseLargeMeasureSide,
            Contents = new Dictionary<string, double>(tool.Contents, StringComparer.Ordinal)
        }).ToList();

    public void RestoreState(
        IReadOnlyCollection<ToolInstanceSnapshot> tools,
        string leftHandToolId,
        string rightHandToolId,
        IReadOnlyCollection<string> boardToolIds)
    {
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentNullException.ThrowIfNull(boardToolIds);

        var incoming = tools.ToDictionary(tool => tool.ToolId, StringComparer.Ordinal);
        if (incoming.Count != _tools.Count || _tools.Keys.Any(id => !incoming.ContainsKey(id)))
            throw new InvalidOperationException("Save tool instances do not match the configured gameplay catalog.");

        LeftHandToolId = leftHandToolId ?? string.Empty;
        RightHandToolId = rightHandToolId ?? string.Empty;
        _boardToolIds.Clear();
        _boardToolIds.AddRange(boardToolIds);

        foreach (var state in _tools.Values)
        {
            var saved = incoming[state.Id];
            state.Location = saved.Location;
            state.BoardSlot = saved.BoardSlot;
            state.Position = saved.Position;
            state.ContentsAreWaste = saved.ContentsAreWaste;
            state.ContentCompletionRatio = Math.Clamp(saved.ContentCompletionRatio, 0d, 1d);
            state.UseLargeMeasureSide = saved.UseLargeMeasureSide;
            state.Contents.Clear();
            foreach (var content in saved.Contents.Where(content => content.Value > 0d))
                state.Contents[content.Key] = content.Value;
        }
    }

    private ToolInstanceState? FindCollectableBoardSource(
        ToolInstanceState carrier,
        ISet<string>? allowedIngredientIds) =>
        _boardToolIds
            .Select(id => _tools[id])
            .FirstOrDefault(source =>
                !source.ContentsAreWaste &&
                source.Contents.Count == 1 &&
                (allowedIngredientIds is null || allowedIngredientIds.Contains(source.Contents.Keys.First())) &&
                carrier.Definition.CanCarry(source.Contents.Keys.First()));

    private void ClearHand(string toolId)
    {
        if (LeftHandToolId == toolId)
            LeftHandToolId = string.Empty;
        if (RightHandToolId == toolId)
            RightHandToolId = string.Empty;
    }
}
