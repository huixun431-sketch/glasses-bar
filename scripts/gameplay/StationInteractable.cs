using System;
using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

public partial class StationInteractable : StaticBody3D, IInteractable, IManualOperation
{
    private const float CustomerDeliveryDistance = 3.15f;

    [Export] public StationKind Kind { get; set; }
    [Export] public string EntityId { get; set; } = string.Empty;

    private InteractionContext? _context;
    private double _loadedAmount;
    private double _duration;

    public bool IsRunning { get; private set; }
    public string OperationPrompt => Kind == StationKind.WaterDispenser
        ? "按住左键并上下移动鼠标向右手水壶取水；松开结束取水"
        : string.Empty;
    public float FeedbackProgress => Kind == StationKind.WaterDispenser && _context is not null
        ? (float)Math.Clamp(_context.Workstation.GetRightHandIngredientAmount("water") / 0.5d, 0d, 1d)
        : 0f;

    public string GetPrompt(InteractionContext context) => Kind switch
    {
        StationKind.Customer when GameSession.Instance.Flow.Current == DayPhase.WaitingForOrder => "[E] 接受订单｜接单前可自由操作，但需求未知且无法交付",
        StationKind.Customer => "[E] 将左手高球杯交给客人",
        StationKind.IceBucket => $"[E] 用{context.Workstation.RightHandDisplayName}夹取一块冰",
        StationKind.WaterDispenser => $"[E] 用{context.Workstation.RightHandDisplayName}开始取水",
        StationKind.CoffeeBeans => $"[E] 用{context.Workstation.RightHandDisplayName}取 0.25 份开发占位咖啡豆",
        StationKind.WasteBin => "[E] 将手中工具里的原材料/废品倒入弃物桶",
        _ => string.Empty
    };

    public string GetUnavailablePrompt(InteractionContext context)
    {
        if (!GameSession.Instance.GameStarted)
            return string.Empty;
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
        var ingredientId = IngredientId;
        if (!string.IsNullOrEmpty(ingredientId) && !context.Workstation.CanLoadIngredient(ingredientId, out var reason))
            return reason;
        return $"当前无法使用 · {DisplayName}";
    }

    public string DisplayName => Kind switch
    {
        StationKind.Customer => "客人",
        StationKind.IceBucket => "冰桶",
        StationKind.WaterDispenser => "水槽",
        StationKind.CoffeeBeans => "咖啡豆",
        StationKind.WasteBin => "弃物桶",
        _ => EntityId
    };

    public bool CanInteract(InteractionContext context)
    {
        if (!GameSession.Instance.GameStarted || GameSession.Instance.WorldMode == WorldMode.Glasses)
            return false;
        return Kind switch
        {
            StationKind.Customer => GameSession.Instance.Flow.Current == DayPhase.WaitingForOrder ||
                GameSession.Instance.Flow.Current == DayPhase.Preparation && context.Workstation.CanDeliver && IsWithinCustomerDeliveryDistance(context),
            StationKind.IceBucket or StationKind.WaterDispenser or StationKind.CoffeeBeans =>
                GameSession.Instance.CanCraft && context.Workstation.CanLoadIngredient(IngredientId, out _),
            StationKind.WasteBin => GameSession.Instance.CanCraft,
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
            case StationKind.WaterDispenser:
                if (Begin(context))
                    context.Player.BeginOperation(this);
                break;
            case StationKind.WasteBin:
                if (!context.Workstation.TryDiscardHeldContents(out var feedback))
                    GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, feedback);
                break;
        }
    }

    public bool Begin(InteractionContext context)
    {
        if (Kind != StationKind.WaterDispenser || IsRunning || !CanInteract(context))
            return false;
        _context = context;
        _loadedAmount = 0d;
        _duration = 0d;
        IsRunning = true;
        return true;
    }

    public void UpdateOperation(double intensity, double deltaSeconds)
    {
        if (!IsRunning || _context is null)
            return;
        var amount = LiquidMath.FlowFromTilt(Math.Clamp(intensity, 0d, 1d), 0.5d, Math.Max(0d, deltaSeconds));
        if (amount <= 0d)
            return;
        _duration += Math.Max(0d, deltaSeconds);
        if (_context.Workstation.TryLoadIngredient("water", amount, out _, false))
            _loadedAmount += amount;
    }

    public OperationResult Complete()
    {
        if (!IsRunning)
            return new OperationResult { Feedback = "没有正在进行的取水操作。" };
        IsRunning = false;
        _context = null;
        return new OperationResult
        {
            Completed = _loadedAmount > 0d,
            Intensity = _loadedAmount,
            DurationSeconds = _duration,
            Feedback = _loadedAmount > 0d
                ? $"水壶已取水 {_loadedAmount:0.00} 份；按 R 尝试向左手容器加水。"
                : "没有取到水，可再次尝试。"
        };
    }

    public void Cancel()
    {
        IsRunning = false;
        _context = null;
        _loadedAmount = 0d;
        _duration = 0d;
    }

    private string IngredientId => Kind switch
    {
        StationKind.IceBucket => "ice",
        StationKind.WaterDispenser => "water",
        StationKind.CoffeeBeans => "coffee_beans",
        _ => string.Empty
    };

    private bool IsWithinCustomerDeliveryDistance(InteractionContext context) =>
        GlobalPosition.DistanceTo(context.Player.GlobalPosition) <= CustomerDeliveryDistance;
}
