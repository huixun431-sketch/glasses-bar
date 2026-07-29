using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlassesBar.Domain;
using NUnit.Framework;

namespace GlassesBar.Tests;

public sealed class DomainTests
{
    [Test]
    public void DayFlow_OnlyAcceptsApprovedSequence()
    {
        var flow = new DayFlow();
        Assert.That(flow.TryAdvance(DayPhase.Preparation), Is.False);
        Assert.That(flow.TryAdvance(DayPhase.OrderReceived), Is.True);
        Assert.That(flow.TryAdvance(DayPhase.RecipeObservation), Is.True);
        Assert.That(flow.TryAdvance(DayPhase.Preparation), Is.True);
        Assert.That(flow.TryAdvance(DayPhase.Delivery), Is.True);
        Assert.That(flow.TryAdvance(DayPhase.Evaluation), Is.True);
        Assert.That(flow.TryAdvance(DayPhase.DaySummary), Is.True);
    }

    [Test]
    public void LiquidTransfer_ConservesAmountAndTracksSpill()
    {
        var result = LiquidMath.Transfer(5d, 2d, 4d, 3d);
        Assert.That(result.SourceAfter, Is.EqualTo(2d));
        Assert.That(result.DestinationAfter, Is.EqualTo(4d));
        Assert.That(result.Transferred, Is.EqualTo(2d));
        Assert.That(result.Spilled, Is.EqualTo(1d));
        Assert.That(result.SourceAfter + result.DestinationAfter + result.Spilled, Is.EqualTo(7d));
    }

    [Test]
    public void PrototypeRecipe_PassesOnRequiredStepsAndIngredientsWithoutQuantityGate()
    {
        var targets = new RecipeTargets { IsPrototype = true };
        targets.RequiredSteps.UnionWith(new[] { "take_glass", "add_water" });
        targets.RequiredIngredients.Add("water");
        targets.TargetAmounts["water"] = 1000d;

        var drink = new DrinkSnapshot();
        drink.CompletedSteps.UnionWith(new[] { "take_glass", "add_water" });
        drink.IngredientAmounts["water"] = 0.1d;

        var evaluation = RecipeEvaluator.Evaluate(targets, drink);
        Assert.That(evaluation.Passed, Is.True);
        Assert.That(evaluation.QuantityAccuracyRatio, Is.Zero);
    }

    [Test]
    public void FormalRecipe_UsesToleranceGate()
    {
        var targets = new RecipeTargets
        {
            IsPrototype = false,
            EnableQuantityScoring = true,
            AmountToleranceRatio = 0.1d
        };
        targets.RequiredSteps.Add("pour");
        targets.RequiredIngredients.Add("water");
        targets.TargetAmounts["water"] = 100d;
        var drink = new DrinkSnapshot();
        drink.CompletedSteps.Add("pour");
        drink.IngredientAmounts["water"] = 120d;

        Assert.That(RecipeEvaluator.Evaluate(targets, drink).Passed, Is.False);
        drink.IngredientAmounts["water"] = 105d;
        Assert.That(RecipeEvaluator.Evaluate(targets, drink).Passed, Is.True);
    }

    [Test]
    public void ToolAndOperationCategories_AreDerivedFromCapabilities()
    {
        var glass = new ToolSpec { Id = "glass", CanContainIngredients = true };
        var pestle = new ToolSpec { Id = "pestle", UsedInHand = true };
        Assert.That(glass.ResolveCategory(), Is.EqualTo(ToolCategory.Placement));
        Assert.That(pestle.ResolveCategory(), Is.EqualTo(ToolCategory.Handheld));

        var simple = new OperationSpec { Id = "water", CanRunOffBoard = true, RequiredHandheldToolId = "carafe" };
        var normal = new OperationSpec { Id = "filter" };
        var complex = new OperationSpec { Id = "grind", RequiredHandheldToolId = "pestle" };
        Assert.That(simple.ResolveComplexity(), Is.EqualTo(OperationComplexity.Simple));
        Assert.That(normal.ResolveComplexity(), Is.EqualTo(OperationComplexity.Normal));
        Assert.That(complex.ResolveComplexity(), Is.EqualTo(OperationComplexity.Complex));
    }

    [Test]
    public void ProcessRules_AllowMistakesAndTurnWrongAttemptsIntoWaste()
    {
        var grind = new OperationSpec
        {
            Id = "grind",
            RequiredHandheldToolId = "pestle",
            RequiredAction = 0.5d
        };
        grind.InputTargets["coffee_beans"] = 1d;

        var wrongTool = ProcessRules.Evaluate(grind, "ice_tongs",
            new Dictionary<string, double> { ["coffee_beans"] = 1d }, 1d, 0d);
        Assert.That(wrongTool.Failure, Is.EqualTo(ProcessFailure.WrongHandheldTool));
        Assert.That(wrongTool.MaterialsBecomeWaste, Is.True);

        var wrongIngredient = ProcessRules.Evaluate(grind, "pestle",
            new Dictionary<string, double> { ["ice"] = 1d }, 1d, 0d);
        Assert.That(wrongIngredient.Failure, Is.EqualTo(ProcessFailure.WrongIngredients));
        Assert.That(wrongIngredient.MaterialsBecomeWaste, Is.True);
    }

    [Test]
    public void ProcessRules_UseDeviationForProbabilityAndCompletion()
    {
        var operation = new OperationSpec { Id = "pour", RequiredAction = 0d };
        operation.InputTargets["water"] = 1d;

        var success = ProcessRules.Evaluate(operation, string.Empty,
            new Dictionary<string, double> { ["water"] = 1.2d }, 1d, 0.5d);
        Assert.That(success.Completed, Is.True);
        Assert.That(success.SuccessProbability, Is.EqualTo(0.8d).Within(0.000001d));
        Assert.That(success.CompletionRatio, Is.EqualTo(0.8d).Within(0.000001d));

        var failure = ProcessRules.Evaluate(operation, string.Empty,
            new Dictionary<string, double> { ["water"] = 1.2d }, 1d, 0.9d);
        Assert.That(failure.Failure, Is.EqualTo(ProcessFailure.ProportionCheckFailed));
        Assert.That(failure.MaterialsBecomeWaste, Is.True);
    }

    [Test]
    public void ProcessRules_TreatFloatingPointNoiseAsExact()
    {
        var operation = new OperationSpec { Id = "extract", RequiredAction = 0d };
        operation.InputTargets["water"] = 0.5d;
        var result = ProcessRules.Evaluate(operation, string.Empty,
            new Dictionary<string, double> { ["water"] = 0.50000001d }, 1d, 0.999999d);

        Assert.That(result.Completed, Is.True);
        Assert.That(result.SuccessProbability, Is.EqualTo(1d));
    }

    [Test]
    public void ProcessRules_AcceptThreeJiggersAndApplyDailyHygienePenalty()
    {
        var operation = new OperationSpec { Id = "measured_water", RequiredAction = 0d };
        operation.AllowedHandheldToolIds.UnionWith(new[] { "jigger_small", "jigger_medium", "jigger_large" });
        operation.InputTargets["water"] = 30d;

        var washed = ProcessRules.Evaluate(operation, "jigger_small",
            new Dictionary<string, double> { ["water"] = 30d }, 1d, 0.98d);
        var unwashed = ProcessRules.Evaluate(operation, "jigger_small",
            new Dictionary<string, double> { ["water"] = 30d }, 1d, 0.98d, 0.04d);
        var wrongTool = ProcessRules.Evaluate(operation, "bean_scoop",
            new Dictionary<string, double> { ["water"] = 30d }, 1d, 0d);

        Assert.That(washed.Completed, Is.True);
        Assert.That(unwashed.Failure, Is.EqualTo(ProcessFailure.ProportionCheckFailed));
        Assert.That(unwashed.SuccessProbability, Is.EqualTo(0.96d).Within(0.000001d));
        Assert.That(wrongTool.Failure, Is.EqualTo(ProcessFailure.WrongHandheldTool));
    }

    [Test]
    public void RepeatRecovery_IsPartialAndCapped()
    {
        var first = ProcessRules.RecoverCompletion(0.72d, 0.96d, 0.42d);
        var second = ProcessRules.RecoverCompletion(first, 0.96d, 0.42d);
        Assert.That(first, Is.GreaterThan(0.72d).And.LessThan(0.96d));
        Assert.That(second, Is.GreaterThan(first).And.LessThanOrEqualTo(0.96d));
        Assert.That(ProcessRules.RecoverCompletion(0.96d, 0.96d, 1d), Is.EqualTo(0.96d));
    }

    [Test]
    public void ToolInventoryService_OwnsHandsAndRejectsInvalidCounterPlacement()
    {
        var inventory = new ToolInventoryService();
        inventory.RegisterTool(new ToolSpec
        {
            Id = "glass",
            DisplayName = "Glass",
            Category = ToolCategory.Placement,
            FootprintRadius = 0.2d
        }, new SpatialPosition(0d, 0d, 0d));
        inventory.RegisterTool(new ToolSpec
        {
            Id = "mortar",
            DisplayName = "Mortar",
            Category = ToolCategory.Placement,
            FootprintRadius = 0.2d
        }, new SpatialPosition(1d, 0d, 0d));
        var scoop = new ToolSpec
        {
            Id = "scoop",
            DisplayName = "Scoop",
            Category = ToolCategory.Handheld,
            CanCarryIngredients = true
        };
        scoop.AllowedIngredientIds.Add("coffee_beans");
        inventory.RegisterTool(scoop, new SpatialPosition(2d, 0d, 0d));

        inventory.PickUp("glass");
        Assert.That(inventory.LeftHandToolId, Is.EqualTo("glass"));
        Assert.That(inventory.CheckPickUp("mortar").Failure, Is.EqualTo(ToolInventoryFailure.HandOccupied));
        Assert.That(inventory.CheckCounterPlacement(new SpatialPosition(1.1d, 0d, 0d)).Failure,
            Is.EqualTo(ToolInventoryFailure.CounterOverlap));

        inventory.PlaceHeldToolAt(new SpatialPosition(3d, 0d, 0d));
        inventory.PickUp("scoop");
        inventory.LoadIngredient("coffee_beans", 1d);
        Assert.That(inventory.CheckCounterPlacement(new SpatialPosition(4d, 0d, 0d)).Failure,
            Is.EqualTo(ToolInventoryFailure.LoadedHandheldCannotBePlaced));
    }

    [Test]
    public void ToolInventoryService_MovesBoardContentsWithoutPresentationState()
    {
        var inventory = new ToolInventoryService();
        inventory.RegisterTool(new ToolSpec
        {
            Id = "mortar",
            DisplayName = "Mortar",
            Category = ToolCategory.Placement,
            CanContainIngredients = true
        }, new SpatialPosition(0d, 0d, 0d));
        var scoop = new ToolSpec
        {
            Id = "scoop",
            DisplayName = "Scoop",
            Category = ToolCategory.Handheld,
            CanCarryIngredients = true
        };
        scoop.AllowedIngredientIds.Add("coffee_beans");
        inventory.RegisterTool(scoop, new SpatialPosition(1d, 0d, 0d));

        inventory.PickUp("mortar");
        inventory.PlaceLeftHandOnBoard(new[] { new SpatialPosition(5d, 0d, 5d) });
        inventory.PickUp("scoop");
        inventory.LoadIngredient("coffee_beans", 1d);
        inventory.DepositRightHandContentsOnBoard();

        Assert.That(inventory.GetRequiredTool("mortar").Contents["coffee_beans"], Is.EqualTo(1d));
        Assert.That(inventory.GetRequiredTool("scoop").Contents, Is.Empty);

        inventory.CollectBoardContents(new HashSet<string> { "coffee_beans" });
        Assert.That(inventory.GetRequiredTool("mortar").Contents, Is.Empty);
        Assert.That(inventory.GetRequiredTool("scoop").Contents["coffee_beans"], Is.EqualTo(1d));
        Assert.That(inventory.BoardToolIds, Is.EqualTo(new[] { "mortar" }));
    }

    [Test]
    public void ToolInventoryService_CapturesRestoresAndResetsAuthoritativeState()
    {
        var inventory = new ToolInventoryService();
        var jigger = new ToolSpec
        {
            Id = "jigger",
            DisplayName = "Jigger",
            Category = ToolCategory.Handheld,
            CanCarryIngredients = true,
            SmallMeasureAmount = 15d,
            LargeMeasureAmount = 30d
        };
        jigger.AllowedIngredientIds.Add("water");
        inventory.RegisterTool(jigger, new SpatialPosition(1d, 2d, 3d));

        inventory.PickUp("jigger");
        inventory.LoadIngredient("water", 15d);
        inventory.GetRequiredTool("jigger").UseLargeMeasureSide = false;
        var snapshots = inventory.CaptureToolSnapshots();

        inventory.ResetAll();
        Assert.That(inventory.RightHandToolId, Is.Empty);
        Assert.That(inventory.GetRequiredTool("jigger").Contents, Is.Empty);

        inventory.RestoreState(snapshots, string.Empty, "jigger", new List<string>());
        Assert.That(inventory.RightHandToolId, Is.EqualTo("jigger"));
        Assert.That(inventory.GetRequiredTool("jigger").Location, Is.EqualTo(ToolLocation.RightHand));
        Assert.That(inventory.GetRequiredTool("jigger").Contents["water"], Is.EqualTo(15d));
        Assert.That(inventory.GetRequiredTool("jigger").UseLargeMeasureSide, Is.False);
    }

    [Test]
    public void ProcessExecutionService_SelectsExactBoardOperationAndCommitsOutput()
    {
        var inventory = new ToolInventoryService();
        inventory.RegisterTool(new ToolSpec
        {
            Id = "mortar",
            DisplayName = "Mortar",
            Category = ToolCategory.Placement,
            CanContainIngredients = true
        }, new SpatialPosition(0d, 0d, 0d));
        inventory.RegisterTool(new ToolSpec
        {
            Id = "pestle",
            DisplayName = "Pestle",
            Category = ToolCategory.Handheld,
            UsedInHand = true
        }, new SpatialPosition(1d, 0d, 0d));
        inventory.PickUp("mortar");
        inventory.PlaceLeftHandOnBoard(new[] { new SpatialPosition(5d, 0d, 5d) });
        inventory.PickUp("pestle");
        inventory.GetRequiredTool("mortar").Contents["coffee_beans"] = 1d;

        var wrongCandidate = new OperationSpec
        {
            Id = "crush_ice",
            DisplayName = "Crush Ice",
            RequiredHandheldToolId = "pestle",
            ResultTargetToolId = "mortar",
            RequiredAction = 0.5d
        };
        wrongCandidate.RequiredPlacementToolIds.Add("mortar");
        wrongCandidate.InputTargets["ice"] = 1d;
        wrongCandidate.Outputs["crushed_ice"] = 1d;
        var grind = new OperationSpec
        {
            Id = "manual_grind",
            DisplayName = "Manual Grind",
            RequiredHandheldToolId = "pestle",
            ResultTargetToolId = "mortar",
            RequiredAction = 0.5d
        };
        grind.RequiredPlacementToolIds.Add("mortar");
        grind.InputTargets["coffee_beans"] = 1d;
        grind.Outputs["ground_coffee"] = 1d;

        var assembly = new DrinkAssemblyState(300d);
        var service = new ProcessExecutionService(inventory, assembly);
        service.ConfigureOperations(new[] { wrongCandidate, grind });

        Assert.That(service.SelectBoardOperation()?.Id, Is.EqualTo("manual_grind"));
        var outcome = service.ExecuteBoardOperation(
            grind,
            1d,
            () => 0d,
            0d,
            true);

        Assert.That(outcome.Kind, Is.EqualTo(ProcessExecutionKind.Completed));
        Assert.That(inventory.GetRequiredTool("mortar").Contents["ground_coffee"], Is.EqualTo(1d));
        Assert.That(inventory.GetRequiredTool("mortar").Contents.ContainsKey("coffee_beans"), Is.False);
        Assert.That(assembly.CompletedSteps, Does.Contain("manual_grind"));
        Assert.That(assembly.FailedOperations, Is.Zero);
    }

    [Test]
    public void ProcessExecutionService_TracksAcceptedLiquidAndSpillForSimpleOutput()
    {
        var inventory = new ToolInventoryService();
        inventory.RegisterTool(new ToolSpec
        {
            Id = "highball_glass",
            DisplayName = "Glass",
            Category = ToolCategory.Placement,
            CanContainIngredients = true
        }, new SpatialPosition(0d, 0d, 0d));
        var jigger = new ToolSpec
        {
            Id = "jigger",
            DisplayName = "Jigger",
            Category = ToolCategory.Handheld,
            CanCarryIngredients = true
        };
        jigger.AllowedIngredientIds.Add("water");
        inventory.RegisterTool(jigger, new SpatialPosition(1d, 0d, 0d));
        inventory.PickUp("highball_glass");
        inventory.PickUp("jigger");
        inventory.LoadIngredient("water", 30d);

        var addWater = new OperationSpec
        {
            Id = "add_water",
            DisplayName = "Add Water",
            CanRunOffBoard = true,
            RequiredHandheldToolId = "jigger",
            ResultTargetToolId = "highball_glass"
        };
        addWater.RequiredPlacementToolIds.Add("highball_glass");
        addWater.InputTargets["water"] = 30d;
        addWater.Outputs["water"] = 30d;

        var assembly = new DrinkAssemblyState(20d);
        var service = new ProcessExecutionService(inventory, assembly);
        service.ConfigureOperations(new[] { addWater });
        var outcome = service.ExecuteSimpleOperation(() => 0d, 0d);

        Assert.That(outcome, Is.Not.Null);
        Assert.That(outcome!.Kind, Is.EqualTo(ProcessExecutionKind.Completed));
        Assert.That(assembly.Glass.CurrentAmount, Is.EqualTo(20d));
        Assert.That(assembly.Glass.SpilledAmount, Is.EqualTo(10d));
        Assert.That(inventory.GetRequiredTool("highball_glass").Contents["water"], Is.EqualTo(20d));
        Assert.That(assembly.IngredientAmounts["water"], Is.EqualTo(20d));
        Assert.That(assembly.SpilledAmount, Is.EqualTo(10d));
        Assert.That(inventory.GetRequiredTool("jigger").Contents, Is.Empty);
    }

    [Test]
    public void ProcessExecutionService_MarksWrongIngredientsAsWaste()
    {
        var inventory = new ToolInventoryService();
        inventory.RegisterTool(new ToolSpec
        {
            Id = "mortar",
            DisplayName = "Mortar",
            Category = ToolCategory.Placement,
            CanContainIngredients = true
        }, new SpatialPosition(0d, 0d, 0d));
        inventory.RegisterTool(new ToolSpec
        {
            Id = "pestle",
            DisplayName = "Pestle",
            Category = ToolCategory.Handheld,
            UsedInHand = true
        }, new SpatialPosition(1d, 0d, 0d));
        inventory.PickUp("mortar");
        inventory.PlaceLeftHandOnBoard(new[] { new SpatialPosition(5d, 0d, 5d) });
        inventory.PickUp("pestle");
        inventory.GetRequiredTool("mortar").Contents["ice"] = 1d;

        var grind = new OperationSpec
        {
            Id = "manual_grind",
            DisplayName = "Manual Grind",
            RequiredHandheldToolId = "pestle",
            ResultTargetToolId = "mortar",
            RequiredAction = 0.5d
        };
        grind.RequiredPlacementToolIds.Add("mortar");
        grind.InputTargets["coffee_beans"] = 1d;
        grind.Outputs["ground_coffee"] = 1d;

        var assembly = new DrinkAssemblyState(300d);
        var service = new ProcessExecutionService(inventory, assembly);
        service.ConfigureOperations(new[] { grind });
        var outcome = service.ExecuteBoardOperation(
            grind,
            1d,
            () => 0d,
            0d,
            true);

        Assert.That(outcome.Kind, Is.EqualTo(ProcessExecutionKind.Failed));
        Assert.That(outcome.Attempt.Failure, Is.EqualTo(ProcessFailure.WrongIngredients));
        Assert.That(inventory.GetRequiredTool("mortar").ContentsAreWaste, Is.True);
        Assert.That(assembly.FailedOperations, Is.EqualTo(1));
        Assert.That(assembly.CompletedSteps, Does.Not.Contain("manual_grind"));
    }

    [Test]
    public void ProcessExecutionService_PreservesRandomRollAcrossBlocksAndRecoversPartially()
    {
        var inventory = new ToolInventoryService();
        inventory.RegisterTool(new ToolSpec
        {
            Id = "filter",
            DisplayName = "Filter",
            Category = ToolCategory.Placement,
            CanContainIngredients = true
        }, new SpatialPosition(0d, 0d, 0d));
        inventory.PickUp("filter");
        inventory.PlaceLeftHandOnBoard(new[] { new SpatialPosition(5d, 0d, 5d) });
        var filter = inventory.GetRequiredTool("filter");
        filter.Contents["ground_coffee"] = 1d;

        var extract = new OperationSpec
        {
            Id = "manual_extract",
            DisplayName = "Manual Extract",
            ResultTargetToolId = "filter",
            RequiredAction = 0.5d,
            RepeatRecoveryInputIngredientId = "coffee_extract",
            RepeatRecoveryCap = 0.96d,
            RepeatRecoveryFraction = 0.42d
        };
        extract.RequiredPlacementToolIds.Add("filter");
        extract.InputTargets["ground_coffee"] = 1d;
        extract.InputTargets["water"] = 30d;
        extract.Outputs["coffee_extract"] = 30d;
        var assembly = new DrinkAssemblyState(300d);
        var service = new ProcessExecutionService(inventory, assembly);
        service.ConfigureOperations(new[] { extract });
        var rollCalls = 0;
        double NextRoll()
        {
            rollCalls++;
            return 0d;
        }

        var dry = service.ExecuteBoardOperation(
            extract,
            1d,
            NextRoll,
            0d,
            false);
        Assert.That(dry.Kind, Is.EqualTo(ProcessExecutionKind.NonDestructiveBlock));
        Assert.That(dry.BlockReason, Is.EqualTo(ProcessBlockReason.KettleEmpty));
        Assert.That(rollCalls, Is.Zero);
        Assert.That(filter.ContentsAreWaste, Is.False);
        Assert.That(filter.Contents["ground_coffee"], Is.EqualTo(1d));

        filter.Contents.Clear();
        filter.Contents["coffee_extract"] = 30d;
        filter.ContentCompletionRatio = 0.72d;

        var incomplete = service.ExecuteBoardOperation(
            extract,
            0.1d,
            NextRoll,
            0d,
            true);
        Assert.That(incomplete.BlockReason, Is.EqualTo(ProcessBlockReason.RepeatActionIncomplete));
        Assert.That(rollCalls, Is.Zero);
        Assert.That(filter.ContentCompletionRatio, Is.EqualTo(0.72d));

        var recovered = service.ExecuteBoardOperation(
            extract,
            1d,
            NextRoll,
            0d,
            true);
        Assert.That(recovered.Kind, Is.EqualTo(ProcessExecutionKind.RepeatRecovery));
        Assert.That(recovered.FullRecovery, Is.True);
        Assert.That(recovered.Attempt.CompletionRatio, Is.GreaterThan(0.72d).And.LessThanOrEqualTo(0.96d));
        Assert.That(rollCalls, Is.EqualTo(1));
        Assert.That(service.RepeatRecoveryCounts["manual_extract"], Is.EqualTo(1));
    }

    [Test]
    public void DrinkAssemblyState_DiscardedDrinkDoesNotLeakIntoRemakeEvaluation()
    {
        var assembly = new DrinkAssemblyState(300d);
        assembly.AddProcessOutput("water", 30d);
        assembly.RecordCompletedOperation("add_water", 0.72d);
        var glassTool = new ToolInstanceState
        {
            Definition = new ToolSpec { Id = "highball_glass", DisplayName = "Glass" },
            InitialPosition = new SpatialPosition(0d, 0d, 0d)
        };
        glassTool.Contents["water"] = 30d;
        glassTool.ContentCompletionRatio = 0.72d;

        Assert.That(assembly.DiscardToolContents(glassTool), Is.EqualTo(30d));
        assembly.AddProcessOutput("ice", 2d);

        var targets = new RecipeTargets();
        targets.RequiredIngredients.Add("water");
        var evaluation = assembly.Evaluate(targets, 1d);

        Assert.That(evaluation.Passed, Is.False);
        Assert.That(evaluation.MissingIngredients, Does.Contain("water"));
        Assert.That(assembly.IngredientAmounts.ContainsKey("water"), Is.False);
        Assert.That(assembly.IngredientAmounts["ice"], Is.EqualTo(2d));
        Assert.That(assembly.WastedAmount, Is.EqualTo(30d));
        Assert.That(assembly.Glass.CurrentAmount, Is.EqualTo(2d));
    }

    [Test]
    public void DrinkAssemblyState_ResetClearsDrinkAndDayMetrics()
    {
        var assembly = new DrinkAssemblyState(10d);
        assembly.AdvanceElapsed(5d);
        assembly.AddProcessOutput("water", 12d);
        assembly.RecordCompletedOperation("add_water", 0.6d);
        assembly.RecordFailedOperation();
        var carrier = new ToolInstanceState
        {
            Definition = new ToolSpec { Id = "jigger", DisplayName = "Jigger" },
            InitialPosition = new SpatialPosition(0d, 0d, 0d)
        };
        carrier.Contents["water"] = 3d;
        assembly.DiscardToolContents(carrier);

        assembly.ResetForNewDay(300d);

        Assert.That(assembly.Glass.Capacity, Is.EqualTo(300d));
        Assert.That(assembly.Glass.CurrentAmount, Is.Zero);
        Assert.That(assembly.ElapsedSeconds, Is.Zero);
        Assert.That(assembly.WastedAmount, Is.Zero);
        Assert.That(assembly.SpilledAmount, Is.Zero);
        Assert.That(assembly.FailedOperations, Is.Zero);
        Assert.That(assembly.CraftCompletionRatio, Is.EqualTo(1d));
        Assert.That(assembly.CompletedSteps, Is.Empty);
        Assert.That(assembly.IngredientAmounts, Is.Empty);
    }

    [Test]
    public void DrinkAssemblyState_RestoresVersionOneSnapshotSemantics()
    {
        var assembly = new DrinkAssemblyState(15d);
        assembly.AdvanceElapsed(4d);
        assembly.AddProcessOutput("water", 20d);
        assembly.RecordCompletedOperation("add_water", 0.8d);
        assembly.RecordFailedOperation();
        var glass = assembly.CaptureGlassSnapshot();
        var completedSteps = assembly.CaptureCompletedSteps();

        var restored = new DrinkAssemblyState(1d);
        restored.Restore(glass, assembly.ElapsedSeconds, 7d, assembly.FailedOperations, completedSteps);

        Assert.That(restored.Glass.Capacity, Is.EqualTo(15d));
        Assert.That(restored.Glass.CurrentAmount, Is.EqualTo(15d));
        Assert.That(restored.Glass.SpilledAmount, Is.EqualTo(5d));
        Assert.That(restored.ElapsedSeconds, Is.EqualTo(4d));
        Assert.That(restored.WastedAmount, Is.EqualTo(7d));
        Assert.That(restored.SpilledAmount, Is.EqualTo(5d));
        Assert.That(restored.FailedOperations, Is.EqualTo(1));
        Assert.That(restored.CompletedSteps, Does.Contain("add_water"));
        Assert.That(restored.CraftCompletionRatio, Is.EqualTo(1d));
        Assert.That(restored.IngredientAmounts, Is.Empty);
    }

    [Test]
    public void LiquidContainer_RemovalPreservesIngredientRatios()
    {
        var liquid = new LiquidContainer(100d);
        liquid.Add("water", 30d);
        liquid.Add("coffee_extract", 10d);

        Assert.That(liquid.Remove(20d), Is.EqualTo(20d));
        Assert.That(liquid.CurrentAmount, Is.EqualTo(20d));
        Assert.That(liquid.Ingredients["water"], Is.EqualTo(15d));
        Assert.That(liquid.Ingredients["coffee_extract"], Is.EqualTo(5d));
        Assert.That(
            () => liquid.Restore(new Dictionary<string, double> { ["water"] = 101d }, 0d),
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void MyopiaProgression_UsesThirtyDayCampaignCurve()
    {
        Assert.That(MyopiaProgression.DegreesForDay(1), Is.EqualTo(50f));
        Assert.That(MyopiaProgression.DegreesForDay(3), Is.EqualTo(50f));
        Assert.That(MyopiaProgression.DegreesForDay(4), Is.EqualTo(75f));
        Assert.That(MyopiaProgression.DegreesForDay(21), Is.EqualTo(200f));
        Assert.That(MyopiaProgression.DegreesForDay(22), Is.EqualTo(250f));
        Assert.That(MyopiaProgression.DegreesForDay(27), Is.EqualTo(300f));
        Assert.That(MyopiaProgression.DegreesForDay(30), Is.EqualTo(350f));
        Assert.That(MyopiaProgression.DegreesForDay(999), Is.LessThanOrEqualTo(MyopiaProgression.MaximumDegrees));
    }

    [Test]
    public void SaveSnapshot_RoundTripsVersionedAuthoritativeState()
    {
        var snapshot = new GameSaveSnapshot
        {
            CurrentDay = 7,
            DayPhase = DayPhase.Preparation,
            WorldModeId = "glasses",
            GameStarted = true,
            RecipeObserved = true,
            Workstation = new WorkstationSnapshot
            {
                LeftHandToolId = "glass",
                BoardToolIds = new List<string> { "filter" },
                HandsWashedToday = true,
                KettleWaterAmountMl = 1230d,
                WastedAmount = 2d,
                Glass = new LiquidSnapshot { Capacity = 300d },
                Tools = new List<ToolInstanceSnapshot>
                {
                    new()
                    {
                        ToolId = "glass",
                        Location = ToolLocation.LeftHand,
                        Position = new SpatialPosition(1d, 2d, 3d),
                        Contents = new Dictionary<string, double> { ["water"] = 30d }
                    },
                    new()
                    {
                        ToolId = "filter",
                        Location = ToolLocation.Workboard,
                        BoardSlot = 0,
                        Position = new SpatialPosition(0d, 1d, 0d)
                    }
                }
            }
        };

        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(snapshot));
        Assert.That(restored.SchemaVersion, Is.EqualTo(GameSaveSnapshot.CurrentSchemaVersion));
        Assert.That(restored.CurrentDay, Is.EqualTo(7));
        Assert.That(restored.WorldModeId, Is.EqualTo("glasses"));
        Assert.That(restored.Workstation.Tools[0].Contents["water"], Is.EqualTo(30d));
    }

    [Test]
    public void SaveSnapshot_RejectsUnknownFutureSchema()
    {
        var snapshot = new GameSaveSnapshot { SchemaVersion = GameSaveSnapshot.CurrentSchemaVersion + 1 };
        Assert.That(() => SaveGameSerializer.Serialize(snapshot), Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public void GameplayActionDefinitions_HaveStableUniqueIdsAndExplicitModes()
    {
        var definitions = typeof(GameplayActionDefinitions).GetFields()
            .Select(field => (GameplayActionDefinition)field.GetValue(null)!)
            .ToArray();

        Assert.That(definitions, Is.Not.Empty);
        Assert.That(definitions.Select(definition => definition.Id).Distinct().Count(), Is.EqualTo(definitions.Length));
        Assert.That(definitions.Single(definition => definition.Id == "process.run_board").Mode,
            Is.EqualTo(GameplayActionMode.Continuous));
        Assert.That(definitions.Where(definition => definition.Id != "process.run_board")
            .All(definition => definition.Mode == GameplayActionMode.Instant), Is.True);
    }

    [Test]
    public void GameplayCatalogValidation_RejectsBrokenCrossReferences()
    {
        var tools = new Dictionary<string, ToolSpec>
        {
            ["glass"] = new()
            {
                Id = "glass",
                DisplayName = "Glass",
                CanContainIngredients = true
            }
        };
        var operation = new OperationSpec
        {
            Id = "pour",
            DisplayName = "Pour",
            RequiredHandheldToolId = "missing_jigger",
            ResultTargetToolId = "glass"
        };
        operation.RequiredPlacementToolIds.Add("glass");
        operation.InputTargets["water"] = 30d;
        operation.Outputs["water"] = 30d;

        Assert.That(
            () => GameplayCatalogValidator.Validate(tools, new[] { operation }),
            Throws.TypeOf<GameplayCatalogValidationException>()
                .With.Message.Contains("missing_jigger"));
    }

    [Test]
    public void GameplayCatalogValidation_RejectsRecipeOperationDrift()
    {
        var recipe = new RecipeTargets { Id = "broken_recipe" };
        recipe.RequiredSteps.Add("missing_step");
        recipe.RequiredIngredients.Add("missing_output");

        Assert.That(
            () => GameplayCatalogValidator.ValidateRecipeCompatibility(recipe, new List<OperationSpec>()),
            Throws.TypeOf<GameplayCatalogValidationException>());
    }

    [Test]
    public void SettingsState_NormalizesSharedMenuRanges()
    {
        var minimum = SettingsState.Create(-20d, -1d);
        Assert.That(minimum.MasterVolumePercent, Is.EqualTo(0d));
        Assert.That(minimum.MouseSensitivity, Is.EqualTo(0.001d));
        Assert.That(minimum.MouseSensitivitySliderValue, Is.EqualTo(1d));

        var maximum = minimum
            .WithMasterVolumePercent(140d)
            .WithMouseSensitivity(0.02d);
        Assert.That(maximum.MasterVolumePercent, Is.EqualTo(100d));
        Assert.That(maximum.MouseSensitivity, Is.EqualTo(0.006d));
        Assert.That(maximum.MouseSensitivitySliderValue, Is.EqualTo(6d));
    }

}
