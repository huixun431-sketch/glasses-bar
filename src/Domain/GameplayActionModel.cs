namespace GlassesBar.Domain;

public enum GameplayActionMode
{
    Instant,
    Continuous
}

public enum GameplayActionPhase
{
    Offered,
    Rejected,
    Started,
    Committed,
    Cancelled
}

/// <summary>
/// Stable definition shared by every runtime request of the same action.
/// Definitions contain identity and lifecycle policy, never actor or target state.
/// </summary>
public sealed record GameplayActionDefinition(string Id, GameplayActionMode Mode);

/// <summary>
/// Read-only evaluation result. Inspecting an action must not mutate gameplay state.
/// </summary>
public sealed record GameplayActionDecision(bool IsAvailable, string Prompt);

/// <summary>
/// Audit record emitted after a pipeline transition.
/// </summary>
public sealed record GameplayActionTrace(
    string ActionId,
    string TargetId,
    GameplayActionPhase Phase,
    string Feedback);

public static class GameplayActionDefinitions
{
    public static readonly GameplayActionDefinition PickUpTool = new("inventory.pick_up", GameplayActionMode.Instant);
    public static readonly GameplayActionDefinition PlaceHeldTool = new("inventory.place", GameplayActionMode.Instant);
    public static readonly GameplayActionDefinition PlaceToolOnBoard = new("workboard.place_tool", GameplayActionMode.Instant);
    public static readonly GameplayActionDefinition DepositBoardIngredient = new("workboard.deposit_ingredient", GameplayActionMode.Instant);
    public static readonly GameplayActionDefinition CollectBoardIngredient = new("workboard.collect_ingredient", GameplayActionMode.Instant);
    public static readonly GameplayActionDefinition RunBoardProcess = new("process.run_board", GameplayActionMode.Continuous);
    public static readonly GameplayActionDefinition AcceptOrder = new("customer.accept_order", GameplayActionMode.Instant);
    public static readonly GameplayActionDefinition DeliverDrink = new("customer.deliver_drink", GameplayActionMode.Instant);
    public static readonly GameplayActionDefinition LoadIngredient = new("inventory.load_ingredient", GameplayActionMode.Instant);
    public static readonly GameplayActionDefinition WashHands = new("station.wash_hands", GameplayActionMode.Instant);
    public static readonly GameplayActionDefinition FillMeasure = new("station.fill_measure", GameplayActionMode.Instant);
    public static readonly GameplayActionDefinition DiscardContents = new("inventory.discard_contents", GameplayActionMode.Instant);
    public static readonly GameplayActionDefinition ToggleStorage = new("storage.toggle", GameplayActionMode.Instant);
    public static readonly GameplayActionDefinition ToggleWorld = new("session.toggle_world", GameplayActionMode.Instant);
    public static readonly GameplayActionDefinition UseHeldTool = new("process.use_held_tool", GameplayActionMode.Instant);
    public static readonly GameplayActionDefinition ToggleMeasureSide = new("inventory.toggle_measure_side", GameplayActionMode.Instant);
    public static readonly GameplayActionDefinition AdvanceDay = new("session.advance_day", GameplayActionMode.Instant);
}
