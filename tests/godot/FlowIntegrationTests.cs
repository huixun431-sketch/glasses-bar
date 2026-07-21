using System;
using Godot;
using GlassesBar.Domain;

namespace GlassesBar.Tests;

public partial class FlowIntegrationTests : Node
{
    private bool _evaluationPassed;

    public override void _Ready() => CallDeferred(MethodName.RunDeferred);

    private void RunDeferred()
    {
        try
        {
            var main = GetNode<Node3D>("Main");
            var workstation = main.GetNode<DrinkWorkstation>("NeutralGameplay/DrinkWorkstation");
            var player = main.GetNode<PlayerController>("Player");
            var customer = Station(main, "customer");
            var board = main.GetNode<WorkboardInteractable>("NeutralGameplay/workboard");
            var bin = Station(main, "waste_bin");
            var reality = main.GetNode<Node3D>("RealityWorld");
            var glasses = main.GetNode<Node3D>("GlassesWorld");
            var realityChildren = reality.GetChildCount();
            var glassesChildren = glasses.GetChildCount();
            GameSession.Instance.EvaluationFinished += (passed, _) => _evaluationPassed = passed;

            main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Start").EmitSignal(Button.SignalName.Pressed);
            Require(GameSession.Instance.GameStarted, "opening menu starts the gameplay session");
            Require(main.HasNode("NeutralGameplay/workboard") && !main.HasNode("NeutralGameplay/grinding_station"),
                "one compositional workboard replaces fixed operation stations");
            Require(reality.HasNode("CuttingBoard") && !reality.HasNode("OperationManual") && glasses.HasNode("OperationManual"),
                "cutting board and glasses-only manual remain in the approved layout");
            Require(reality.HasNode("coffee_beans") && !glasses.HasNode("coffee_beans"),
                "raw ingredients remain hidden in the glasses world");

            Require(workstation.GetToolSpec("highball_glass").ResolveCategory() == ToolCategory.Placement &&
                    workstation.GetToolSpec("mortar").ResolveCategory() == ToolCategory.Placement,
                "glass and mortar auto-classify as placement tools");
            Require(workstation.GetToolSpec("pestle").ResolveCategory() == ToolCategory.Handheld &&
                    workstation.GetToolSpec("ice_tongs").ResolveCategory() == ToolCategory.Handheld,
                "pestle and tongs auto-classify as handheld tools");
            Require(workstation.GetOperationComplexity("add_water") == OperationComplexity.Simple &&
                    workstation.GetOperationComplexity("add_ice") == OperationComplexity.Simple &&
                    workstation.GetOperationComplexity("filter_coffee") == OperationComplexity.Normal &&
                    workstation.GetOperationComplexity("grind_coffee") == OperationComplexity.Complex,
                "operation complexity is derived from board and handheld requirements");

            var context = Context(player, workstation);
            var drawer = main.GetNode<CabinetInteractable>("NeutralGameplay/front_drawer_1");
            drawer.Interact(context);
            Require(drawer.IsOpen, "empty under-counter drawers are interactable and open with animation state");
            drawer.Interact(context);
            Require(!drawer.IsOpen, "drawer interaction toggles closed again");
            var scoop = Tool(main, "bean_scoop");
            scoop.Interact(context);
            LoadBeans(main, context, 1);
            Require(GameSession.Instance.Flow.Current == DayPhase.WaitingForOrder &&
                    Math.Abs(workstation.GetRightHandIngredientAmount("coffee_beans") - 0.25d) < 0.000001d,
                "player can freely prepare before accepting the order while customer demand remains unknown");
            GameSession.Instance.AcceptOrder();
            Require(GameSession.Instance.Flow.Current == DayPhase.Preparation && !GameSession.Instance.RecipeObserved &&
                    Math.Abs(workstation.GetRightHandIngredientAmount("coffee_beans") - 0.25d) < 0.000001d,
                "accepting an order reveals demand without resetting pre-order preparation or mistakes");
            GameSession.Instance.ToggleWorld();
            Require(GameSession.Instance.WorldMode == WorldMode.Glasses && GameSession.Instance.Flow.Current == DayPhase.Preparation,
                "glasses are optional information lookup and do not own the crafting phase");
            Require(!Tool(main, "mortar").CanInteract(Context(player, workstation)),
                "physical tool interaction remains disabled while glasses are worn");
            GameSession.Instance.ToggleWorld();

            var mortar = Tool(main, "mortar");
            var glass = Tool(main, "highball_glass");
            var frontSurface = main.GetNode<CounterSurfaceInteractable>("NeutralGameplay/front_counter_surface");
            var frontPlacementContext = new InteractionContext
            {
                Player = player,
                Workstation = workstation,
                InteractionPoint = Position("front_left_free")
            };
            Require(!frontSurface.CanInteract(frontPlacementContext) &&
                    frontSurface.GetUnavailablePrompt(frontPlacementContext).Contains("不能直接搁在台面"),
                "loaded handheld carrier cannot be placed directly on an ordinary counter");
            mortar.Interact(context);
            Require(workstation.LeftHandToolId == "mortar" && !mortar.Visible,
                "picked placement tool occupies the left hand and disappears from its old position");
            Require(!glass.CanInteract(context), "only one placement tool can be held at once");
            frontSurface.Interact(frontPlacementContext);
            Require(mortar.Visible && workstation.GetToolLocation("mortar") == ToolLocation.Counter &&
                    workstation.RightHandToolId == "bean_scoop",
                "ordinary counter places the left-hand tool first while keeping the loaded right-hand carrier held");
            glass.Interact(context);
            Require(!frontSurface.CanInteract(frontPlacementContext),
                "continuous counter placement rejects footprint overlap");
            PlaceAt(workstation, "back_free_far");
            mortar.Interact(context);
            board.Interact(context);
            Require(workstation.IsToolOnBoard("mortar") && workstation.GetBoardCapabilityText().Contains("研磨"),
                "mortar on board enables its data-defined grinding capability");
            board.Interact(context);
            Require(Math.Abs(workstation.GetToolContentAmount("mortar", "coffee_beans") - 0.25d) < 0.000001d,
                "pre-order ingredient can be transferred after the order is accepted");
            PlaceAt(workstation, "back_scoop");

            var filter = Tool(main, "traditional_filter");
            filter.Interact(context);
            Require(!workstation.CanPlaceLeftHandOnBoard(out var conflictReason) && conflictReason.Contains("冲突"),
                "conflicting placement tools cannot share the board");
            PlaceAt(workstation, "front_filter");

            var tongs = Tool(main, "ice_tongs");
            scoop.Interact(context);
            Require(workstation.RightHandToolId == "bean_scoop" && !tongs.CanInteract(context),
                "only one handheld tool can be held at once");
            LoadBeans(main, context, 1);
            Require(!Station(main, "ice_bucket").CanInteract(context),
                "one handheld tool cannot mix a second ingredient while carrying the first");
            LoadBeans(main, context, 2);
            board.Interact(context);
            Require(workstation.GetToolContentAmount("mortar", "coffee_beans") == 1d,
                "raw material is deposited only after a placement tool is on the board");
            PlaceAt(workstation, "back_free_left");

            tongs.Interact(context);
            workstation.QueueAttemptRollForTests(0d);
            CompleteBoard(board, context, 0.7d);
            Require(workstation.LastProcessResult?.Failure == ProcessFailure.WrongHandheldTool && workstation.IsToolContentWaste("mortar"),
                "wrong handheld tool is allowed to operate, then fails and turns material into waste");
            PlaceAt(workstation, "back_tongs");
            mortar.Interact(context);
            bin.Interact(context);
            Require(!workstation.IsToolContentWaste("mortar") && workstation.TotalWaste > 0d,
                "failed material remains until the player manually empties its container into the waste bin");
            board.Interact(context);

            tongs.Interact(context);
            Station(main, "ice_bucket").Interact(context);
            board.Interact(context);
            PlaceAt(workstation, "back_tongs");
            Tool(main, "pestle").Interact(context);
            workstation.QueueAttemptRollForTests(0d);
            CompleteBoard(board, context, 0.7d);
            Require(workstation.LastProcessResult?.Failure == ProcessFailure.WrongIngredients && workstation.IsToolContentWaste("mortar"),
                "ingredient combination that matches no recipe is attempted and then becomes waste");
            PlaceAt(workstation, "front_pestle");
            mortar.Interact(context);
            bin.Interact(context);
            board.Interact(context);

            scoop.Interact(context);
            LoadBeans(main, context, 3);
            board.Interact(context);
            PlaceAt(workstation, "back_free_left");
            Tool(main, "pestle").Interact(context);
            workstation.QueueAttemptRollForTests(0d);
            Require(CompleteBoard(board, context, 0.7d).Completed &&
                    Math.Abs((workstation.LastProcessResult?.CompletionRatio ?? 0d) - 0.75d) < 0.000001d &&
                    Math.Abs((workstation.LastProcessResult?.SuccessProbability ?? 0d) - 0.75d) < 0.000001d,
                "quantity deviation passes probabilistic judgment and assigns matching operation completion");
            PlaceAt(workstation, "front_pestle");
            scoop.Interact(context);
            board.Interact(context);
            Require(workstation.GetRightHandIngredientAmount("ground_coffee") == 1d,
                "handheld carrier transfers one processed ingredient at a time");

            mortar.Interact(context);
            PlaceAt(workstation, "back_free_left");
            Require(workstation.RightHandToolId == "bean_scoop" &&
                    workstation.GetRightHandIngredientAmount("ground_coffee") > 0d,
                "left-first counter placement keeps the loaded scoop in hand while moving the mortar away");
            filter.Interact(context);
            board.Interact(context);
            board.Interact(context);
            Require(workstation.GetToolContentAmount("traditional_filter", "ground_coffee") == 1d,
                "ground coffee is placed into the board-mounted filter");
            PlaceAt(workstation, "back_scoop");
            Tool(main, "water_carafe").Interact(context);
            Require(workstation.TryLoadIngredient("water", 0.5d, out _), "water must first be carried by a handheld carafe");
            board.Interact(context);
            workstation.QueueAttemptRollForTests(0d);
            Require(CompleteBoard(board, context, 0.8d).Completed, "filter, ground coffee, water and carafe complete extraction");
            Require(workstation.GetBoardCapabilityText().Contains("中间产物已完成") &&
                    workstation.GetBoardAttemptWarning().Contains("再次执行"),
                "completed extraction clearly points to filtering and warns that repeating extraction will be a deliberate mistake");
            PlaceAt(workstation, "back_carafe");

            glass.Interact(context);
            board.Interact(context);
            Require(workstation.GetBoardCapabilityText().Contains("过滤"),
                "filter and glass combination enables the normal filtering operation");
            workstation.QueueAttemptRollForTests(0d);
            Require(CompleteBoard(board, context, 0.6d).Completed, "normal filtering succeeds without a handheld tool");
            glass.Interact(context);
            Require(workstation.HasGlass && workstation.Glass.Ingredients.ContainsKey("espresso"),
                "finished glass returns to the player's left hand");

            tongs.Interact(context);
            Station(main, "ice_bucket").Interact(context);
            workstation.QueueAttemptRollForTests(0d);
            Require(workstation.TryUseSimpleOperation().Completed && workstation.IcePieces == 1,
                "ice is a simple off-board operation using left-hand glass and right-hand tongs");
            PlaceAt(workstation, "back_free_far");
            PlaceAt(workstation, "back_tongs");
            glass.Interact(context);
            Tool(main, "water_carafe").Interact(context);
            workstation.TryLoadIngredient("water", 0.5d, out _);
            workstation.QueueAttemptRollForTests(0d);
            Require(workstation.TryUseSimpleOperation().Completed && workstation.Glass.Ingredients.ContainsKey("water"),
                "water is a simple off-board operation using the two hand slots");
            Require(Math.Abs(workstation.DrinkCompletionRatio - 0.75d) < 0.000001d,
                "deviation-derived completion persists as a property of the finished drink");
            PlaceAt(workstation, "back_free_far");
            PlaceAt(workstation, "back_carafe");
            glass.Interact(context);

            Require(!customer.CanInteract(context), "finished drink cannot be submitted from the initial distant position");
            player.GlobalPosition = new Vector3(0f, 0.9f, -0.9f);
            Require(customer.CanInteract(context), "finished drink can be submitted after approaching the customer");
            customer.Interact(context);
            Require(_evaluationPassed && GameSession.Instance.Flow.Current == DayPhase.DaySummary,
                "valid drink reaches evaluation even after recoverable failed attempts");

            player._UnhandledInput(new InputEventAction { Action = "next_day", Pressed = true, Strength = 1f });
            Require(GameSession.Instance.CurrentDay == 2 && GameSession.Instance.Flow.Current == DayPhase.WaitingForOrder,
                "next day resets the flow");
            Require(string.IsNullOrEmpty(workstation.LeftHandToolId) && string.IsNullOrEmpty(workstation.RightHandToolId) && workstation.BoardToolCount == 0,
                "next day returns all tools from hands and board to their initial positions");
            Require(Tool(main, "mortar").Visible && Tool(main, "pestle").Visible && Math.Abs(player.GlobalPosition.Z - (-3f)) < 0.01f,
                "tool entities and player transform reset visibly for the new day");
            for (var expectedDay = 2; expectedDay <= GameSession.MaxCampaignDays; expectedDay++)
            {
                GameSession.Instance.AcceptOrder();
                Require(GameSession.Instance.BeginDelivery(), $"day {expectedDay} can reach delivery");
                GameSession.Instance.FinishEvaluation(new DrinkEvaluation { Passed = true });
                if (expectedDay < GameSession.MaxCampaignDays)
                    Require(GameSession.Instance.AdvanceToNextDay() && GameSession.Instance.CurrentDay == expectedDay + 1,
                        $"campaign advances from day {expectedDay}");
            }
            Require(!GameSession.Instance.AdvanceToNextDay() && GameSession.Instance.CurrentDay == GameSession.MaxCampaignDays &&
                    !GameSession.Instance.GameStarted && main.GetNode<OpeningMenuController>("OpeningMenu").Visible,
                "day 30 ends the campaign and returns to the main menu instead of creating day 31");
            Require(Math.Abs(main.GetNode<MyopiaEffectController>("MyopiaEffectController").MyopiaDegrees - 350f) < 0.01f,
                "day progression updates reality myopia through the 30-day curve");
            Require(reality.GetChildCount() == realityChildren && glasses.GetChildCount() == glassesChildren,
                "world switching never duplicates presentation nodes");
            GD.Print("FLOW_INTEGRATION_PASS");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private static OperationResult CompleteBoard(WorkboardInteractable board, InteractionContext context, double action)
    {
        Require(board.Begin(context), "board operation begins when tools and material are physically present");
        board.UpdateOperation(1d, action);
        return board.Complete();
    }

    private static void LoadBeans(Node3D main, InteractionContext context, int portions)
    {
        var beans = Station(main, "coffee_beans");
        for (var index = 0; index < portions; index++)
            beans.Interact(context);
    }

    private static InteractionContext Context(PlayerController player, DrinkWorkstation workstation) =>
        new() { Player = player, Workstation = workstation };
    private static ToolInteractable Tool(Node3D main, string id) => main.GetNode<ToolInteractable>($"NeutralGameplay/{id}");
    private static StationInteractable Station(Node3D main, string id) => main.GetNode<StationInteractable>($"NeutralGameplay/{id}");
    private static void PlaceAt(DrinkWorkstation workstation, string id)
    {
        Require(workstation.TryPlaceHeldToolAtPosition(Position(id), out var feedback), feedback);
    }

    private static Vector3 Position(string id) => id switch
    {
        "front_filter" => new Vector3(-1.62f, 1.24f, 0.12f),
        "front_mortar" => new Vector3(1.62f, 1.24f, 0.12f),
        "front_pestle" => new Vector3(2.22f, 1.24f, 0.12f),
        "front_left_free" => new Vector3(-2.35f, 1.24f, 0.12f),
        "back_tongs" => new Vector3(-3.55f, 1.13f, -5.42f),
        "back_free_left" => new Vector3(-0.45f, 1.13f, -5.42f),
        "back_scoop" => new Vector3(0.35f, 1.13f, -5.42f),
        "back_carafe" => new Vector3(1.25f, 1.13f, -5.42f),
        "back_free_far" => new Vector3(4.2f, 1.13f, -5.42f),
        _ => throw new InvalidOperationException($"Unknown test placement: {id}")
    };

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
