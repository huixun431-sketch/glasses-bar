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
            var board = main.GetNode<WorkboardInteractable>("NeutralGameplay/workboard");
            var customer = Station(main, "customer");
            var sink = Station(main, "hand_wash_sink");
            var kettle = Station(main, "kettle");
            var ice = Station(main, "ice_bucket");
            var bin = Station(main, "waste_bin");
            var reality = main.GetNode<Node3D>("RealityWorld");
            var glasses = main.GetNode<Node3D>("GlassesWorld");
            var realityChildren = reality.GetChildCount();
            var glassesChildren = glasses.GetChildCount();
            GameSession.Instance.EvaluationFinished += (passed, _) => _evaluationPassed = passed;

            main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Start").EmitSignal(Button.SignalName.Pressed);
            Require(GameSession.Instance.GameStarted, "approved split main menu starts the gameplay session");
            var context = Context(player, workstation);

            VerifyLayout(main, workstation, sink, ice, context);
            VerifyDailyWaterAndIceRules(main, workstation, sink, kettle, ice, bin, context);

            GameSession.Instance.AcceptOrder();
            Require(GameSession.Instance.Flow.Current == DayPhase.Preparation,
                "accepting an order keeps manual crafting available without requiring glasses");
            GameSession.Instance.ToggleWorld();
            Require(GameSession.Instance.WorldMode == WorldMode.Glasses && !Tool(main, "mortar").CanInteract(context),
                "glasses remain a movable information world but physical interaction stays disabled");
            GameSession.Instance.ToggleWorld();

            var scoop = Tool(main, "bean_scoop");
            var mortar = Tool(main, "mortar");
            var filter = Tool(main, "traditional_filter");
            var glass = Tool(main, "highball_glass");
            scoop.Interact(context);
            LoadBeans(main, context, 4);
            mortar.Interact(context);
            board.Interact(context);
            board.Interact(context);
            PlaceAt(workstation, "scoop_free");
            Tool(main, "pestle").Interact(context);
            workstation.QueueAttemptRollForTests(0d);
            Require(CompleteBoard(board, context, 0.7d).Completed, "mortar, beans and pestle complete prototype grinding");
            PlaceAt(workstation, "pestle_free");

            scoop.Interact(context);
            board.Interact(context);
            mortar.Interact(context);
            PlaceAt(workstation, "mortar_free");
            filter.Interact(context);
            board.Interact(context);
            board.Interact(context);
            PlaceAt(workstation, "scoop_free");
            Require(Math.Abs(workstation.GetToolContentAmount("traditional_filter", "ground_coffee") - 1d) < 0.000001d,
                "ground coffee reaches the board-mounted traditional filter");

            var largeJigger = Tool(main, "jigger_large");
            largeJigger.Interact(context);
            workstation.SetKettleWaterForTests(0d);
            workstation.QueueAttemptRollForTests(0d);
            Require(!CompleteBoard(board, context, 0.8d).Completed &&
                    workstation.LastOperationFeedback.Contains("水壶无水") &&
                    !workstation.IsToolContentWaste("traditional_filter"),
                "dry extraction identifies the empty kettle without destroying the coffee");
            workstation.SetKettleWaterForTests(DrinkWorkstation.PrototypeKettleCapacityMl);
            Require(workstation.ToggleRightHandMeasureSide(out _) && Math.Abs(workstation.RightHandMeasureAmount - 25d) < 0.001d,
                "large double-ended jigger can switch to its 25 ml prototype side");
            kettle.Interact(context);
            board.Interact(context);
            workstation.QueueAttemptRollForTests(0d);
            Require(CompleteBoard(board, context, 0.8d).Completed, "measured jigger water enables manual extraction");
            var firstExtraction = workstation.GetToolContentCompletionRatio("traditional_filter");
            Require(firstExtraction > 0.8d && firstExtraction < 1d,
                "25 ml against the 30 ml prototype target creates a recoverable completion loss");
            workstation.QueueAttemptRollForTests(0d);
            Require(CompleteBoard(board, context, 0.8d).Completed, "repeat extraction is a valid recovery attempt");
            var recoveredExtraction = workstation.GetToolContentCompletionRatio("traditional_filter");
            Require(recoveredExtraction > firstExtraction && recoveredExtraction <= 0.96d,
                "repeat extraction restores part of the loss but stays capped");
            PlaceAt(workstation, "jigger_large_free");

            glass.Interact(context);
            board.Interact(context);
            workstation.QueueAttemptRollForTests(0d);
            Require(CompleteBoard(board, context, 0.6d).Completed, "filter and highball glass complete normal filtering");
            var firstFiltration = workstation.GetToolContentCompletionRatio("highball_glass");
            workstation.QueueAttemptRollForTests(0d);
            Require(CompleteBoard(board, context, 0.6d).Completed, "repeat filtering is a valid recovery attempt");
            var recoveredFiltration = workstation.GetToolContentCompletionRatio("highball_glass");
            Require(recoveredFiltration > firstFiltration && recoveredFiltration <= 0.96d,
                "repeat filtering also provides bounded completion recovery");

            glass.Interact(context);
            Tool(main, "jigger_small").Interact(context);
            Require(Math.Abs(workstation.RightHandMeasureAmount - 30d) < 0.001d,
                "small jigger defaults to its 30 ml end for keyboard-accessible measured water");
            kettle.Interact(context);
            workstation.QueueAttemptRollForTests(0d);
            Require(workstation.TryUseSimpleOperation().Completed &&
                    Math.Abs(workstation.Glass.Ingredients["water"] - 30d) < 0.001d,
                "jigger water replaces the old direct-kettle pour operation");
            PlaceAt(workstation, "glass_free");
            PlaceAt(workstation, "jigger_free");
            glass.Interact(context);
            Tool(main, "ice_tongs").Interact(context);
            ice.Interact(context);
            workstation.QueueAttemptRollForTests(0d);
            var iceResult = workstation.TryUseSimpleOperation();
            Require(iceResult.Completed && workstation.IcePieces == 1,
                $"only ice tongs can load ice from the opened upper drawer into the glass: {iceResult.Feedback} | {workstation.GetDebugText()}");
            PlaceAt(workstation, "glass_free");
            PlaceAt(workstation, "tongs_free");
            glass.Interact(context);

            Require(!customer.CanInteract(context), "expanded bar keeps customer delivery outside the initial work position");
            player.GlobalPosition = new Vector3(0f, 1.045f, -0.2f);
            Require(customer.CanInteract(context), "finished drink can be submitted only after approaching the customer");
            customer.Interact(context);
            Require(_evaluationPassed && GameSession.Instance.Flow.Current == DayPhase.DaySummary,
                "prototype drink reaches evaluation with bounded recovery preserved");

            player._UnhandledInput(new InputEventAction { Action = "next_day", Pressed = true, Strength = 1f });
            var iceDrawer = main.GetNode<CabinetInteractable>("NeutralGameplay/front_drawer_2_upper");
            Require(GameSession.Instance.CurrentDay == 2 && !workstation.HandsWashedToday &&
                    Math.Abs(workstation.KettleWaterAmountMl - DrinkWorkstation.PrototypeKettleCapacityMl) < 0.001d &&
                    !iceDrawer.IsOpen && Math.Abs(player.GlobalPosition.Z - (-1.2f)) < 0.01f,
                "next day resets hand washing, kettle, drawer state, tools and raised-camera player position");

            for (var expectedDay = 2; expectedDay <= GameSession.MaxCampaignDays; expectedDay++)
            {
                GameSession.Instance.AcceptOrder();
                Require(GameSession.Instance.BeginDelivery(), $"day {expectedDay} can reach delivery");
                GameSession.Instance.FinishEvaluation(new DrinkEvaluation { Passed = true });
                if (expectedDay < GameSession.MaxCampaignDays)
                    Require(GameSession.Instance.AdvanceToNextDay() && GameSession.Instance.CurrentDay == expectedDay + 1,
                        $"campaign advances from day {expectedDay}");
            }
            Require(!GameSession.Instance.AdvanceToNextDay() && !GameSession.Instance.GameStarted &&
                    main.GetNode<OpeningMenuController>("OpeningMenu").Visible,
                "day 30 ends the campaign and returns to the split main menu");
            Require(reality.GetChildCount() == realityChildren && glasses.GetChildCount() == glassesChildren,
                "world switching and day resets never duplicate presentation nodes");
            GD.Print("FLOW_INTEGRATION_PASS");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private static void VerifyLayout(Node3D main, DrinkWorkstation workstation, StationInteractable sink,
        StationInteractable ice, InteractionContext context)
    {
        Require(Math.Abs(GrayboxLevelBuilder.FrontBarTopHeight - 1.42f) < 0.001f &&
                Math.Abs(GrayboxLevelBuilder.OperationAisleClearWidth - 2.31f) < 0.001f &&
                GrayboxLevelBuilder.OperationAisleClearWidth >= 0.7f * 2f + 0.3f,
            "low rear bar is removed and the taller internal operation aisle comfortably fits two player-width people");
        Require(Math.Abs(main.GetNode<Node3D>("Player/Head").GlobalPosition.Y - GrayboxLevelBuilder.PlayerEyeHeight) < 0.01f,
            "player capsule and camera rise together to the doubled playtest increment");
        Require(main.GetNode<Node3D>("RealityWorld").HasNode("MergedBottleRackBack") &&
                main.GetNode<Node3D>("RealityWorld").HasNode("UpperBackCabinet") &&
                main.GetNode<Node3D>("RealityWorld").HasNode("RearWallShelf") &&
                !main.GetNode<Node3D>("NeutralGameplay").HasNode("MergedBackBarCollider") &&
                main.GetNode<Node3D>("RealityWorld").HasNode("RearBooth"),
            "the low rear bar becomes a shallow shelf plus an overhead cabinet above the bottle rack");
        foreach (var toolId in new[] { "highball_glass", "mortar", "traditional_filter", "pestle", "bean_scoop", "ice_tongs", "jigger_small", "jigger_medium", "jigger_large" })
            Require(Math.Abs(Tool(main, toolId).Position.Z - 0.2f) < 0.01f, $"{toolId} starts on the front bar");
        Require(Tool(main, "ice_tongs").GetNode<CollisionShape3D>("CollisionShape3D").Shape is BoxShape3D &&
                Tool(main, "jigger_small").GetNode<CollisionShape3D>("CollisionShape3D").Shape is CylinderShape3D,
            "tool collision shapes now match their visible graybox families");

        var frontDrawerCount = 0;
        var backDoorCount = 0;
        var narrowestWalkingLane = float.MaxValue;
        foreach (var node in main.GetTree().GetNodesInGroup("cabinet_storage"))
        {
            if (node is not CabinetInteractable cabinet)
                continue;
            var name = cabinet.Name.ToString();
            if (name.StartsWith("front_drawer_", StringComparison.Ordinal))
            {
                frontDrawerCount++;
                Require(cabinet.OpenPosition.Z < cabinet.ClosedPosition.Z && cabinet.OutwardDirection.Z < 0f &&
                        cabinet.OpenTravelDistance >= 0.6f,
                    "front drawers pull substantially farther outward and keep deep trays");
                // The rear wall shelf begins at z=-2.70 after the low rear bar is removed.
                var openFrontDrawerBackEdge = cabinet.OpenPosition.Z - cabinet.PanelSize.Z * 0.5f;
                narrowestWalkingLane = Math.Min(narrowestWalkingLane, openFrontDrawerBackEdge - (-2.70f));
            }
            if (name.StartsWith("back_cabinet_", StringComparison.Ordinal))
            {
                backDoorCount++;
                Require(cabinet.OutwardDirection.Z > 0f && Math.Abs(cabinet.OpenRotationY) > 1.4f &&
                        cabinet.ClosedPosition.Y - cabinet.PanelSize.Y * 0.5f >= GrayboxLevelBuilder.BottleRackTopHeight + 0.2f &&
                        cabinet.PanelSize.X >= 1.4f && cabinet.PanelSize.Y >= 0.9f,
                    "larger back cabinet doors swing outward from above the bottle rack instead of occupying the walking lane");
            }
        }
        Require(frontDrawerCount == 8 && backDoorCount == 6,
            "front bar keeps four double-drawer bays with a clear sink bay while three overhead cabinets use paired large doors");
        Require(Math.Abs(sink.Position.Z - 0.2f) < 0.01f && sink.Position.X > 3.5f &&
                !main.GetNode<Node3D>("NeutralGameplay").HasNode("sink_left_drawer_upper") &&
                !main.GetNode<Node3D>("NeutralGameplay").HasNode("sink_left_drawer_lower"),
            "the wash sink is back on the screen-left front bar with no drawer or cabinet underneath");
        var manual = main.GetNode<Node3D>("GlassesWorld/OperationManual");
        Require(manual.Position.X < -5f && manual.Position.Z < -0.6f &&
                !main.GetNode<Node3D>("RealityWorld").HasNode("OperationManual"),
            "the glasses-only manual sits on the side enclosure away from the kettle and front tools");
        Require(narrowestWalkingLane >= 1.65f,
            "a fully opened long front drawer still leaves a comfortably passable lane to the shallow rear shelf");
        var iceDrawer = main.GetNode<CabinetInteractable>("NeutralGameplay/front_drawer_2_upper");
        Require(ice.GetParent() == iceDrawer && !ice.CanInteract(context),
            "ice bucket is physically stored in the cutting-board-right upper drawer and is inaccessible while closed");
        var backDoor = main.GetNode<CabinetInteractable>("NeutralGameplay/back_cabinet_3_left");
        iceDrawer.SetOpen(true, false);
        backDoor.SetOpen(true, false);
        Require(backDoor.IsOpen && !iceDrawer.IsOpen,
            "opening another storage front auto-closes the previous one so the widened aisle cannot be pinched from both sides");
        backDoor.SetOpen(false, false);
    }

    private static void VerifyDailyWaterAndIceRules(Node3D main, DrinkWorkstation workstation,
        StationInteractable sink, StationInteractable kettle, StationInteractable ice, StationInteractable bin,
        InteractionContext context)
    {
        Require(!workstation.HandsWashedToday && Math.Abs(workstation.SuccessProbabilityPenalty - 0.04d) < 0.000001d,
            "each day begins unwashed with a small prototype success-rate penalty");
        sink.Interact(context);
        Require(workstation.HandsWashedToday && workstation.SuccessProbabilityPenalty == 0d,
            "sink interaction records the required daily hand wash and is no longer a crafting water source");
        Require(workstation.GetToolSpec("jigger_small").HasDualMeasure &&
                workstation.GetToolSpec("jigger_medium").HasDualMeasure &&
                workstation.GetToolSpec("jigger_large").HasDualMeasure,
            "three data-defined double-ended jiggers replace direct kettle pouring");

        var iceDrawer = main.GetNode<CabinetInteractable>("NeutralGameplay/front_drawer_2_upper");
        iceDrawer.Interact(context);
        Require(iceDrawer.IsOpen, "upper ice drawer opens before using its contents");
        Tool(main, "bean_scoop").Interact(context);
        Require(!ice.CanInteract(context) && ice.GetUnavailablePrompt(context).Contains("无法在物理上携带冰块"),
            "ingredient scoop is explicitly forbidden from taking ice");
        PlaceAt(workstation, "scoop_free");
        Tool(main, "ice_tongs").Interact(context);
        ice.Interact(context);
        Require(Math.Abs(workstation.GetRightHandIngredientAmount("ice") - 1d) < 0.001d,
            "ice tongs can take one piece from the opened drawer bucket");
        bin.Interact(context);
        PlaceAt(workstation, "tongs_free");

        Tool(main, "jigger_small").Interact(context);
        var before = workstation.KettleWaterAmountMl;
        kettle.Interact(context);
        Require(Math.Abs(workstation.GetRightHandIngredientAmount("water") - 30d) < 0.001d &&
                Math.Abs(workstation.KettleWaterAmountMl - (before - 30d)) < 0.001d,
            "jigger takes its selected measured amount from the kettle");
        bin.Interact(context);
        PlaceAt(workstation, "jigger_free");
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

    private static ToolInteractable Tool(Node3D main, string id) =>
        main.GetNode<ToolInteractable>($"NeutralGameplay/{id}");

    private static StationInteractable Station(Node3D main, string id)
    {
        foreach (var node in main.GetTree().GetNodesInGroup("interactable"))
            if (node is StationInteractable station && station.EntityId == id)
                return station;
        throw new InvalidOperationException($"Station not found: {id}");
    }

    private static void PlaceAt(DrinkWorkstation workstation, string id)
    {
        Require(workstation.TryPlaceHeldToolAtPosition(Position(id), out var feedback), feedback);
    }

    private static Vector3 Position(string id) => id switch
    {
        "scoop_free" => new Vector3(-2.8f, 1.82f, -2.92f),
        "tongs_free" => new Vector3(-2.05f, 1.82f, -2.92f),
        "pestle_free" => new Vector3(-1.3f, 1.82f, -2.92f),
        "mortar_free" => new Vector3(-0.45f, 1.82f, -2.92f),
        "jigger_free" => new Vector3(1.2f, 1.82f, -2.92f),
        "jigger_large_free" => new Vector3(1.9f, 1.82f, -2.92f),
        "glass_free" => new Vector3(2.6f, 1.82f, -2.92f),
        _ => throw new InvalidOperationException($"Unknown test placement: {id}")
    };

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
