using Godot;
using GlassesBar.Domain;

namespace GlassesBar.Tests;

public partial class DaySummaryVisualCapture : Node
{
    public override void _Ready() => CallDeferred(MethodName.ShowSummary);

    private void ShowSummary()
    {
        GetNode<Button>("Main/OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Start").EmitSignal(Button.SignalName.Pressed);
        GameSession.Instance.AcceptOrder();
        GameSession.Instance.ToggleWorld();
        GameSession.Instance.ToggleWorld();
        GameSession.Instance.BeginDelivery();
        GameSession.Instance.FinishEvaluation(new DrinkEvaluation
        {
            Passed = true,
            StepCompletionRatio = 1d,
            IngredientCompletionRatio = 1d
        });
    }
}
