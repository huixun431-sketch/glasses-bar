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
}
