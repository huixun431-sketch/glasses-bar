using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

public partial class GameSession : Node
{
    [Signal] public delegate void WorldModeChangedEventHandler(int mode);
    [Signal] public delegate void DayPhaseChangedEventHandler(int phase);
    [Signal] public delegate void DayChangedEventHandler(int day);
    [Signal] public delegate void GameStartedChangedEventHandler(bool started);
    [Signal] public delegate void StatusMessageEventHandler(string message);
    [Signal] public delegate void EvaluationFinishedEventHandler(bool passed, string summary);

    public static GameSession Instance { get; private set; } = null!;

    public WorldMode WorldMode { get; private set; } = WorldMode.Reality;
    public DayFlow Flow { get; } = new();
    public int CurrentDay { get; private set; } = 1;
    public bool GameStarted { get; private set; }
    public bool RecipeObserved { get; private set; }

    public bool CanMove => GameStarted && Flow.Current is not DayPhase.Evaluation and not DayPhase.DaySummary;
    public bool CanCraft => GameStarted && WorldMode == WorldMode.Reality && Flow.Current == DayPhase.Preparation;

    public override void _EnterTree() => Instance = this;

    public void AcceptOrder()
    {
        if (!GameStarted)
            return;
        if (!Flow.TryAdvance(DayPhase.OrderReceived))
        {
            EmitSignal(SignalName.StatusMessage, "当前无法重复接单。");
            return;
        }

        EmitPhase();
        EmitSignal(SignalName.StatusMessage, "订单：冰美式。按 G 戴上眼镜查看开发占位配方。");
    }

    public void ToggleWorld()
    {
        if (!GameStarted || Flow.Current == DayPhase.DaySummary)
            return;

        WorldMode = WorldMode == WorldMode.Reality ? WorldMode.Glasses : WorldMode.Reality;
        if (WorldMode == WorldMode.Glasses && Flow.Current == DayPhase.OrderReceived)
        {
            RecipeObserved = true;
            Flow.TryAdvance(DayPhase.RecipeObservation);
            EmitPhase();
        }
        else if (WorldMode == WorldMode.Reality && Flow.Current == DayPhase.RecipeObservation)
        {
            Flow.TryAdvance(DayPhase.Preparation);
            EmitPhase();
        }

        EmitSignal(SignalName.WorldModeChanged, (int)WorldMode);
        EmitSignal(SignalName.StatusMessage,
            WorldMode == WorldMode.Glasses
                ? "眼镜世界：可以移动观察；客人与原材料已隐藏，无法制作或交互。"
                : "现实世界：可以移动与制作。");
    }

    public bool BeginDelivery()
    {
        if (!Flow.TryAdvance(DayPhase.Delivery))
            return false;
        EmitPhase();
        return true;
    }

    public void FinishEvaluation(DrinkEvaluation evaluation)
    {
        if (!Flow.TryAdvance(DayPhase.Evaluation))
            return;

        EmitPhase();
        var summary = evaluation.Passed
            ? $"教学完成｜步骤 {evaluation.StepCompletionRatio:P0}｜原料 {evaluation.IngredientCompletionRatio:P0}｜浪费 {evaluation.WastedAmount:0.00}"
            : $"成品未通过｜缺少步骤：{string.Join("、", evaluation.MissingSteps)}｜缺少原料：{string.Join("、", evaluation.MissingIngredients)}";

        EmitSignal(SignalName.EvaluationFinished, evaluation.Passed, summary);
        Flow.TryAdvance(DayPhase.DaySummary);
        EmitPhase();
    }

    public void RestartDay()
    {
        Flow.Reset();
        RecipeObserved = false;
        WorldMode = WorldMode.Reality;
        EmitSignal(SignalName.WorldModeChanged, (int)WorldMode);
        EmitPhase();
        EmitSignal(SignalName.StatusMessage, "教学日已重置。与客人交互开始接单。");
    }

    public bool AdvanceToNextDay()
    {
        if (Flow.Current != DayPhase.DaySummary)
            return false;

        CurrentDay++;
        ResetFlowForDay();
        EmitSignal(SignalName.DayChanged, CurrentDay);
        EmitSignal(SignalName.StatusMessage, $"第 {CurrentDay} 天开始。与客人交互接单。");
        return true;
    }

    public void StartNewGame()
    {
        GameStarted = true;
        EmitSignal(SignalName.GameStartedChanged, true);
        ResetCampaign();
    }

    public void ResetCampaign()
    {
        CurrentDay = 1;
        ResetFlowForDay();
        EmitSignal(SignalName.DayChanged, CurrentDay);
        EmitSignal(SignalName.StatusMessage, "第 1 天开始。与客人交互接单。");
    }

    private void ResetFlowForDay()
    {
        Flow.Reset();
        RecipeObserved = false;
        WorldMode = WorldMode.Reality;
        EmitSignal(SignalName.WorldModeChanged, (int)WorldMode);
        EmitPhase();
    }

    private void EmitPhase() => EmitSignal(SignalName.DayPhaseChanged, (int)Flow.Current);
}
