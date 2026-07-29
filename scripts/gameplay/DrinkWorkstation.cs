using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

public partial class DrinkWorkstation : Node
{
    public const double PrototypeHygienePenalty = 0.04d;
    public const double PrototypeKettleCapacityMl = 1600d;

    [Signal] public delegate void DrinkChangedEventHandler(string debugText);
    [Signal] public delegate void HandsChangedEventHandler(string leftHand, string rightHand);
    [Signal] public delegate void HandToolIdsChangedEventHandler(string leftToolId, string rightToolId);

    private readonly Dictionary<string, ToolSpec> _toolSpecs = new(StringComparer.Ordinal);
    private readonly ToolInventoryService _inventory = new();
    private readonly DrinkAssemblyState _assembly = new(300d);
    private readonly ProcessExecutionService _processes;
    private readonly Dictionary<string, ToolPresentationBinding> _toolPresentations = new(StringComparer.Ordinal);
    private readonly RandomNumberGenerator _random = new();
    private RecipeTargets _recipeTargets = new() { IsPrototype = true };
    private bool _timing;
    private double? _nextAttemptRoll;

    public DrinkWorkstation()
    {
        _processes = new ProcessExecutionService(_inventory, _assembly);
    }

    public LiquidContainer Glass => _assembly.Glass;
    public string LeftHandToolId => _inventory.LeftHandToolId;
    public string RightHandToolId => _inventory.RightHandToolId;
    public bool HasHeldTool => _inventory.HasHeldTool;
    public bool HasGlass => string.Equals(LeftHandToolId, "highball_glass", StringComparison.Ordinal);
    public int IcePieces => (int)Math.Round(Glass.Ingredients.TryGetValue("ice", out var ice) ? ice : 0d);
    public double TotalWaste => _assembly.WastedAmount;
    public bool HandsWashedToday { get; private set; }
    public double KettleWaterAmountMl { get; private set; } = PrototypeKettleCapacityMl;
    public int BoardToolCount => _inventory.BoardToolIds.Count;
    public string LastOperationFeedback { get; private set; } = string.Empty;
    public ProcessAttemptResult? LastProcessResult { get; private set; }
    public string RecipeId => _recipeTargets.Id;
    public string LeftHandDisplayName => HandDisplay(LeftHandToolId);
    public string RightHandDisplayName => HandDisplay(RightHandToolId, true);
    public string CounterPlacementDisplayName => HandDisplay(_inventory.GetCounterPlacementToolId(), true);
    public double SuccessProbabilityPenalty => HandsWashedToday ? 0d : PrototypeHygienePenalty;
    public bool RightHandHasDualMeasure => !string.IsNullOrEmpty(RightHandToolId) &&
                                           _inventory.GetRequiredTool(RightHandToolId).Definition.HasDualMeasure;
    public double RightHandMeasureAmount => RightHandHasDualMeasure
            ? (_inventory.GetRequiredTool(RightHandToolId).UseLargeMeasureSide
            ? _inventory.GetRequiredTool(RightHandToolId).Definition.LargeMeasureAmount
            : _inventory.GetRequiredTool(RightHandToolId).Definition.SmallMeasureAmount)
        : 0d;
    public string RightHandMeasureSideName => RightHandHasDualMeasure &&
                                              _inventory.GetRequiredTool(RightHandToolId).UseLargeMeasureSide
        ? "大头"
        : "小头";

    public double GetRightHandIngredientAmount(string ingredientId) =>
        !string.IsNullOrEmpty(RightHandToolId) &&
        _inventory.GetRequiredTool(RightHandToolId).Contents.TryGetValue(ingredientId, out var amount)
            ? amount
            : 0d;

    public bool WashHands(out string feedback)
    {
        if (HandsWashedToday)
        {
            feedback = "今天已经洗过手；卫生状态保持正常。";
            return false;
        }
        HandsWashedToday = true;
        feedback = "已在水槽洗手；今天后续工序不再承受 4% 开发占位成功率惩罚。";
        EmitHandsAndState(feedback);
        return true;
    }

    public bool ToggleRightHandMeasureSide(out string feedback)
    {
        if (!RightHandHasDualMeasure)
        {
            feedback = "右手需要拿着一种双头量酒器才能切换量杯端。";
            return false;
        }
        var state = _inventory.GetRequiredTool(RightHandToolId);
        if (state.Contents.Count > 0)
        {
            feedback = $"{state.Definition.DisplayName}里已有{ContentText(state)}；倒出后才能翻转选择另一端重新计量。";
            return false;
        }
        state.UseLargeMeasureSide = !state.UseLargeMeasureSide;
        feedback = $"已切换为{state.Definition.DisplayName}的{RightHandMeasureSideName}：{RightHandMeasureAmount:0} ml（开发占位容量）。";
        EmitHandsAndState(feedback);
        return true;
    }

    public bool CanFillRightHandFromKettle(out string reason)
    {
        reason = string.Empty;
        if (!RightHandHasDualMeasure)
        {
            reason = "必须先用右手拿一种双头量酒器；水壶不再直接倒入制作容器。";
            return false;
        }
        var state = _inventory.GetRequiredTool(RightHandToolId);
        if (state.Contents.Count > 0)
        {
            reason = $"{state.Definition.DisplayName}里已有{ContentText(state)}，先倒出后才能重新计量。";
            return false;
        }
        if (KettleWaterAmountMl <= 0.000001d)
        {
            reason = "水壶无水，量酒器无法接水；当前萃取缺水原因就是水壶已空。";
            return false;
        }
        if (KettleWaterAmountMl + 0.000001d < RightHandMeasureAmount)
        {
            reason = $"水壶只剩 {KettleWaterAmountMl:0} ml，不足以装满当前{RightHandMeasureSideName} {RightHandMeasureAmount:0} ml。";
            return false;
        }
        return true;
    }

    public bool TryFillRightHandFromKettle(out string feedback)
    {
        if (!CanFillRightHandFromKettle(out feedback))
            return false;
        var amount = RightHandMeasureAmount;
        var state = _inventory.GetRequiredTool(RightHandToolId);
        state.Contents["water"] = amount;
        KettleWaterAmountMl -= amount;
        feedback = $"已用{state.Definition.DisplayName}{RightHandMeasureSideName}从水壶接取 {amount:0} ml 水；水壶剩余 {KettleWaterAmountMl:0} ml。";
        EmitHandsAndState(feedback);
        return true;
    }

    public ToolLocation GetToolLocation(string toolId) => _inventory.GetRequiredTool(toolId).Location;
    public bool IsToolContentWaste(string toolId) => _inventory.GetRequiredTool(toolId).ContentsAreWaste;
    public double GetToolContentAmount(string toolId, string ingredientId) =>
        _inventory.GetRequiredTool(toolId).Contents.TryGetValue(ingredientId, out var amount) ? amount : 0d;
    public double GetToolContentCompletionRatio(string toolId) =>
        _inventory.GetRequiredTool(toolId).ContentCompletionRatio;
    public bool IsToolOnBoard(string toolId) => _inventory.BoardToolIds.Contains(toolId);
    public double DrinkCompletionRatio => _inventory.Tools.TryGetValue("highball_glass", out var glass)
        ? glass.ContentCompletionRatio
        : 1d;

    public override void _Ready()
    {
        _random.Randomize();
        var recipe = ResourceLoader.Load<RecipeDefinition>("res://data/recipes/prototype_iced_americano.tres");
        if (recipe is null)
            throw new InvalidOperationException("Prototype recipe resource could not be loaded.");
        _recipeTargets = recipe.BuildTargets();
        GameplayCatalogValidator.ValidateRecipeCompatibility(_recipeTargets, _processes.Operations);
        GameSession.Instance.DayPhaseChanged += OnPhaseChanged;
    }

    public override void _Process(double delta)
    {
        if (_timing)
            _assembly.AdvanceElapsed(delta);
    }

    public void ConfigureCatalog(GameplayCatalogDefinition catalog)
    {
        var validated = catalog.BuildValidatedSpecs();
        _toolSpecs.Clear();
        foreach (var pair in validated.Tools)
            _toolSpecs.Add(pair.Key, pair.Value);
        _processes.ConfigureOperations(validated.Operations);
    }

    public ToolSpec GetToolSpec(string toolId) =>
        _toolSpecs.TryGetValue(toolId, out var spec)
            ? spec
            : throw new InvalidOperationException($"Unknown tool ID: {toolId}");

    public OperationComplexity GetOperationComplexity(string operationId) =>
        _processes.Operations.First(operation => operation.Id == operationId).ResolveComplexity();

    public void RegisterTool(ToolInteractable node, string toolId, Vector3 initialPosition)
    {
        var spec = GetToolSpec(toolId);
        _inventory.RegisterTool(spec, ToSpatialPosition(initialPosition));
        _toolPresentations.Add(toolId, new ToolPresentationBinding
        {
            Node = node
        });
        node.ApplyWorldState(initialPosition, true);
    }

    public bool CanPickUpTool(string toolId)
    {
        return _inventory.CheckPickUp(toolId).Allowed;
    }

    public bool TryPickUpTool(string toolId)
    {
        if (!_inventory.CheckPickUp(toolId).Allowed)
            return false;

        var state = _inventory.PickUp(toolId);
        var presentation = _toolPresentations[toolId];
        presentation.Node.ApplyWorldState(presentation.Node.GlobalPosition, false);
        EmitHandsAndState($"已将{state.Definition.DisplayName}拿到{(state.Definition.ResolveCategory() == ToolCategory.Placement ? "左手" : "右手")}。原位置现在为空。");
        return true;
    }

    public bool CanPlaceHeldToolAtPosition(Vector3 position, out string reason)
    {
        reason = string.Empty;
        var check = _inventory.CheckCounterPlacement(ToSpatialPosition(position));
        if (check.Allowed)
            return true;
        switch (check.Failure)
        {
            case ToolInventoryFailure.LoadedHandheldCannotBePlaced:
                var loaded = _inventory.GetRequiredTool(check.ToolId);
                reason = $"{loaded.Definition.DisplayName}还装有{ContentText(loaded)}，不能直接搁在台面；先完成转移或倒入弃物桶。";
                break;
            case ToolInventoryFailure.CounterOverlap:
                var existing = _inventory.GetRequiredTool(check.RelatedToolId);
                reason = $"此处会与{existing.Definition.DisplayName}重合，请瞄准其他空余位置。";
                break;
            default:
                reason = "双手没有可放置的工具。";
                break;
        }
        return false;
    }

    public bool TryPlaceHeldToolAtPosition(Vector3 position, out string feedback)
    {
        if (!CanPlaceHeldToolAtPosition(position, out feedback))
            return false;
        var state = _inventory.PlaceHeldToolAt(ToSpatialPosition(position));
        _toolPresentations[state.Id].Node.ApplyWorldState(position, true);
        feedback = $"已将{state.Definition.DisplayName}放到瞄准的空余吧台位置；其他工具不能与它重合。";
        EmitHandsAndState(feedback);
        return true;
    }

    public bool CanPlaceLeftHandOnBoard(out string reason)
    {
        reason = string.Empty;
        var check = _inventory.CheckBoardPlacement();
        if (check.Allowed)
            return true;
        switch (check.Failure)
        {
            case ToolInventoryFailure.BoardFull:
                reason = "砧板已经没有空余工具位。";
                break;
            case ToolInventoryFailure.BoardConflict:
                var incoming = _inventory.GetRequiredTool(check.ToolId);
                var existing = _inventory.GetRequiredTool(check.RelatedToolId);
                reason = $"{incoming.Definition.DisplayName}与{existing.Definition.DisplayName}属于冲突工具，不能同时放上砧板。";
                break;
            default:
                reason = "左手没有放置类工具。";
                break;
        }
        return false;
    }

    public bool TryPlaceLeftHandOnBoard(Vector3[] boardPositions, out string feedback)
    {
        if (!CanPlaceLeftHandOnBoard(out feedback))
            return false;
        var positions = boardPositions.Select(ToSpatialPosition).ToArray();
        var state = _inventory.PlaceLeftHandOnBoard(positions);
        _toolPresentations[state.Id].Node.ApplyWorldState(boardPositions[state.BoardSlot], true);
        feedback = $"已先将{state.Definition.DisplayName}放上砧板。当前可实现：{GetBoardCapabilityText()}。";
        EmitHandsAndState(feedback);
        return true;
    }

    public bool CanDepositRightHandIngredientOnBoard(out string reason)
    {
        reason = string.Empty;
        var check = _inventory.CheckDepositRightHandContentsOnBoard();
        if (check.Allowed)
            return true;
        reason = check.Failure switch
        {
            ToolInventoryFailure.NoBoardContainer when _inventory.BoardToolIds.Count == 0 =>
                "砧板上必须先有至少一种放置类工具，才能放入原材料。",
            ToolInventoryFailure.NoBoardContainer => "砧板上的放置类工具都不能容纳原材料。",
            ToolInventoryFailure.RightHandContentsAreWaste => "右手携带的是废品，请先倒入弃物桶。",
            _ => "右手工具没有携带原材料。"
        };
        return false;
    }

    public bool TryDepositRightHandIngredientOnBoard(out string feedback)
    {
        if (!CanDepositRightHandIngredientOnBoard(out feedback))
            return false;
        var carrier = _inventory.GetRequiredTool(RightHandToolId);
        var ingredientText = ContentText(carrier);
        var transfer = _inventory.DepositRightHandContentsOnBoard();
        feedback = $"已用{transfer.Source.Definition.DisplayName}把{ingredientText}放入{transfer.Target.Definition.DisplayName}；系统不会预先判断配方是否正确。";
        EmitHandsAndState(feedback);
        return true;
    }

    public bool TryCollectBoardIngredient(out string feedback)
    {
        feedback = string.Empty;
        var check = _inventory.CheckCollectBoardContents();
        if (!check.Allowed)
        {
            feedback = check.Failure == ToolInventoryFailure.NoRightHandTool
                ? "右手需要先拿一种可搬运原材料的手持工具。"
                : check.Failure == ToolInventoryFailure.CarrierAlreadyLoaded
                    ? $"{_inventory.GetRequiredTool(check.ToolId).Definition.DisplayName}已经携带一种原材料，不能再拿另一种。"
                    : "砧板上没有可由当前右手工具搬运的原材料或中间产物。";
            return false;
        }
        var transfer = _inventory.CollectBoardContents();
        feedback = $"已用{transfer.Target.Definition.DisplayName}从{transfer.Source.Definition.DisplayName}取出{IngredientDisplay(transfer.IngredientId)}。";
        EmitHandsAndState(feedback);
        return true;
    }

    public bool CanCollectBoardIngredient(out string reason)
    {
        reason = string.Empty;
        var intermediateIds = _processes.Operations
            .Where(operation => operation.ResolveComplexity() != OperationComplexity.Simple && operation.ResultTargetToolId != "highball_glass")
            .SelectMany(operation => operation.Outputs.Keys)
            .ToHashSet(StringComparer.Ordinal);
        var check = _inventory.CheckCollectBoardContents(intermediateIds);
        if (check.Allowed)
            return true;
        reason = check.Failure switch
        {
            ToolInventoryFailure.NoRightHandTool => "右手需要手持一种原料搬运工具。",
            ToolInventoryFailure.CarrierAlreadyLoaded => "右手工具已携带原材料。",
            _ => "当前右手工具无法搬运砧板上的中间产物。"
        };
        return false;
    }

    public bool CanLoadIngredient(string ingredientId, out string reason)
    {
        reason = string.Empty;
        var check = _inventory.CheckLoadIngredient(ingredientId);
        if (check.Allowed)
            return true;
        reason = check.Failure switch
        {
            ToolInventoryFailure.NoRightHandTool =>
                $"必须先用右手拿取可搬运{IngredientDisplay(ingredientId)}的手持工具。",
            ToolInventoryFailure.ToolCannotCarryIngredient =>
                $"{_inventory.GetRequiredTool(check.ToolId).Definition.DisplayName}无法在物理上携带{IngredientDisplay(ingredientId)}。",
            ToolInventoryFailure.RightHandContentsAreWaste => "右手工具里是废品，请先倒入弃物桶。",
            ToolInventoryFailure.CarrierContainsDifferentIngredient =>
                $"一种手持工具一次只能携带一种原材料；当前已有{ContentText(_inventory.GetRequiredTool(check.ToolId))}。",
            _ => "当前工具无法装载该原材料。"
        };
        return false;
    }

    public bool TryLoadIngredient(string ingredientId, double amount, out string feedback, bool emitStatus = true)
    {
        if (!CanLoadIngredient(ingredientId, out feedback))
            return false;
        var carrier = _inventory.LoadIngredient(ingredientId, amount);
        feedback = $"{carrier.Definition.DisplayName}正在携带{IngredientAmountText(ingredientId, carrier.Contents[ingredientId])}；可继续取同类，但不能混拿其他原料。";
        EmitHandsAndState(feedback, emitStatus);
        return true;
    }

    public bool CanUseSimpleOperation => _processes.CanUseSimpleOperation;

    public OperationResult TryUseSimpleOperation()
    {
        if (!CanUseSimpleOperation)
            return new OperationResult { Feedback = "当前双手组合无法进行简易工序；左手需持放置类工具，右手工具需携带原材料。" };

        var outcome = _processes.ExecuteSimpleOperation(
            NextRoll,
            SuccessProbabilityPenalty);
        if (outcome is null)
            return new OperationResult { Feedback = "没有由当前左手工具支持的简易工序。" };

        return PublishProcessOutcome(outcome);
    }

    public IReadOnlyList<OperationSpec> GetBoardCapabilities() => _processes.GetBoardCapabilities();

    public string GetBoardCapabilityText()
    {
        var capabilities = GetBoardCapabilities();
        var recovery = capabilities.FirstOrDefault(operation =>
            _processes.TryGetRepeatRecoveryTarget(operation, out _));
        var transition = FormatTransitionHint(_processes.GetBoardTransitionHint());
        if (recovery is not null)
            return string.IsNullOrEmpty(transition)
                ? $"可重复{recovery.DisplayName}，有限恢复工序完成度"
                : $"可重复{recovery.DisplayName}有限补救 / {transition}";
        if (!string.IsNullOrEmpty(transition))
            return transition;
        return capabilities.Count == 0 ? "暂无工序" : string.Join(" / ",
            capabilities.Select(operation => $"{operation.DisplayName}（{ComplexityDisplay(operation.ResolveComplexity())}）"));
    }

    public OperationSpec? SelectBoardOperation() => _processes.SelectBoardOperation();

    public string GetBoardAttemptWarning()
    {
        var operation = SelectBoardOperation();
        if (operation is null)
            return string.Empty;
        if (_processes.TryGetRepeatRecoveryTarget(operation, out var recoveryTarget))
            return $"重复{operation.DisplayName}可有限恢复{recoveryTarget.ContentCompletionRatio:P0}完成度，开发占位上限 {operation.RepeatRecoveryCap:P0}。";
        if (FormatTransitionHint(_processes.GetBoardTransitionHint()) is { Length: > 0 } transition)
            return transition;
        return _processes.OperationInputsMatch(operation)
            ? string.Empty
            : $"当前材料不匹配{operation.DisplayName}；仍可尝试，但会产生废品。";
    }

    public ProcessAttemptResult CompleteBoardOperation(OperationSpec operation, double action)
    {
        var outcome = _processes.ExecuteBoardOperation(
            operation,
            action,
            NextRoll,
            SuccessProbabilityPenalty,
            KettleWaterAmountMl > 0.000001d);
        PublishProcessOutcome(outcome);
        return outcome.Attempt;
    }

    public bool TryDiscardHeldContents(out string feedback)
    {
        ToolInstanceState? target = null;
        if (!string.IsNullOrEmpty(RightHandToolId) &&
            _inventory.GetRequiredTool(RightHandToolId).Contents.Count > 0)
            target = _inventory.GetRequiredTool(RightHandToolId);
        else if (!string.IsNullOrEmpty(LeftHandToolId) &&
                 _inventory.GetRequiredTool(LeftHandToolId).Contents.Count > 0)
            target = _inventory.GetRequiredTool(LeftHandToolId);

        if (target is null)
        {
            feedback = "双手工具中没有可丢弃的原材料或废品；工具本身不会被扔掉。";
            return false;
        }

        _assembly.DiscardToolContents(target);
        feedback = $"已手动把{target.Definition.DisplayName}中的内容倒入弃物桶；工具仍拿在手中。";
        EmitHandsAndState(feedback);
        return true;
    }

    public bool CanDeliver => HasGlass && Glass.CurrentAmount > 0d;

    public void QueueAttemptRollForTests(double roll) => _nextAttemptRoll = Math.Clamp(roll, 0d, 1d);
    public void SetKettleWaterForTests(double amountMl) => KettleWaterAmountMl = Math.Clamp(amountMl, 0d, PrototypeKettleCapacityMl);

    public void ResetForNewDay()
    {
        _assembly.ResetForNewDay(300d);
        _timing = false;
        _nextAttemptRoll = null;
        LastOperationFeedback = string.Empty;
        LastProcessResult = null;
        HandsWashedToday = false;
        KettleWaterAmountMl = PrototypeKettleCapacityMl;
        _processes.Reset();
        _inventory.ResetAll();
        foreach (var state in _inventory.Tools.Values)
        {
            var presentation = _toolPresentations[state.Id];
            presentation.Node.ApplyWorldState(ToVector3(state.Position), true);
        }
        EmitHandsAndState(string.Empty, false);
    }

    public DrinkEvaluation EvaluateAndFinish()
    {
        _timing = false;
        var evaluation = EvaluateCurrentDrink();
        if (GameSession.Instance.BeginDelivery())
            GameSession.Instance.FinishEvaluation(evaluation);
        return evaluation;
    }

    public DrinkEvaluation EvaluateCurrentDrink()
    {
        return _assembly.Evaluate(_recipeTargets, DrinkCompletionRatio);
    }

    public string GetDebugText()
    {
        var board = _inventory.BoardToolIds.Count == 0
            ? "空"
            : string.Join("+", _inventory.BoardToolIds.Select(id => _inventory.GetRequiredTool(id).Definition.DisplayName));
        var measure = RightHandHasDualMeasure ? $"｜量酒器:{RightHandMeasureSideName} {RightHandMeasureAmount:0} ml" : string.Empty;
        return $"左手:{LeftHandDisplayName}｜右手:{RightHandDisplayName}{measure}｜洗手:{(HandsWashedToday ? "已完成" : "未完成(-4%)")}｜水壶:{KettleWaterAmountMl:0} ml｜砧板:{board} [{GetBoardCapabilityText()}]｜杯量:{Glass.CurrentAmount:0.0}/300 ml｜完成度:{DrinkCompletionRatio:P0}｜失败:{_assembly.FailedOperations}｜浪费:{TotalWaste:0.00}";
    }

    private OperationResult PublishProcessOutcome(ProcessExecutionOutcome outcome)
    {
        var feedback = FormatProcessFeedback(outcome);
        LastOperationFeedback = feedback;
        LastProcessResult = outcome.Attempt;
        EmitHandsAndState(feedback);
        return new OperationResult
        {
            Completed = outcome.Attempt.Completed,
            Intensity = outcome.OutputCompletion,
            Feedback = feedback
        };
    }

    private static string FormatProcessFeedback(ProcessExecutionOutcome outcome)
    {
        var operation = outcome.Operation;
        if (outcome.Kind == ProcessExecutionKind.NonDestructiveBlock)
        {
            return outcome.BlockReason switch
            {
                ProcessBlockReason.KettleEmpty =>
                    "萃取尚未开始：水壶无水，无法用量酒器给滤具加水；咖啡粉保持可用，没有被误判为废品。",
                ProcessBlockReason.MissingMeasuredWater =>
                    "萃取尚未开始：滤具里缺水；先用双头量酒器从水壶接水并倒入滤具。咖啡粉保持可用。",
                ProcessBlockReason.RepeatActionIncomplete =>
                    $"重复{operation.DisplayName}尚未完成；继续操作可尝试补救，材料保持可用。",
                _ => $"{operation.DisplayName}尚未开始；材料保持可用。"
            };
        }

        if (outcome.Kind == ProcessExecutionKind.RepeatRecovery)
        {
            return outcome.FullRecovery
                ? $"重复{operation.DisplayName}完成：已有限恢复到 {outcome.Attempt.CompletionRatio:P0}，开发占位上限 {operation.RepeatRecoveryCap:P0}，不会抹平全部损失。"
                : $"重复{operation.DisplayName}出现偏差：仅少量恢复到 {outcome.Attempt.CompletionRatio:P0}；仍可继续补救但上限不变。";
        }

        if (outcome.Kind == ProcessExecutionKind.Completed)
            return $"{operation.DisplayName}成功｜成品链完成度 {outcome.OutputCompletion:P0}｜本次成功率 {outcome.Attempt.SuccessProbability:P0}";
        if (outcome.Kind == ProcessExecutionKind.InsufficientAction)
            return $"{operation.DisplayName}尚未完成；材料未报废，可继续操作。";

        return outcome.Attempt.Failure switch
        {
            ProcessFailure.WrongHandheldTool =>
                $"{operation.DisplayName}失败：右手工具不正确，原材料已成为废品；请手动拿起容器并倒入弃物桶。",
            ProcessFailure.WrongIngredients =>
                $"{operation.DisplayName}失败：原材料种类不符合任何对应配方，已成为废品；请手动清理。",
            ProcessFailure.ProportionCheckFailed =>
                $"{operation.DisplayName}失败：比例偏离导致成功率仅 {outcome.Attempt.SuccessProbability:P0}，本次鉴定未通过，材料已报废。",
            _ => $"{operation.DisplayName}失败，材料已成为废品。"
        };
    }

    private string FormatTransitionHint(ProcessTransitionHint? hint)
    {
        if (hint is null)
            return string.Empty;
        var missing = hint.MissingPlacementToolIds
            .Select(id => _inventory.Tools.TryGetValue(id, out var state)
                ? state.Definition.DisplayName
                : id);
        return $"中间产物已完成；加入{string.Join("＋", missing)}后可{hint.Operation.DisplayName}";
    }

    private static SpatialPosition ToSpatialPosition(Vector3 value) => new(value.X, value.Y, value.Z);

    private static Vector3 ToVector3(SpatialPosition value) => new((float)value.X, (float)value.Y, (float)value.Z);

    private string HandDisplay(string toolId, bool includePayload = false)
    {
        if (string.IsNullOrEmpty(toolId) || !_inventory.Tools.TryGetValue(toolId, out var state))
            return "空";
        var suffix = includePayload && state.Contents.Count > 0
            ? $"（{(state.ContentsAreWaste ? "废品:" : string.Empty)}{ContentText(state)}）"
            : state.ContentsAreWaste ? "（含废品）" : string.Empty;
        return state.Definition.DisplayName + suffix;
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

    private static string ContentText(ToolInstanceState state) => state.Contents.Count == 0
        ? "空"
        : string.Join("+", state.Contents.Select(pair => IngredientAmountText(pair.Key, pair.Value)));

    private static string IngredientAmountText(string ingredientId, double amount) => ingredientId switch
    {
        "water" or "coffee_extract" or "espresso" => $"{IngredientDisplay(ingredientId)} {amount:0.#} ml",
        "ice" => $"{IngredientDisplay(ingredientId)} {amount:0} 块",
        _ => $"{IngredientDisplay(ingredientId)} {amount:0.00} 份"
    };

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
