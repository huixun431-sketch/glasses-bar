using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

public partial class HudController : CanvasLayer
{
    private Label _phase = null!;
    private Label _day = null!;
    private Label _status = null!;
    private Label _prompt = null!;
    private PanelContainer _promptPanel = null!;
    private Label _crosshair = null!;
    private ProgressBar _operationProgress = null!;
    private PanelContainer _feedbackPanel = null!;
    private Label _feedback = null!;
    private Label _debug = null!;
    private Label _leftHand = null!;
    private Label _rightHand = null!;
    private PanelContainer _summaryPanel = null!;
    private Label _summary = null!;
    private Tween? _feedbackTween;

    public override void _Ready()
    {
        _day = GetNode<Label>("Margin/Stack/Day");
        _phase = GetNode<Label>("Margin/Stack/Phase");
        _status = GetNode<Label>("Margin/Stack/Status");
        _promptPanel = GetNode<PanelContainer>("PromptPanel");
        _prompt = GetNode<Label>("PromptPanel/Prompt");
        _crosshair = GetNode<Label>("Crosshair");
        _operationProgress = GetNode<ProgressBar>("OperationProgress");
        _feedbackPanel = GetNode<PanelContainer>("FeedbackPanel");
        _feedback = GetNode<Label>("FeedbackPanel/Feedback");
        _debug = GetNode<Label>("Debug");
        _leftHand = GetNode<Label>("HandsPanel/Margin/Stack/LeftHand");
        _rightHand = GetNode<Label>("HandsPanel/Margin/Stack/RightHand");
        _summaryPanel = GetNode<PanelContainer>("SummaryPanel");
        _summary = GetNode<Label>("SummaryPanel/Summary");

        GameSession.Instance.DayPhaseChanged += OnPhaseChanged;
        GameSession.Instance.DayChanged += OnDayChanged;
        GameSession.Instance.GameStartedChanged += OnGameStartedChanged;
        GameSession.Instance.StatusMessage += OnStatusMessage;
        GameSession.Instance.EvaluationFinished += OnEvaluationFinished;
        _status.Text = "需求未知，可自由准备但不能交付。E 拿取/摆放｜R 双手简易工序｜Esc 暂停";
        OnPhaseChanged((int)GameSession.Instance.Flow.Current);
        OnDayChanged(GameSession.Instance.CurrentDay);
        Visible = GameSession.Instance.GameStarted;
    }

    public void Bind(PlayerController player, DrinkWorkstation workstation)
    {
        player.PromptStateChanged += OnPromptChanged;
        player.OperationChanged += (prompt, active) =>
        {
            _operationProgress.Visible = active;
            if (active)
                OnPromptChanged(prompt, true);
        };
        player.OperationProgressChanged += progress => _operationProgress.Value = progress * 100f;
        workstation.DrinkChanged += debug => _debug.Text = debug;
        workstation.HandsChanged += OnHandsChanged;
        _debug.Text = workstation.GetDebugText();
        OnHandsChanged(workstation.LeftHandDisplayName, workstation.RightHandDisplayName);
    }

    private void OnPhaseChanged(int phase)
    {
        var label = (DayPhase)phase switch
        {
            DayPhase.WaitingForOrder => "等待接单",
            DayPhase.OrderReceived => "观察订单",
            DayPhase.RecipeObservation => "记忆配方",
            DayPhase.Preparation => "手工制作",
            DayPhase.Delivery => "交付",
            DayPhase.Evaluation => "评价",
            DayPhase.DaySummary => "日结",
            _ => ((DayPhase)phase).ToString()
        };
        _phase.Text = $"当前阶段｜{label}";
        if ((DayPhase)phase == DayPhase.WaitingForOrder)
            _status.Text = "顾客需求未知｜可自由操作并允许犯错，但接单前无法交付。";
    }

    private void OnDayChanged(int day)
    {
        _day.Text = $"第 {day} / {GameSession.MaxCampaignDays} 天｜近视 {MyopiaProgression.DegreesForDay(day):0} 度";
        if (GameSession.Instance.Flow.Current == DayPhase.WaitingForOrder)
        {
            _summaryPanel.Visible = false;
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    private void OnPromptChanged(string prompt, bool available)
    {
        _prompt.Text = prompt;
        _promptPanel.Visible = !string.IsNullOrWhiteSpace(prompt);
        var color = available ? new Color("ffd36b") : new Color("a9b0ba");
        _prompt.Modulate = color;
        _crosshair.Modulate = color;
        _crosshair.Text = available ? "◆" : "+";
    }

    private void OnStatusMessage(string message)
    {
        _status.Text = message;
        var failed = message.Contains("失败") || message.Contains("废品") || message.Contains("无法");
        _feedback.Text = $"{(failed ? "✕" : "✓")}  {message}";
        _feedback.Modulate = failed ? new Color("ff8a7a") : new Color("c2ffe0");
        _feedbackPanel.Visible = true;
        _feedbackPanel.Modulate = Colors.White;
        _feedbackTween?.Kill();
        _feedbackTween = CreateTween();
        _feedbackTween.TweenInterval(1.5d);
        _feedbackTween.TweenProperty(_feedbackPanel, "modulate:a", 0f, 0.35d);
        _feedbackTween.TweenCallback(Callable.From(() => _feedbackPanel.Visible = false));
    }

    private void OnEvaluationFinished(bool passed, string summary)
    {
        var continuation = GameSession.Instance.CurrentDay >= GameSession.MaxCampaignDays
            ? "[Enter] 完成 30 天周目并返回主菜单"
            : $"[Enter] 开始第 {GameSession.Instance.CurrentDay + 1} 天";
        _summary.Text = $"第 {GameSession.Instance.CurrentDay} 天" + (passed ? "完成\n" : "结束\n") + summary +
            $"\n\n正式配方数值尚未批准，本结果只验证流程。\n\n{continuation}";
        _summaryPanel.Visible = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    private void OnHandsChanged(string leftHand, string rightHand)
    {
        _leftHand.Text = $"左手｜放置类：{leftHand}";
        _rightHand.Text = $"右手｜手持类：{rightHand}";
    }

    private void OnGameStartedChanged(bool started)
    {
        Visible = started;
        if (started)
            return;
        _summaryPanel.Visible = false;
        _promptPanel.Visible = false;
        _feedbackPanel.Visible = false;
        _operationProgress.Visible = false;
    }
}
