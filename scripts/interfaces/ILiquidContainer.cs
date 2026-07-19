using System.Collections.Generic;

namespace GlassesBar;

public interface ILiquidContainer
{
    double Capacity { get; }
    double CurrentAmount { get; }
    double SpilledAmount { get; }
    IReadOnlyDictionary<string, double> Ingredients { get; }
    double Add(string ingredientId, double amount);
    double Remove(double amount);
    double Empty();
}

