using System;
using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

public partial class StationInteractable : StaticBody3D, IInteractable, IManualOperation
{
    [Export] public StationKind Kind { get; set; }
    [Export] public string EntityId { get; set; } = string.Empty;

    private InteractionContext? _context;
    private double _progress;
    private double _duration;

    public bool IsRunning { get; private set; }
    public string OperationPrompt => Kind switch
    {
        StationKind.WaterDispenser => "按住左键并向下移动鼠标倒水；空格/↑ 为辅助；松开完成",
        StationKind.Grinder => "按住左键并移动鼠标研磨；空格/↑ 为辅助；松开完成",
        StationKind.EspressoMachine => "按住左键持续萃取；空格/↑ 为辅助；松开完成",
        _ => string.Empty
    };

    public float FeedbackProgress => Kind switch
    {
        StationKind.WaterDispenser when _context is not null =>
            (float)Math.Clamp(_context.Workstation.Glass.Ingredients.TryGetValue("water", out var water) ? water / 0.05d : 0d, 0d, 1d),
        StationKind.Grinder => (float)Math.Clamp(_progress / 0.6d, 0d, 1d),
        StationKind.EspressoMachine when _context is not null =>
            (float)Math.Clamp(_context.Workstation.Glass.Ingredients.TryGetValue("espresso", out var espresso) ? espresso / 0.15d : 0d, 0d, 1d),
        _ => 0f
    };

    public string GetPrompt(InteractionContext context) => Kind switch
    {
        StationKind.Customer => "[E] 接受冰美式订单",
        StationKind.PickupGlass => context.Workstation.HasGlass ? "高球杯已在手中" : "[E] 拿取高球杯",
        StationKind.IceBucket => "[E] 加入一块冰",
        StationKind.WaterDispenser => "[E] 开始手工倒水",
        StationKind.Grinder => "[E] 开始研磨咖啡豆",
        StationKind.EspressoMachine => "[E] 开始萃取浓缩咖啡",
        StationKind.ServeCounter => "[E] 交付饮品并评价",
        StationKind.Sink => "[E] 丢弃当前饮品并重做",
        _ => string.Empty
    };

    public string GetUnavailablePrompt(InteractionContext context)
    {
        if (GameSession.Instance.WorldMode == WorldMode.Glasses && Kind == StationKind.Customer)
            return string.Empty;
        if (GameSession.Instance.WorldMode == WorldMode.Glasses)
            return $"[G] 摘下眼镜后操作 · {DisplayName}";
        if (Kind == StationKind.PickupGlass && context.Workstation.HasGlass)
            return "高球杯已拿取 · 继续下一步";
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
        StationKind.WaterDispenser => "水台",
        StationKind.Grinder => "磨豆机",
        StationKind.EspressoMachine => "咖啡机",
        StationKind.ServeCounter => "出杯区",
        StationKind.Sink => "水槽/丢弃",
        _ => EntityId
    };

    public bool CanInteract(InteractionContext context)
    {
        if (GameSession.Instance.WorldMode == WorldMode.Glasses)
            return false;

        return Kind switch
        {
            StationKind.Customer => GameSession.Instance.Flow.Current == DayPhase.WaitingForOrder,
            StationKind.PickupGlass => GameSession.Instance.CanCraft && !context.Workstation.HasGlass,
            StationKind.IceBucket => GameSession.Instance.CanCraft && context.Workstation.HasGlass,
            StationKind.WaterDispenser => GameSession.Instance.CanCraft && context.Workstation.HasGlass,
            StationKind.Grinder => GameSession.Instance.CanCraft,
            StationKind.EspressoMachine => GameSession.Instance.CanCraft && context.Workstation.GroundCoffeeReady && context.Workstation.HasGlass,
            StationKind.ServeCounter => GameSession.Instance.CanCraft && context.Workstation.HasGlass,
            StationKind.Sink => GameSession.Instance.CanCraft,
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
                GameSession.Instance.AcceptOrder();
                break;
            case StationKind.PickupGlass:
                context.Workstation.TakeGlass();
                break;
            case StationKind.IceBucket:
                context.Workstation.AddIce();
                break;
            case StationKind.WaterDispenser:
            case StationKind.Grinder:
            case StationKind.EspressoMachine:
                if (Begin(context))
                    context.Player.BeginOperation(this);
                break;
            case StationKind.ServeCounter:
                context.Workstation.EvaluateAndFinish();
                break;
            case StationKind.Sink:
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
            case StationKind.EspressoMachine:
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
            StationKind.Grinder => _progress >= 0.6d,
            StationKind.EspressoMachine => _context.Workstation.Glass.Ingredients.TryGetValue("espresso", out var espresso) && espresso > 0.15d,
            _ => false
        };

        if (completed)
        {
            if (Kind == StationKind.WaterDispenser)
                _context.Workstation.MarkWaterComplete();
            else if (Kind == StationKind.Grinder)
                _context.Workstation.MarkGroundCoffee();
            else if (Kind == StationKind.EspressoMachine)
                _context.Workstation.MarkEspressoComplete();
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
}
