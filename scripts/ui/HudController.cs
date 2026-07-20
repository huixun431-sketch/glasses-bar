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
        _summaryPanel = GetNode<PanelContainer>("SummaryPanel");
        _summary = GetNode<Label>("SummaryPanel/Summary");

        GameSession.Instance.DayPhaseChanged += OnPhaseChanged;
        GameSession.Instance.DayChanged += OnDayChanged;
        GameSession.Instance.StatusMessage += OnStatusMessage;
        GameSession.Instance.EvaluationFinished += OnEvaluationFinished;
        _status.Text = "与客人交互开始教学。WASD 移动｜鼠标观察｜E 交互｜G 切换眼镜｜` 开发控制台";
        OnPhaseChanged((int)GameSession.Instance.Flow.Current);
        OnDayChanged(GameSession.Instance.CurrentDay);
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
        _debug.Text = workstation.GetDebugText();
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
    }

    private void OnDayChanged(int day)
    {
        _day.Text = $"第 {day} 天";
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
        _feedback.Text = $"✓  {message}";
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
        _summary.Text = $"第 {GameSession.Instance.CurrentDay} 天" + (passed ? "完成\n" : "结束\n") + summary +
            $"\n\n正式配方数值尚未批准，本结果只验证流程。\n\n[Enter] 开始第 {GameSession.Instance.CurrentDay + 1} 天";
        _summaryPanel.Visible = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }
}
