using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

public partial class DrinkWorkstation : Node
{
    [Signal] public delegate void DrinkChangedEventHandler(string debugText);
    [Signal] public delegate void HandsChangedEventHandler(string leftHand, string rightHand);
    [Signal] public delegate void HandToolIdsChangedEventHandler(string leftToolId, string rightToolId);

    private readonly DrinkSnapshot _snapshot = new();
    private readonly Dictionary<string, ToolSpec> _toolSpecs = new(StringComparer.Ordinal);
    private readonly List<OperationSpec> _operations = new();
    private readonly Dictionary<string, ToolRuntimeState> _tools = new(StringComparer.Ordinal);
    private readonly List<string> _boardToolIds = new();
    private readonly RandomNumberGenerator _random = new();
    private RecipeTargets _recipeTargets = new() { IsPrototype = true };
    private bool _timing;
    private double? _nextAttemptRoll;

    public LiquidContainer Glass { get; private set; } = new(3d);
    public string LeftHandToolId { get; private set; } = string.Empty;
    public string RightHandToolId { get; private set; } = string.Empty;
    public bool HasHeldTool => !string.IsNullOrEmpty(LeftHandToolId) || !string.IsNullOrEmpty(RightHandToolId);
    public bool HasGlass => string.Equals(LeftHandToolId, "highball_glass", StringComparison.Ordinal);
    public int IcePieces => (int)Math.Round(Glass.Ingredients.TryGetValue("ice", out var ice) ? ice : 0d);
    public double TotalWaste { get; private set; }
    public int BoardToolCount => _boardToolIds.Count;
    public string LastOperationFeedback { get; private set; } = string.Empty;
    public ProcessAttemptResult? LastProcessResult { get; private set; }
    public string LeftHandDisplayName => HandDisplay(LeftHandToolId);
    public string RightHandDisplayName => HandDisplay(RightHandToolId, true);
    public string CounterPlacementDisplayName => HandDisplay(GetCounterPlacementToolId(), true);

    public double GetRightHandIngredientAmount(string ingredientId) =>
        !string.IsNullOrEmpty(RightHandToolId) && _tools[RightHandToolId].Contents.TryGetValue(ingredientId, out var amount)
            ? amount
            : 0d;

    public ToolLocation GetToolLocation(string toolId) => _tools[toolId].Location;
    public bool IsToolContentWaste(string toolId) => _tools[toolId].ContentsAreWaste;
    public double GetToolContentAmount(string toolId, string ingredientId) =>
        _tools[toolId].Contents.TryGetValue(ingredientId, out var amount) ? amount : 0d;
    public bool IsToolOnBoard(string toolId) => _boardToolIds.Contains(toolId);
    public double DrinkCompletionRatio => _tools.TryGetValue("highball_glass", out var glass)
        ? glass.ContentCompletionRatio
        : 1d;

    public override void _Ready()
    {
        _random.Randomize();
        var recipe = ResourceLoader.Load<RecipeDefinition>("res://data/recipes/prototype_iced_americano.tres");
        if (recipe is null)
            throw new InvalidOperationException("Prototype recipe resource could not be loaded.");
        _recipeTargets = recipe.BuildTargets();
        GameSession.Instance.DayPhaseChanged += OnPhaseChanged;
    }

    public override void _Process(double delta)
    {
        if (_timing)
            _snapshot.ElapsedSeconds += Math.Max(0d, delta);
    }

    public void ConfigureCatalog(GameplayCatalogDefinition catalog)
    {
        _toolSpecs.Clear();
        foreach (var pair in catalog.BuildToolSpecs())
            _toolSpecs.Add(pair.Key, pair.Value);
        _operations.Clear();
        _operations.AddRange(catalog.BuildOperationSpecs());
    }

    public ToolSpec GetToolSpec(string toolId) =>
        _toolSpecs.TryGetValue(toolId, out var spec)
            ? spec
            : throw new InvalidOperationException($"Unknown tool ID: {toolId}");

    public OperationComplexity GetOperationComplexity(string operationId) =>
        _operations.First(operation => operation.Id == operationId).ResolveComplexity();

    public void RegisterTool(ToolInteractable node, string toolId, Vector3 initialPosition)
    {
        var spec = GetToolSpec(toolId);
        var state = new ToolRuntimeState
        {
            Spec = spec,
            Node = node,
            InitialPosition = initialPosition
        };
        _tools.Add(toolId, state);
        node.ApplyWorldState(initialPosition, true);
    }

    public bool CanPickUpTool(string toolId)
    {
        if (!_tools.TryGetValue(toolId, out var state))
            return false;
        return state.Spec.ResolveCategory() switch
        {
            ToolCategory.Placement => string.IsNullOrEmpty(LeftHandToolId),
            ToolCategory.Handheld => string.IsNullOrEmpty(RightHandToolId),
            _ => false
        };
    }

    public bool TryPickUpTool(string toolId)
    {
        if (!CanPickUpTool(toolId) || !_tools.TryGetValue(toolId, out var state))
            return false;

        if (state.Location == ToolLocation.Workboard)
            _boardToolIds.Remove(toolId);

        state.BoardSlot = -1;
        if (state.Spec.ResolveCategory() == ToolCategory.Placement)
        {
            LeftHandToolId = toolId;
            state.Location = ToolLocation.LeftHand;
        }
        else
        {
            RightHandToolId = toolId;
            state.Location = ToolLocation.RightHand;
        }
        state.Node.ApplyWorldState(state.Node.GlobalPosition, false);
        EmitHandsAndState($"已将{state.Spec.DisplayName}拿到{(state.Spec.ResolveCategory() == ToolCategory.Placement ? "左手" : "右手")}。原位置现在为空。");
        return true;
    }

    public bool CanPlaceHeldToolAtPosition(Vector3 position, out string reason)
    {
        reason = string.Empty;
        var toolId = GetCounterPlacementToolId();
        if (string.IsNullOrEmpty(toolId))
        {
            reason = "双手没有可放置的工具。";
            return false;
        }
        var incoming = _tools[toolId];
        if (incoming.Spec.ResolveCategory() == ToolCategory.Handheld && incoming.Contents.Count > 0)
        {
            reason = $"{incoming.Spec.DisplayName}还装有{ContentText(incoming)}，不能直接搁在台面；先完成转移或倒入弃物桶。";
            return false;
        }
        foreach (var existing in _tools.Values.Where(state => state.Location == ToolLocation.Counter && state.Spec.Id != toolId))
        {
            var distance = new Vector2(position.X - existing.Node.GlobalPosition.X, position.Z - existing.Node.GlobalPosition.Z).Length();
            if (distance < incoming.Spec.FootprintRadius + existing.Spec.FootprintRadius + 0.08d)
            {
                reason = $"此处会与{existing.Spec.DisplayName}重合，请瞄准其他空余位置。";
                return false;
            }
        }
        return true;
    }

    public bool TryPlaceHeldToolAtPosition(Vector3 position, out string feedback)
    {
        if (!CanPlaceHeldToolAtPosition(position, out feedback))
            return false;
        var toolId = GetCounterPlacementToolId();
        var state = _tools[toolId];
        state.Location = ToolLocation.Counter;
        state.BoardSlot = -1;
        ClearHand(toolId);
        state.Node.ApplyWorldState(position, true);
        feedback = $"已将{state.Spec.DisplayName}放到瞄准的空余吧台位置；其他工具不能与它重合。";
        EmitHandsAndState(feedback);
        return true;
    }

    public bool CanPlaceLeftHandOnBoard(out string reason)
    {
        reason = string.Empty;
        if (string.IsNullOrEmpty(LeftHandToolId))
        {
            reason = "左手没有放置类工具。";
            return false;
        }
        if (_boardToolIds.Count >= 3)
        {
            reason = "砧板已经没有空余工具位。";
            return false;
        }
        var incoming = _tools[LeftHandToolId].Spec;
        foreach (var existingId in _boardToolIds)
        {
            if (ProcessRules.ToolsConflict(incoming, _tools[existingId].Spec))
            {
                reason = $"{incoming.DisplayName}与{_tools[existingId].Spec.DisplayName}属于冲突工具，不能同时放上砧板。";
                return false;
            }
        }
        return true;
    }

    public bool TryPlaceLeftHandOnBoard(Vector3[] boardPositions, out string feedback)
    {
        if (!CanPlaceLeftHandOnBoard(out feedback))
            return false;
        var toolId = LeftHandToolId;
        var state = _tools[toolId];
        var slot = Enumerable.Range(0, boardPositions.Length).First(index => _boardToolIds.All(id => _tools[id].BoardSlot != index));
        state.Location = ToolLocation.Workboard;
        state.BoardSlot = slot;
        _boardToolIds.Add(toolId);
        LeftHandToolId = string.Empty;
        state.Node.ApplyWorldState(boardPositions[slot], true);
        feedback = $"已先将{state.Spec.DisplayName}放上砧板。当前可实现：{GetBoardCapabilityText()}。";
        EmitHandsAndState(feedback);
        return true;
    }

    public bool CanDepositRightHandIngredientOnBoard(out string reason)
    {
        reason = string.Empty;
        if (_boardToolIds.Count == 0)
        {
            reason = "砧板上必须先有至少一种放置类工具，才能放入原材料。";
            return false;
        }
        if (string.IsNullOrEmpty(RightHandToolId) || _tools[RightHandToolId].Contents.Count == 0)
        {
            reason = "右手工具没有携带原材料。";
            return false;
        }
        if (_tools[RightHandToolId].ContentsAreWaste)
        {
            reason = "右手携带的是废品，请先倒入弃物桶。";
            return false;
        }
        if (!_boardToolIds.Any(id => _tools[id].Spec.CanContainIngredients))
        {
            reason = "砧板上的放置类工具都不能容纳原材料。";
            return false;
        }
        return true;
    }

    public bool TryDepositRightHandIngredientOnBoard(out string feedback)
    {
        if (!CanDepositRightHandIngredientOnBoard(out feedback))
            return false;
        var carrier = _tools[RightHandToolId];
        var target = _boardToolIds.Select(id => _tools[id]).First(state => state.Spec.CanContainIngredients);
        foreach (var pair in carrier.Contents)
        {
            target.Contents.TryGetValue(pair.Key, out var existing);
            target.Contents[pair.Key] = existing + pair.Value;
        }
        target.ContentCompletionRatio = Math.Min(target.ContentCompletionRatio, carrier.ContentCompletionRatio);
        var ingredientText = ContentText(carrier);
        carrier.ClearContents();
        feedback = $"已用{carrier.Spec.DisplayName}把{ingredientText}放入{target.Spec.DisplayName}；系统不会预先判断配方是否正确。";
        EmitHandsAndState(feedback);
        return true;
    }

    public bool TryCollectBoardIngredient(out string feedback)
    {
        feedback = string.Empty;
        if (string.IsNullOrEmpty(RightHandToolId))
        {
            feedback = "右手需要先拿一种可搬运原材料的手持工具。";
            return false;
        }
        var carrier = _tools[RightHandToolId];
        if (carrier.Contents.Count > 0)
        {
            feedback = $"{carrier.Spec.DisplayName}已经携带一种原材料，不能再拿另一种。";
            return false;
        }
        foreach (var source in _boardToolIds.Select(id => _tools[id]))
        {
            if (source.ContentsAreWaste || source.Contents.Count != 1)
                continue;
            var pair = source.Contents.First();
            if (!carrier.Spec.CanCarry(pair.Key))
                continue;
            carrier.Contents[pair.Key] = pair.Value;
            carrier.ContentCompletionRatio = source.ContentCompletionRatio;
            source.ClearContents();
            feedback = $"已用{carrier.Spec.DisplayName}从{source.Spec.DisplayName}取出{IngredientDisplay(pair.Key)}。";
            EmitHandsAndState(feedback);
            return true;
        }
        feedback = "砧板上没有可由当前右手工具搬运的原材料或中间产物。";
        return false;
    }

    public bool CanCollectBoardIngredient(out string reason)
    {
        reason = string.Empty;
        if (string.IsNullOrEmpty(RightHandToolId))
        {
            reason = "右手需要手持一种原料搬运工具。";
            return false;
        }
        var carrier = _tools[RightHandToolId];
        if (carrier.Contents.Count > 0)
        {
            reason = "右手工具已携带原材料。";
            return false;
        }
        var intermediateIds = _operations
            .Where(operation => operation.ResolveComplexity() != OperationComplexity.Simple && operation.ResultTargetToolId != "highball_glass")
            .SelectMany(operation => operation.Outputs.Keys)
            .ToHashSet(StringComparer.Ordinal);
        var available = _boardToolIds.Select(id => _tools[id]).Any(state =>
            !state.ContentsAreWaste && state.Contents.Count == 1 &&
            intermediateIds.Contains(state.Contents.Keys.First()) && carrier.Spec.CanCarry(state.Contents.Keys.First()));
        if (!available)
            reason = "当前右手工具无法搬运砧板上的中间产物。";
        return available;
    }

    public bool CanLoadIngredient(string ingredientId, out string reason)
    {
        reason = string.Empty;
        if (string.IsNullOrEmpty(RightHandToolId))
        {
            reason = $"必须先用右手拿取可搬运{IngredientDisplay(ingredientId)}的手持工具。";
            return false;
        }
        var carrier = _tools[RightHandToolId];
        if (!carrier.Spec.CanCarry(ingredientId))
        {
            reason = $"{carrier.Spec.DisplayName}无法在物理上携带{IngredientDisplay(ingredientId)}。";
            return false;
        }
        if (carrier.ContentsAreWaste)
        {
            reason = "右手工具里是废品，请先倒入弃物桶。";
            return false;
        }
        if (carrier.Contents.Count > 0 && !carrier.Contents.ContainsKey(ingredientId))
        {
            reason = $"一种手持工具一次只能携带一种原材料；当前已有{ContentText(carrier)}。";
            return false;
        }
        return true;
    }

    public bool TryLoadIngredient(string ingredientId, double amount, out string feedback, bool emitStatus = true)
    {
        if (!CanLoadIngredient(ingredientId, out feedback))
            return false;
        var carrier = _tools[RightHandToolId];
        carrier.Contents.TryGetValue(ingredientId, out var existing);
        carrier.Contents[ingredientId] = existing + Math.Max(0d, amount);
        feedback = $"{carrier.Spec.DisplayName}正在携带{IngredientDisplay(ingredientId)} {carrier.Contents[ingredientId]:0.00} 份；可继续取同类，但不能混拿其他原料。";
        EmitHandsAndState(feedback, emitStatus);
        return true;
    }

    public bool CanUseSimpleOperation =>
        !string.IsNullOrEmpty(LeftHandToolId) && !string.IsNullOrEmpty(RightHandToolId) &&
        _tools[RightHandToolId].Contents.Count > 0 &&
        _operations.Any(operation => operation.ResolveComplexity() == OperationComplexity.Simple &&
            operation.IsEnabledBy(new HashSet<string>(new[] { LeftHandToolId }, StringComparer.Ordinal)));

    public OperationResult TryUseSimpleOperation()
    {
        if (!CanUseSimpleOperation)
            return new OperationResult { Feedback = "当前双手组合无法进行简易工序；左手需持放置类工具，右手工具需携带原材料。" };

        var placementIds = new HashSet<string>(new[] { LeftHandToolId }, StringComparer.Ordinal);
        var carrier = _tools[RightHandToolId];
        var operation = SelectBestOperation(_operations.Where(candidate =>
            candidate.ResolveComplexity() == OperationComplexity.Simple && candidate.IsEnabledBy(placementIds)), carrier.Contents);
        if (operation is null)
            return new OperationResult { Feedback = "没有由当前左手工具支持的简易工序。" };

        var result = ProcessRules.Evaluate(operation, RightHandToolId, carrier.Contents, 1d, NextRoll());
        return ApplyAttempt(operation, result, new[] { carrier }, false);
    }

    public IReadOnlyList<OperationSpec> GetBoardCapabilities()
    {
        var ids = new HashSet<string>(_boardToolIds, StringComparer.Ordinal);
        return _operations.Where(operation => operation.ResolveComplexity() != OperationComplexity.Simple && operation.IsEnabledBy(ids)).ToArray();
    }

    public string GetBoardCapabilityText()
    {
        if (GetBoardTransitionHint() is { Length: > 0 } transition)
            return transition;
        var capabilities = GetBoardCapabilities();
        return capabilities.Count == 0
            ? "暂无工序"
            : string.Join(" / ", capabilities.Select(operation => $"{operation.DisplayName}（{ComplexityDisplay(operation.ResolveComplexity())}）"));
    }

    public OperationSpec? SelectBoardOperation()
    {
        var candidates = GetBoardCapabilities();
        if (candidates.Count == 0 || !_boardToolIds.Any(id => _tools[id].Contents.Count > 0))
            return null;
        return SelectBestOperation(candidates, null);
    }

    public string GetBoardAttemptWarning()
    {
        if (GetBoardTransitionHint() is { Length: > 0 } transition)
            return $"{transition}；再次执行当前工序仍被允许，但会因材料不匹配而报废。";
        var operation = SelectBoardOperation();
        if (operation is null)
            return string.Empty;
        var actual = MergeContents(GetOperationSourceStates(operation)).Where(pair => pair.Value > 0.000001d)
            .Select(pair => pair.Key).ToHashSet(StringComparer.Ordinal);
        var expected = operation.InputTargets.Keys.ToHashSet(StringComparer.Ordinal);
        return actual.SetEquals(expected)
            ? string.Empty
            : $"当前材料不匹配{operation.DisplayName}；仍可尝试，但会产生废品。";
    }

    public ProcessAttemptResult CompleteBoardOperation(OperationSpec operation, double action)
    {
        var sources = GetOperationSourceStates(operation);
        var ingredients = MergeContents(sources);
        var result = ProcessRules.Evaluate(operation, RightHandToolId, ingredients, action, NextRoll());
        ApplyAttempt(operation, result, sources, true);
        return result;
    }

    public bool TryDiscardHeldContents(out string feedback)
    {
        ToolRuntimeState? target = null;
        if (!string.IsNullOrEmpty(RightHandToolId) && _tools[RightHandToolId].Contents.Count > 0)
            target = _tools[RightHandToolId];
        else if (!string.IsNullOrEmpty(LeftHandToolId) && _tools[LeftHandToolId].Contents.Count > 0)
            target = _tools[LeftHandToolId];

        if (target is null)
        {
            feedback = "双手工具中没有可丢弃的原材料或废品；工具本身不会被扔掉。";
            return false;
        }

        var discarded = target.ContentAmount;
        if (target.Spec.Id == "highball_glass")
            Glass.Empty();
        target.ClearContents();
        TotalWaste += discarded;
        _snapshot.WastedAmount += discarded;
        feedback = $"已手动把{target.Spec.DisplayName}中的内容倒入弃物桶；工具仍拿在手中。";
        EmitHandsAndState(feedback);
        return true;
    }

    public bool CanDeliver => HasGlass && Glass.CurrentAmount > 0d;

    public void QueueAttemptRollForTests(double roll) => _nextAttemptRoll = Math.Clamp(roll, 0d, 1d);

    public void ResetForNewDay()
    {
        _snapshot.CompletedSteps.Clear();
        _snapshot.IngredientAmounts.Clear();
        _snapshot.WastedAmount = 0d;
        _snapshot.SpilledAmount = 0d;
        _snapshot.ElapsedSeconds = 0d;
        _snapshot.CraftCompletionRatio = 1d;
        _snapshot.FailedOperations = 0;
        Glass = new LiquidContainer(3d);
        _timing = false;
        _boardToolIds.Clear();
        LeftHandToolId = string.Empty;
        RightHandToolId = string.Empty;
        foreach (var state in _tools.Values)
        {
            state.ClearContents();
            state.Location = ToolLocation.Counter;
            state.BoardSlot = -1;
            state.Node.ApplyWorldState(state.InitialPosition, true);
        }
        EmitHandsAndState(string.Empty, false);
    }

    public DrinkEvaluation EvaluateAndFinish()
    {
        _timing = false;
        var evaluation = RecipeEvaluator.Evaluate(_recipeTargets, _snapshot);
        if (GameSession.Instance.BeginDelivery())
            GameSession.Instance.FinishEvaluation(evaluation);
        return evaluation;
    }

    public string GetDebugText()
    {
        var board = _boardToolIds.Count == 0
            ? "空"
            : string.Join("+", _boardToolIds.Select(id => _tools[id].Spec.DisplayName));
        return $"左手:{LeftHandDisplayName}｜右手:{RightHandDisplayName}｜砧板:{board} [{GetBoardCapabilityText()}]｜杯量:{Glass.CurrentAmount:0.00}/3.00｜完成度:{_snapshot.CraftCompletionRatio:P0}｜失败:{_snapshot.FailedOperations}｜浪费:{TotalWaste:0.00}";
    }

    private OperationResult ApplyAttempt(OperationSpec operation, ProcessAttemptResult result,
        IEnumerable<ToolRuntimeState> sourceStates, bool boardOperation)
    {
        var sources = sourceStates.Distinct().ToArray();
        var inheritedCompletion = sources.Where(state => state.Contents.Count > 0)
            .Select(state => state.ContentCompletionRatio)
            .DefaultIfEmpty(1d)
            .Min();
        var outputCompletion = Math.Min(inheritedCompletion, result.CompletionRatio);
        string feedback;
        if (result.Completed)
        {
            foreach (var source in sources)
            {
                if (source.Spec.Id == "highball_glass")
                    Glass.Empty();
                source.ClearContents();
            }
            if (_tools.TryGetValue(operation.ResultTargetToolId, out var target))
            {
                foreach (var output in operation.Outputs)
                    AddOutput(target, output.Key, output.Value, outputCompletion);
            }
            _snapshot.CompletedSteps.Add(operation.Id);
            _snapshot.CraftCompletionRatio = Math.Min(_snapshot.CraftCompletionRatio, outputCompletion);
            feedback = $"{operation.DisplayName}成功｜成品链完成度 {outputCompletion:P0}｜本次成功率 {result.SuccessProbability:P0}";
        }
        else if (result.Failure == ProcessFailure.InsufficientAction)
        {
            feedback = $"{operation.DisplayName}尚未完成；材料未报废，可继续操作。";
        }
        else
        {
            foreach (var source in sources.Where(state => state.Contents.Count > 0))
                source.ContentsAreWaste = true;
            _snapshot.FailedOperations++;
            feedback = result.Failure switch
            {
                ProcessFailure.WrongHandheldTool => $"{operation.DisplayName}失败：右手工具不正确，原材料已成为废品；请手动拿起容器并倒入弃物桶。",
                ProcessFailure.WrongIngredients => $"{operation.DisplayName}失败：原材料种类不符合任何对应配方，已成为废品；请手动清理。",
                ProcessFailure.ProportionCheckFailed => $"{operation.DisplayName}失败：比例偏离导致成功率仅 {result.SuccessProbability:P0}，本次鉴定未通过，材料已报废。",
                _ => $"{operation.DisplayName}失败，材料已成为废品。"
            };
        }
        EmitHandsAndState(feedback);
        LastOperationFeedback = feedback;
        LastProcessResult = result;
        return new OperationResult
        {
            Completed = result.Completed,
            Intensity = outputCompletion,
            Feedback = feedback
        };
    }

    private void AddOutput(ToolRuntimeState target, string ingredientId, double amount, double completion)
    {
        var accepted = Math.Max(0d, amount);
        if (target.Spec.Id == "highball_glass")
        {
            var spillBefore = Glass.SpilledAmount;
            accepted = Glass.Add(ingredientId, amount);
            _snapshot.SpilledAmount += Glass.SpilledAmount - spillBefore;
            _snapshot.IngredientAmounts.TryGetValue(ingredientId, out var existingSnapshot);
            _snapshot.IngredientAmounts[ingredientId] = existingSnapshot + accepted;
        }
        target.Contents.TryGetValue(ingredientId, out var existing);
        target.Contents[ingredientId] = existing + accepted;
        target.ContentCompletionRatio = Math.Min(target.ContentCompletionRatio, completion);
    }

    private List<ToolRuntimeState> GetOperationSourceStates(OperationSpec operation)
    {
        var states = _boardToolIds.Select(id => _tools[id]).Where(state => state.Contents.Count > 0).ToList();
        if (states.Count > 1 && states.Any(state => state.Spec.Id == operation.ResultTargetToolId))
        {
            var nonTargetHasInput = states.Where(state => state.Spec.Id != operation.ResultTargetToolId)
                .SelectMany(state => state.Contents.Keys).Any(operation.InputTargets.ContainsKey);
            if (nonTargetHasInput)
                states.RemoveAll(state => state.Spec.Id == operation.ResultTargetToolId);
        }
        return states;
    }

    private OperationSpec? SelectBestOperation(IEnumerable<OperationSpec> candidates, IReadOnlyDictionary<string, double>? directContents)
    {
        return candidates
            .Select(operation =>
            {
                var contents = directContents ?? MergeContents(GetOperationSourceStates(operation));
                var actual = contents.Where(pair => pair.Value > 0.000001d).Select(pair => pair.Key).ToHashSet(StringComparer.Ordinal);
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
    }

    private string GetBoardTransitionHint()
    {
        if (_boardToolIds.Count == 0)
            return string.Empty;
        var contents = MergeContents(_boardToolIds.Select(id => _tools[id]).Where(state => !state.ContentsAreWaste));
        var actual = contents.Where(pair => pair.Value > 0.000001d).Select(pair => pair.Key).ToHashSet(StringComparer.Ordinal);
        if (actual.Count == 0)
            return string.Empty;
        var placementIds = new HashSet<string>(_boardToolIds, StringComparer.Ordinal);
        var next = _operations.FirstOrDefault(operation => operation.ResolveComplexity() != OperationComplexity.Simple &&
            !operation.IsEnabledBy(placementIds) && actual.SetEquals(operation.InputTargets.Keys));
        if (next is null)
            return string.Empty;
        var missing = next.RequiredPlacementToolIds.Where(id => !placementIds.Contains(id))
            .Select(id => _tools.TryGetValue(id, out var state) ? state.Spec.DisplayName : id);
        return $"中间产物已完成；加入{string.Join("＋", missing)}后可{next.DisplayName}";
    }

    private static Dictionary<string, double> MergeContents(IEnumerable<ToolRuntimeState> states)
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

    private void ClearHand(string toolId)
    {
        if (LeftHandToolId == toolId)
            LeftHandToolId = string.Empty;
        if (RightHandToolId == toolId)
            RightHandToolId = string.Empty;
    }

    private string GetCounterPlacementToolId() =>
        !string.IsNullOrEmpty(LeftHandToolId) ? LeftHandToolId : RightHandToolId;

    private string HandDisplay(string toolId, bool includePayload = false)
    {
        if (string.IsNullOrEmpty(toolId) || !_tools.TryGetValue(toolId, out var state))
            return "空";
        var suffix = includePayload && state.Contents.Count > 0
            ? $"（{(state.ContentsAreWaste ? "废品:" : string.Empty)}{ContentText(state)}）"
            : state.ContentsAreWaste ? "（含废品）" : string.Empty;
        return state.Spec.DisplayName + suffix;
    }

    private void EmitHandsAndState(string status, bool emitStatus = true)
    {
        EmitSignal(SignalName.HandsChanged, LeftHandDisplayName, RightHandDisplayName);
        EmitSignal(SignalName.HandToolIdsChanged, LeftHandToolId, RightHandToolId);
        EmitSignal(SignalName.DrinkChanged, GetDebugText());
        if (emitStatus && !string.IsNullOrWhiteSpace(status))
            GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, status);
    }

    private double NextRoll()
    {
        if (_nextAttemptRoll is { } value)
        {
            _nextAttemptRoll = null;
            return value;
        }
        return _random.Randf();
    }

    private static string ContentText(ToolRuntimeState state) =>
        state.Contents.Count == 0
            ? "空"
            : string.Join("+", state.Contents.Select(pair => $"{IngredientDisplay(pair.Key)} {pair.Value:0.00}"));

    private static string IngredientDisplay(string ingredientId) => ingredientId switch
    {
        "coffee_beans" => "咖啡豆",
        "ground_coffee" => "咖啡粉",
        "water" => "水",
        "ice" => "冰块",
        "coffee_extract" => "咖啡萃取液",
        "espresso" => "过滤咖啡液",
        _ => ingredientId
    };

    private static string ComplexityDisplay(OperationComplexity complexity) => complexity switch
    {
        OperationComplexity.Simple => "简易",
        OperationComplexity.Normal => "普通",
        OperationComplexity.Complex => "复杂",
        _ => "自动"
    };

    private void OnPhaseChanged(int phase) => _timing = (DayPhase)phase == DayPhase.Preparation;
}
