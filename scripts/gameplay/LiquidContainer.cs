using System;
using System.Collections.Generic;
using GlassesBar.Domain;

namespace GlassesBar;

public sealed class LiquidContainer : ILiquidContainer
{
    private readonly Dictionary<string, double> _ingredients = new(StringComparer.Ordinal);

    public LiquidContainer(double capacity) => Capacity = Math.Max(0d, capacity);

    public double Capacity { get; }
    public double CurrentAmount { get; private set; }
    public double SpilledAmount { get; private set; }
    public IReadOnlyDictionary<string, double> Ingredients => _ingredients;

    public double Add(string ingredientId, double amount)
    {
        amount = Math.Max(0d, amount);
        var result = LiquidMath.Transfer(amount, CurrentAmount, Capacity, amount);
        CurrentAmount = result.DestinationAfter;
        SpilledAmount += result.Spilled;

        if (result.Transferred > 0d)
        {
            _ingredients.TryGetValue(ingredientId, out var existing);
            _ingredients[ingredientId] = existing + result.Transferred;
        }

        return result.Transferred;
    }

    public double Remove(double amount)
    {
        amount = Math.Clamp(amount, 0d, CurrentAmount);
        if (amount <= 0d)
            return 0d;

        var ratio = amount / CurrentAmount;
        foreach (var key in new List<string>(_ingredients.Keys))
        {
            _ingredients[key] = Math.Max(0d, _ingredients[key] * (1d - ratio));
            if (_ingredients[key] <= 0.000001d)
                _ingredients.Remove(key);
        }

        CurrentAmount -= amount;
        return amount;
    }

    public double Empty()
    {
        var amount = CurrentAmount;
        CurrentAmount = 0d;
        _ingredients.Clear();
        return amount;
    }
}

