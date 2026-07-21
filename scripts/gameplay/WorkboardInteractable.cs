using System;
using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

public partial class WorkboardInteractable : StaticBody3D, IInteractable, IManualOperation
{
    private DrinkWorkstation _workstation = null!;
    private Vector3[] _toolPositions = Array.Empty<Vector3>();
    private OperationSpec? _activeOperation;
    private double _action;
    private double _duration;

    public bool IsRunning { get; private set; }
    public string OperationPrompt => _activeOperation is null
        ? string.Empty
        : $"正在进行{_activeOperation.DisplayName}（{ComplexityDisplay(_activeOperation.ResolveComplexity())}工序）｜按住左键并移动鼠标，松开结算";
    public float FeedbackProgress => _activeOperation is null
        ? 0f
        : (float)Math.Clamp(_action / Math.Max(0.01d, _activeOperation.RequiredAction), 0d, 1d);

    public void Configure(DrinkWorkstation workstation, Vector3 position, Vector3 size, Vector3[] toolPositions)
    {
        _workstation = workstation;
        _toolPositions = toolPositions;
        Name = "workboard";
        Position = position;
        AddToGroup("interactable");
        AddToGroup("workboard");
        AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
    }

    public string GetPrompt(InteractionContext context)
    {
        if (!string.IsNullOrEmpty(_workstation.LeftHandToolId))
            return $"[E] 先将左手的{_workstation.LeftHandDisplayName}放上砧板";
        if (_workstation.CanDepositRightHandIngredientOnBoard(out _))
            return $"[E] 将右手携带的原材料放入砧板上的容器｜{_workstation.GetBoardCapabilityText()}";
        if (_workstation.CanCollectBoardIngredient(out _))
            return "[E] 用右手工具取出砧板上的中间产物";
        if (_workstation.SelectBoardOperation() is { } operation)
        {
            var warning = _workstation.GetBoardAttemptWarning();
            return string.IsNullOrEmpty(warning)
                ? $"[E] 尝试{operation.DisplayName}（{ComplexityDisplay(operation.ResolveComplexity())}）｜错误工具或材料会报废"
                : $"[E] 尝试{operation.DisplayName}（{ComplexityDisplay(operation.ResolveComplexity())}）｜{warning}";
        }
        return $"砧板当前能力：{_workstation.GetBoardCapabilityText()}";
    }

    public string GetUnavailablePrompt(InteractionContext context)
    {
        if (!GameSession.Instance.GameStarted)
            return string.Empty;
        if (GameSession.Instance.WorldMode == WorldMode.Glasses)
            return $"砧板信息｜{_workstation.GetBoardCapabilityText()}｜[G] 摘镜后操作";
        if (!string.IsNullOrEmpty(_workstation.LeftHandToolId) && !_workstation.CanPlaceLeftHandOnBoard(out var placeReason))
            return placeReason;
        if (!string.IsNullOrEmpty(_workstation.RightHandToolId) &&
            _workstation.CanDepositRightHandIngredientOnBoard(out var depositReason) == false &&
            _workstation.SelectBoardOperation() is null)
            return depositReason;
        if (_workstation.GetBoardAttemptWarning() is { Length: > 0 } warning)
            return warning;
        return _workstation.BoardToolCount == 0
            ? "先把左手放置类工具放上砧板"
            : "先用右手工具放入原材料；系统允许错误材料进入工序";
    }

    public bool CanInteract(InteractionContext context)
    {
        if (!GameSession.Instance.CanCraft || GameSession.Instance.WorldMode != WorldMode.Reality)
            return false;
        if (!string.IsNullOrEmpty(_workstation.LeftHandToolId))
            return _workstation.CanPlaceLeftHandOnBoard(out _);
        if (_workstation.CanDepositRightHandIngredientOnBoard(out _))
            return true;
        if (_workstation.CanCollectBoardIngredient(out _))
            return true;
        return _workstation.SelectBoardOperation() is not null;
    }

    public void Interact(InteractionContext context)
    {
        if (!string.IsNullOrEmpty(_workstation.LeftHandToolId))
        {
            _workstation.TryPlaceLeftHandOnBoard(_toolPositions, out var feedback);
            return;
        }
        if (_workstation.CanDepositRightHandIngredientOnBoard(out _))
        {
            _workstation.TryDepositRightHandIngredientOnBoard(out _);
            return;
        }
        if (_workstation.CanCollectBoardIngredient(out _))
        {
            _workstation.TryCollectBoardIngredient(out _);
            return;
        }
        if (Begin(context))
            context.Player.BeginOperation(this);
        else
            GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, GetUnavailablePrompt(context));
    }

    public bool Begin(InteractionContext context)
    {
        if (IsRunning || GameSession.Instance.WorldMode != WorldMode.Reality)
            return false;
        _activeOperation = _workstation.SelectBoardOperation();
        if (_activeOperation is null)
            return false;
        _action = 0d;
        _duration = 0d;
        IsRunning = true;
        return true;
    }

    public void UpdateOperation(double intensity, double deltaSeconds)
    {
        if (!IsRunning)
            return;
        _duration += Math.Max(0d, deltaSeconds);
        _action += Math.Clamp(intensity, 0d, 1d) * Math.Max(0d, deltaSeconds);
    }

    public OperationResult Complete()
    {
        if (!IsRunning || _activeOperation is null)
            return new OperationResult { Feedback = "没有正在进行的砧板工序。" };
        var operation = _activeOperation;
        var result = _workstation.CompleteBoardOperation(operation, _action);
        IsRunning = false;
        _activeOperation = null;
        return new OperationResult
        {
            Completed = result.Completed,
            Intensity = _action,
            DurationSeconds = _duration,
            Feedback = _workstation.LastOperationFeedback
        };
    }

    public void Cancel()
    {
        IsRunning = false;
        _activeOperation = null;
        _action = 0d;
        _duration = 0d;
    }

    private static string ComplexityDisplay(OperationComplexity complexity) => complexity switch
    {
        OperationComplexity.Simple => "简易",
        OperationComplexity.Normal => "普通",
        OperationComplexity.Complex => "复杂",
        _ => "自动"
    };
}
