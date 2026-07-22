using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

public partial class StationInteractable : StaticBody3D, IInteractable
{
    private const float CustomerDeliveryDistance = 3.15f;

    [Export] public StationKind Kind { get; set; }
    [Export] public string EntityId { get; set; } = string.Empty;

    public string GetPrompt(InteractionContext context) => Kind switch
    {
        StationKind.Customer when GameSession.Instance.Flow.Current == DayPhase.WaitingForOrder => "[E] 接受订单｜接单前可自由操作，但需求未知且无法交付",
        StationKind.Customer => "[E] 将左手高球杯交给客人",
        StationKind.IceBucket => $"[E] 用{context.Workstation.RightHandDisplayName}从已打开的上层抽屉冰桶夹取一块冰",
        StationKind.HandWashSink => context.Workstation.HandsWashedToday ? "[E] 再次洗手（今天已完成）" : "[E] 洗手｜每天至少一次，否则后续工序成功率小幅降低",
        StationKind.Kettle when context.Workstation.RightHandHasDualMeasure =>
            $"[E] 用{context.Workstation.RightHandDisplayName}{context.Workstation.RightHandMeasureSideName}接取 {context.Workstation.RightHandMeasureAmount:0} ml｜[F] 切换量酒器另一端",
        StationKind.Kettle => "[E] 从水壶取水｜先拿一种双头量酒器；水壶不直接倒入制作容器",
        StationKind.CoffeeBeans => $"[E] 用{context.Workstation.RightHandDisplayName}取 0.25 份开发占位咖啡豆",
        StationKind.WasteBin => "[E] 将手中工具里的原材料/废品倒入弃物桶",
        _ => string.Empty
    };

    public string GetUnavailablePrompt(InteractionContext context)
    {
        if (!GameSession.Instance.GameStarted)
            return string.Empty;
        if (GetStorageParent() is { IsOpen: false })
            return "先打开砧板右下方的上层抽屉，才能使用里面的冰桶。";
        if (GameSession.Instance.WorldMode == WorldMode.Glasses)
            return Kind is StationKind.Customer or StationKind.IceBucket or StationKind.CoffeeBeans
                ? string.Empty
                : $"[G] 摘下眼镜后操作 · {DisplayName}";
        if (Kind == StationKind.Customer && GameSession.Instance.Flow.Current == DayPhase.Preparation)
        {
            if (!context.Workstation.CanDeliver)
                return "左手拿着装有成品的高球杯后再来提交";
            return IsWithinCustomerDeliveryDistance(context)
                ? "[E] 将左手成品交给客人"
                : "请走近客人后再提交成品";
        }
        if (Kind == StationKind.Kettle && !context.Workstation.CanFillRightHandFromKettle(out var kettleReason))
            return kettleReason;
        var ingredientId = IngredientId;
        if (!string.IsNullOrEmpty(ingredientId) && !context.Workstation.CanLoadIngredient(ingredientId, out var reason))
            return reason;
        return $"当前无法使用 · {DisplayName}";
    }

    public string DisplayName => Kind switch
    {
        StationKind.Customer => "客人",
        StationKind.IceBucket => "抽屉冰桶",
        StationKind.HandWashSink => "洗手水槽",
        StationKind.Kettle => "水壶",
        StationKind.CoffeeBeans => "咖啡豆",
        StationKind.WasteBin => "弃物桶",
        _ => EntityId
    };

    public bool CanInteract(InteractionContext context)
    {
        if (!GameSession.Instance.GameStarted || GameSession.Instance.WorldMode == WorldMode.Glasses ||
            GetStorageParent() is { IsOpen: false })
            return false;
        return Kind switch
        {
            StationKind.Customer => GameSession.Instance.Flow.Current == DayPhase.WaitingForOrder ||
                GameSession.Instance.Flow.Current == DayPhase.Preparation && context.Workstation.CanDeliver && IsWithinCustomerDeliveryDistance(context),
            StationKind.IceBucket or StationKind.CoffeeBeans =>
                GameSession.Instance.CanCraft && context.Workstation.CanLoadIngredient(IngredientId, out _),
            StationKind.Kettle => GameSession.Instance.CanCraft && context.Workstation.CanFillRightHandFromKettle(out _),
            StationKind.HandWashSink or StationKind.WasteBin => GameSession.Instance.CanCraft,
            _ => false
        };
    }

    public void Interact(InteractionContext context)
    {
        if (!CanInteract(context))
        {
            GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, GetUnavailablePrompt(context));
            return;
        }
        switch (Kind)
        {
            case StationKind.Customer:
                if (GameSession.Instance.Flow.Current == DayPhase.WaitingForOrder)
                    GameSession.Instance.AcceptOrder();
                else
                    context.Workstation.EvaluateAndFinish();
                break;
            case StationKind.IceBucket:
                context.Workstation.TryLoadIngredient("ice", 1d, out _);
                break;
            case StationKind.CoffeeBeans:
                context.Workstation.TryLoadIngredient("coffee_beans", 0.25d, out _);
                break;
            case StationKind.HandWashSink:
                if (!context.Workstation.WashHands(out var washFeedback))
                    GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, washFeedback);
                break;
            case StationKind.Kettle:
                if (!context.Workstation.TryFillRightHandFromKettle(out var kettleFeedback))
                    GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, kettleFeedback);
                break;
            case StationKind.WasteBin:
                if (!context.Workstation.TryDiscardHeldContents(out var discardFeedback))
                    GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, discardFeedback);
                break;
        }
    }

    private string IngredientId => Kind switch
    {
        StationKind.IceBucket => "ice",
        StationKind.CoffeeBeans => "coffee_beans",
        _ => string.Empty
    };

    private CabinetInteractable? GetStorageParent() => GetParent() as CabinetInteractable;

    private bool IsWithinCustomerDeliveryDistance(InteractionContext context) =>
        GlobalPosition.DistanceTo(context.Player.GlobalPosition) <= CustomerDeliveryDistance;
}
