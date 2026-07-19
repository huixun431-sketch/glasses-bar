using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

public partial class HudController : CanvasLayer
{
    private Label _phase = null!;
    private Label _status = null!;
    private Label _prompt = null!;
    private Label _debug = null!;
    private PanelContainer _summaryPanel = null!;
    private Label _summary = null!;

    public override void _Ready()
    {
        _phase = GetNode<Label>("Margin/Stack/Phase");
        _status = GetNode<Label>("Margin/Stack/Status");
        _prompt = GetNode<Label>("Prompt");
        _debug = GetNode<Label>("Debug");
        _summaryPanel = GetNode<PanelContainer>("SummaryPanel");
        _summary = GetNode<Label>("SummaryPanel/Summary");

        GameSession.Instance.DayPhaseChanged += OnPhaseChanged;
        GameSession.Instance.StatusMessage += message => _status.Text = message;
        GameSession.Instance.EvaluationFinished += OnEvaluationFinished;
        _status.Text = "与客人交互开始教学。WASD 移动，鼠标观察，E 交互，G 切换眼镜。";
        OnPhaseChanged((int)GameSession.Instance.Flow.Current);
    }

    public void Bind(PlayerController player, DrinkWorkstation workstation)
    {
        player.PromptChanged += prompt => _prompt.Text = prompt;
        player.OperationChanged += (prompt, active) => _prompt.Text = active ? prompt : string.Empty;
        workstation.DrinkChanged += debug => _debug.Text = debug;
        _debug.Text = workstation.GetDebugText();
    }

    private void OnPhaseChanged(int phase) => _phase.Text = $"阶段：{(DayPhase)phase}";

    private void OnEvaluationFinished(bool passed, string summary)
    {
        _summary.Text = (passed ? "教学日完成\n" : "教学日结束\n") + summary + "\n\n正式配方数值尚未批准，本结果只验证流程。";
        _summaryPanel.Visible = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }
}

