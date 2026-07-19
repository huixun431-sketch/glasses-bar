using System;
using System.Linq;
using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

public partial class DrinkWorkstation : Node
{
    [Signal] public delegate void DrinkChangedEventHandler(string debugText);
    [Signal] public delegate void GlassHeldChangedEventHandler(bool held);

    private readonly DrinkSnapshot _snapshot = new();
    private RecipeTargets _recipeTargets = new() { IsPrototype = true };
    private bool _timing;

    public LiquidContainer Glass { get; private set; } = new(3d);
    public bool HasGlass { get; private set; }
    public int IcePieces { get; private set; }
    public bool GroundCoffeeReady { get; private set; }
    public double TotalWaste { get; private set; }

    public override void _Ready()
    {
        var recipe = ResourceLoader.Load<RecipeDefinition>("res://data/recipes/prototype_iced_americano.tres");
        if (recipe is null)
            throw new InvalidOperationException("Prototype recipe resource could not be loaded.");
        _recipeTargets = recipe.BuildTargets();
        GameSession.Instance.DayPhaseChanged += OnPhaseChanged;
    }

    public override void _Process(double delta)
    {
        if (_timing)
            _snapshot.ElapsedSeconds += Math.Max(0d, delta);
    }

    public void TakeGlass()
    {
        if (HasGlass)
            return;
        HasGlass = true;
        MarkStep("take_glass");
        EmitSignal(SignalName.GlassHeldChanged, true);
    }

    public void AddIce()
    {
        if (!HasGlass)
            return;
        IcePieces++;
        _snapshot.IngredientAmounts["ice"] = IcePieces;
        MarkStep("add_ice");
    }

    public double AddLiquid(string id, double amount)
    {
        if (!HasGlass)
            return 0d;
        var beforeSpill = Glass.SpilledAmount;
        var transferred = Glass.Add(id, amount);
        _snapshot.IngredientAmounts.TryGetValue(id, out var existing);
        _snapshot.IngredientAmounts[id] = existing + transferred;
        _snapshot.SpilledAmount += Glass.SpilledAmount - beforeSpill;
        EmitChanged();
        return transferred;
    }

    public void MarkWaterComplete() => MarkStep("add_water");

    public void MarkGroundCoffee()
    {
        GroundCoffeeReady = true;
        MarkStep("grind_coffee");
    }

    public void MarkEspressoComplete() => MarkStep("extract_espresso");

    public void DiscardAndReset()
    {
        var discarded = Glass.Empty() + IcePieces;
        TotalWaste += discarded;
        _snapshot.WastedAmount += discarded;
        _snapshot.CompletedSteps.Clear();
        _snapshot.IngredientAmounts.Clear();
        _snapshot.SpilledAmount = 0d;
        IcePieces = 0;
        GroundCoffeeReady = false;
        HasGlass = false;
        Glass = new LiquidContainer(3d);
        EmitSignal(SignalName.GlassHeldChanged, false);
        EmitChanged();
        GameSession.Instance.EmitSignal(GameSession.SignalName.StatusMessage, "已丢弃成品，可重新拿杯制作；浪费已记录。");
    }

    public DrinkEvaluation EvaluateAndFinish()
    {
        _timing = false;
        var evaluation = RecipeEvaluator.Evaluate(_recipeTargets, _snapshot);
        if (GameSession.Instance.BeginDelivery())
            GameSession.Instance.FinishEvaluation(evaluation);
        return evaluation;
    }

    public string GetDebugText()
    {
        var ingredients = _snapshot.IngredientAmounts.Count == 0
            ? "无"
            : string.Join("，", _snapshot.IngredientAmounts.Select(pair => $"{pair.Key}:{pair.Value:0.00}"));
        return $"杯:{(HasGlass ? "手持" : "未拿取")}｜冰:{IcePieces}｜液体:{Glass.CurrentAmount:0.00}/3.00｜{ingredients}｜溢出:{_snapshot.SpilledAmount:0.00}";
    }

    private void MarkStep(string id)
    {
        _snapshot.CompletedSteps.Add(id);
        EmitChanged();
    }

    private void EmitChanged() => EmitSignal(SignalName.DrinkChanged, GetDebugText());

    private void OnPhaseChanged(int phase)
    {
        _timing = (DayPhase)phase == DayPhase.Preparation;
    }
}
