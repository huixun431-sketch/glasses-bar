using System;
using System.Collections.Generic;
using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

internal readonly record struct StationActionContext(
    StationInteractable Station,
    StationDefinition Definition,
    InteractionContext Interaction)
{
    public DrinkWorkstation Workstation => Interaction.Workstation;

    public bool IsPlayerWithin(float distance) =>
        Station.GlobalPosition.DistanceTo(Interaction.Player.GlobalPosition) <= distance;

    public string DefaultUnavailablePrompt =>
        $"当前无法使用 · {Definition.DisplayName}";
}

internal interface IStationActionHandler
{
    string GetPrompt(StationActionContext context);
    GameplayActionDefinition GetActionDefinition(StationActionContext context);
    string GetUnavailablePrompt(StationActionContext context);
    bool CanInteract(StationActionContext context);
    void Execute(StationActionContext context);
}

internal static class StationActionHandlerRegistry
{
    private static readonly IReadOnlyDictionary<string, IStationActionHandler> Handlers =
        new Dictionary<string, IStationActionHandler>(StringComparer.Ordinal)
        {
            [StationHandlerIds.Customer] = new CustomerStationActionHandler(),
            [StationHandlerIds.IngredientSource] = new IngredientSourceStationActionHandler(),
            [StationHandlerIds.HandWash] = new HandWashStationActionHandler(),
            [StationHandlerIds.Kettle] = new KettleStationActionHandler(),
            [StationHandlerIds.WasteBin] = new WasteBinStationActionHandler()
        };

    public static IStationActionHandler Resolve(StringName handlerId)
    {
        var id = handlerId.ToString();
        return Handlers.TryGetValue(id, out var handler)
            ? handler
            : throw new InvalidOperationException($"Unknown station action handler '{id}'.");
    }

    public static bool IsRegistered(StringName handlerId) =>
        Handlers.ContainsKey(handlerId.ToString());
}

internal sealed class CustomerStationActionHandler : IStationActionHandler
{
    public string GetPrompt(StationActionContext context) =>
        GameSession.Instance.Flow.Current == DayPhase.WaitingForOrder
            ? "[E] 接受订单｜接单前可自由操作，但需求未知且无法交付"
            : "[E] 将左手高球杯交给客人";

    public GameplayActionDefinition GetActionDefinition(StationActionContext context) =>
        GameSession.Instance.Flow.Current == DayPhase.WaitingForOrder
            ? GameplayActionDefinitions.AcceptOrder
            : GameplayActionDefinitions.DeliverDrink;

    public string GetUnavailablePrompt(StationActionContext context)
    {
        if (GameSession.Instance.Flow.Current != DayPhase.Preparation)
            return context.DefaultUnavailablePrompt;
        if (!context.Workstation.CanDeliver)
            return "左手拿着装有成品的高球杯后再来提交";
        return context.IsPlayerWithin(context.Definition.InteractionDistance)
            ? "[E] 将左手成品交给客人"
            : "请走近客人后再提交成品";
    }

    public bool CanInteract(StationActionContext context) =>
        GameSession.Instance.Flow.Current == DayPhase.WaitingForOrder ||
        GameSession.Instance.Flow.Current == DayPhase.Preparation &&
        context.Workstation.CanDeliver &&
        context.IsPlayerWithin(context.Definition.InteractionDistance);

    public void Execute(StationActionContext context)
    {
        if (GameSession.Instance.Flow.Current == DayPhase.WaitingForOrder)
            GameSession.Instance.AcceptOrder();
        else
            context.Workstation.EvaluateAndFinish();
    }
}

internal sealed class IngredientSourceStationActionHandler : IStationActionHandler
{
    public string GetPrompt(StationActionContext context) =>
        context.Definition.PromptTemplate.Replace(
            "{right_hand}",
            context.Workstation.RightHandDisplayName,
            StringComparison.Ordinal);

    public GameplayActionDefinition GetActionDefinition(StationActionContext context) =>
        GameplayActionDefinitions.LoadIngredient;

    public string GetUnavailablePrompt(StationActionContext context) =>
        context.Workstation.CanLoadIngredient(context.Definition.IngredientId.ToString(), out var reason)
            ? context.DefaultUnavailablePrompt
            : reason;

    public bool CanInteract(StationActionContext context) =>
        GameSession.Instance.CanCraft &&
        context.Workstation.CanLoadIngredient(context.Definition.IngredientId.ToString(), out _);

    public void Execute(StationActionContext context) =>
        context.Workstation.TryLoadIngredient(
            context.Definition.IngredientId.ToString(),
            context.Definition.IngredientAmount,
            out _);
}

internal sealed class HandWashStationActionHandler : IStationActionHandler
{
    public string GetPrompt(StationActionContext context) =>
        context.Workstation.HandsWashedToday
            ? "[E] 再次洗手（今天已完成）"
            : "[E] 洗手｜每天至少一次，否则后续工序成功率小幅降低";

    public GameplayActionDefinition GetActionDefinition(StationActionContext context) =>
        GameplayActionDefinitions.WashHands;

    public string GetUnavailablePrompt(StationActionContext context) =>
        context.DefaultUnavailablePrompt;

    public bool CanInteract(StationActionContext context) =>
        GameSession.Instance.CanCraft;

    public void Execute(StationActionContext context)
    {
        if (!context.Workstation.WashHands(out var feedback))
            GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, feedback);
    }
}

internal sealed class KettleStationActionHandler : IStationActionHandler
{
    public string GetPrompt(StationActionContext context) =>
        context.Workstation.RightHandHasDualMeasure
            ? $"[E] 用{context.Workstation.RightHandDisplayName}{context.Workstation.RightHandMeasureSideName}接取 {context.Workstation.RightHandMeasureAmount:0} ml｜[F] 切换量酒器另一端"
            : "[E] 从水壶取水｜先拿一种双头量酒器；水壶不直接倒入制作容器";

    public GameplayActionDefinition GetActionDefinition(StationActionContext context) =>
        GameplayActionDefinitions.FillMeasure;

    public string GetUnavailablePrompt(StationActionContext context) =>
        context.Workstation.CanFillRightHandFromKettle(out var reason)
            ? context.DefaultUnavailablePrompt
            : reason;

    public bool CanInteract(StationActionContext context) =>
        GameSession.Instance.CanCraft &&
        context.Workstation.CanFillRightHandFromKettle(out _);

    public void Execute(StationActionContext context)
    {
        if (!context.Workstation.TryFillRightHandFromKettle(out var feedback))
            GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, feedback);
    }
}

internal sealed class WasteBinStationActionHandler : IStationActionHandler
{
    public string GetPrompt(StationActionContext context) =>
        "[E] 将手中工具里的原材料/废品倒入弃物桶";

    public GameplayActionDefinition GetActionDefinition(StationActionContext context) =>
        GameplayActionDefinitions.DiscardContents;

    public string GetUnavailablePrompt(StationActionContext context) =>
        context.DefaultUnavailablePrompt;

    public bool CanInteract(StationActionContext context) =>
        GameSession.Instance.CanCraft;

    public void Execute(StationActionContext context)
    {
        if (!context.Workstation.TryDiscardHeldContents(out var feedback))
            GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, feedback);
    }
}
