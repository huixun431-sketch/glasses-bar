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
    private double _progress;
    private double _duration;

    public bool IsRunning { get; private set; }
    public string OperationPrompt => Kind switch
    {
        StationKind.WaterDispenser => "按住左键并向下移动鼠标倒水；空格/↑ 为辅助；松开完成",
        StationKind.GrindingStation => "在砧板上按住左键并移动鼠标研磨；空格/↑ 为辅助；松开完成",
        StationKind.ExtractionStation => "按住左键持续浸润萃取；空格/↑ 为辅助；松开完成",
        StationKind.FilteringStation => "按住左键将萃取液过滤入杯；空格/↑ 为辅助；松开完成",
        _ => string.Empty
    };

    public float FeedbackProgress => Kind switch
    {
        StationKind.WaterDispenser when _context is not null =>
            (float)Math.Clamp(_context.Workstation.Glass.Ingredients.TryGetValue("water", out var water) ? water / 0.05d : 0d, 0d, 1d),
        StationKind.GrindingStation => (float)Math.Clamp(_progress / 0.6d, 0d, 1d),
        StationKind.ExtractionStation => (float)Math.Clamp(_progress / 0.7d, 0d, 1d),
        StationKind.FilteringStation when _context is not null =>
            (float)Math.Clamp(_context.Workstation.Glass.Ingredients.TryGetValue("espresso", out var espresso) ? espresso / 0.15d : 0d, 0d, 1d),
        _ => 0f
    };

    public string GetPrompt(InteractionContext context) => Kind switch
    {
        StationKind.Customer when GameSession.Instance.Flow.Current == DayPhase.WaitingForOrder => "[E] 接受冰美式订单",
        StationKind.Customer => "[E] 将手中成品交给客人",
        StationKind.PickupGlass => context.Workstation.HasGlass ? "高球杯已在手中" : "[E] 拿取高球杯",
        StationKind.IceBucket => "[E] 加入一块冰",
        StationKind.WaterDispenser => "[E] 从左侧水槽开始手工加水",
        StationKind.CoffeeBeans => "[E] 从后吧台取用咖啡豆",
        StationKind.MortarTool => "[E] 拿取研钵与研杵",
        StationKind.FilterTool => "[E] 拿取传统滤具",
        StationKind.GrindingStation => "[E] 在砧板上手工研磨咖啡豆",
        StationKind.ExtractionStation => "[E] 开始手工浸润萃取",
        StationKind.FilteringStation => "[E] 将萃取液过滤入杯",
        StationKind.WasteBin => "[E] 丢弃当前饮品并重做",
        _ => string.Empty
    };

    public string GetUnavailablePrompt(InteractionContext context)
    {
        if (GameSession.Instance.WorldMode == WorldMode.Glasses && Kind == StationKind.Customer)
            return string.Empty;
        if (GameSession.Instance.WorldMode == WorldMode.Glasses)
            return $"[G] 摘下眼镜后操作 · {DisplayName}";
        if (Kind == StationKind.Customer && GameSession.Instance.Flow.Current == DayPhase.Preparation)
        {
            if (!context.Workstation.HasGlass)
                return "先完成饮品，再回到客人面前提交";
            return IsWithinCustomerDeliveryDistance(context)
                ? "[E] 将手中成品交给客人"
                : "请走近客人后再提交成品";
        }
        if (Kind == StationKind.PickupGlass && context.Workstation.HasGlass)
            return "高球杯已拿取 · 继续下一步";
        if (Kind == StationKind.MortarTool && context.Workstation.HasMortarTool)
            return "研钵与研杵已拿取 · 前往中央砧板";
        if (Kind == StationKind.FilterTool && context.Workstation.HasFilterTool)
            return "滤具已拿取 · 前往中央砧板";
        if (Kind == StationKind.CoffeeBeans && context.Workstation.CoffeeBeansPortioned)
            return "咖啡豆已取用 · 前往中央砧板";
        if (GameSession.Instance.Flow.Current == DayPhase.WaitingForOrder && Kind != StationKind.Customer)
            return $"先向客人接单 · {DisplayName}";
        if (!GameSession.Instance.CanCraft && Kind != StationKind.Customer)
            return $"当前阶段暂不可用 · {DisplayName}";
        return $"尚未满足操作条件 · {DisplayName}";
    }

    public string DisplayName => Kind switch
    {
        StationKind.Customer => "客人",
        StationKind.PickupGlass => "高球杯",
        StationKind.IceBucket => "冰桶",
        StationKind.WaterDispenser => "水槽",
        StationKind.CoffeeBeans => "咖啡豆",
        StationKind.MortarTool => "研钵与研杵",
        StationKind.FilterTool => "传统滤具",
        StationKind.GrindingStation => "砧板·研磨",
        StationKind.ExtractionStation => "砧板·萃取",
        StationKind.FilteringStation => "砧板·过滤",
        StationKind.WasteBin => "弃物桶",
        _ => EntityId
    };

    public bool CanInteract(InteractionContext context)
    {
        if (GameSession.Instance.WorldMode == WorldMode.Glasses)
            return false;

        return Kind switch
        {
            StationKind.Customer => GameSession.Instance.Flow.Current == DayPhase.WaitingForOrder ||
                GameSession.Instance.Flow.Current == DayPhase.Preparation && context.Workstation.HasGlass && IsWithinCustomerDeliveryDistance(context),
            StationKind.PickupGlass => GameSession.Instance.CanCraft && !context.Workstation.HasGlass,
            StationKind.IceBucket => GameSession.Instance.CanCraft && context.Workstation.HasGlass,
            StationKind.WaterDispenser => GameSession.Instance.CanCraft && context.Workstation.HasGlass,
            StationKind.CoffeeBeans => GameSession.Instance.CanCraft && !context.Workstation.CoffeeBeansPortioned,
            StationKind.MortarTool => GameSession.Instance.CanCraft && !context.Workstation.HasMortarTool,
            StationKind.FilterTool => GameSession.Instance.CanCraft && !context.Workstation.HasFilterTool,
            StationKind.GrindingStation => GameSession.Instance.CanCraft && context.Workstation.HasMortarTool &&
                context.Workstation.CoffeeBeansPortioned && !context.Workstation.GroundCoffeeReady,
            StationKind.ExtractionStation => GameSession.Instance.CanCraft && context.Workstation.GroundCoffeeReady &&
                context.Workstation.HasFilterTool && !context.Workstation.ExtractedCoffeeReady,
            StationKind.FilteringStation => GameSession.Instance.CanCraft && context.Workstation.ExtractedCoffeeReady &&
                context.Workstation.HasFilterTool && context.Workstation.HasGlass && !context.Workstation.FilteredCoffeeComplete,
            StationKind.WasteBin => GameSession.Instance.CanCraft,
            _ => false
        };
    }

    public void Interact(InteractionContext context)
    {
        if (!CanInteract(context))
        {
            GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, "当前状态无法执行该操作。");
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
            case StationKind.PickupGlass:
                context.Workstation.TakeGlass();
                break;
            case StationKind.IceBucket:
                context.Workstation.AddIce();
                break;
            case StationKind.CoffeeBeans:
                context.Workstation.TakeCoffeeBeans();
                break;
            case StationKind.MortarTool:
                context.Workstation.TakeMortarTool();
                break;
            case StationKind.FilterTool:
                context.Workstation.TakeFilterTool();
                break;
            case StationKind.WaterDispenser:
            case StationKind.GrindingStation:
            case StationKind.ExtractionStation:
            case StationKind.FilteringStation:
                if (Begin(context))
                    context.Player.BeginOperation(this);
                break;
            case StationKind.WasteBin:
                context.Workstation.DiscardAndReset();
                break;
        }
    }

    public bool Begin(InteractionContext context)
    {
        if (IsRunning || !CanInteract(context))
            return false;
        _context = context;
        _progress = 0d;
        _duration = 0d;
        IsRunning = true;
        return true;
    }

    public void UpdateOperation(double intensity, double deltaSeconds)
    {
        if (!IsRunning || _context is null)
            return;

        intensity = Math.Clamp(intensity, 0d, 1d);
        deltaSeconds = Math.Max(0d, deltaSeconds);
        _duration += deltaSeconds;
        _progress += intensity * deltaSeconds;

        switch (Kind)
        {
            case StationKind.WaterDispenser:
                _context.Workstation.AddLiquid("water", LiquidMath.FlowFromTilt(intensity, 0.75d, deltaSeconds));
                break;
            case StationKind.FilteringStation:
                _context.Workstation.AddLiquid("espresso", LiquidMath.FlowFromTilt(intensity, 0.45d, deltaSeconds));
                break;
        }
    }

    public OperationResult Complete()
    {
        if (!IsRunning || _context is null)
            return new OperationResult { Feedback = "没有正在进行的操作。" };

        var completed = Kind switch
        {
            StationKind.WaterDispenser => _context.Workstation.Glass.Ingredients.TryGetValue("water", out var water) && water > 0.05d,
            StationKind.GrindingStation => _progress >= 0.6d,
            StationKind.ExtractionStation => _progress >= 0.7d,
            StationKind.FilteringStation => _context.Workstation.Glass.Ingredients.TryGetValue("espresso", out var espresso) && espresso > 0.15d,
            _ => false
        };

        if (completed)
        {
            if (Kind == StationKind.WaterDispenser)
                _context.Workstation.MarkWaterComplete();
            else if (Kind == StationKind.GrindingStation)
                _context.Workstation.MarkGroundCoffee();
            else if (Kind == StationKind.ExtractionStation)
                _context.Workstation.MarkExtractionComplete();
            else if (Kind == StationKind.FilteringStation)
                _context.Workstation.MarkFilteringComplete();
        }

        IsRunning = false;
        var result = new OperationResult
        {
            Completed = completed,
            Intensity = _progress,
            DurationSeconds = _duration,
            Feedback = completed ? "操作完成。" : "操作不足，可再次尝试。"
        };
        _context = null;
        return result;
    }

    public void Cancel()
    {
        IsRunning = false;
        _context = null;
        _progress = 0d;
        _duration = 0d;
    }

    private bool IsWithinCustomerDeliveryDistance(InteractionContext context) =>
        GlobalPosition.DistanceTo(context.Player.GlobalPosition) <= CustomerDeliveryDistance;
}
