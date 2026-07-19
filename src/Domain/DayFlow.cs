using System;

namespace GlassesBar.Domain;

public enum DayPhase
{
    WaitingForOrder,
    OrderReceived,
    RecipeObservation,
    Preparation,
    Delivery,
    Evaluation,
    DaySummary
}

public sealed class DayFlow
{
    public DayPhase Current { get; private set; } = DayPhase.WaitingForOrder;

    public bool TryAdvance(DayPhase next)
    {
        if (!IsAllowed(Current, next))
            return false;

        Current = next;
        return true;
    }

    public void Reset() => Current = DayPhase.WaitingForOrder;

    private static bool IsAllowed(DayPhase current, DayPhase next) => (current, next) switch
    {
        (DayPhase.WaitingForOrder, DayPhase.OrderReceived) => true,
        (DayPhase.OrderReceived, DayPhase.RecipeObservation) => true,
        (DayPhase.RecipeObservation, DayPhase.Preparation) => true,
        (DayPhase.Preparation, DayPhase.Delivery) => true,
        (DayPhase.Delivery, DayPhase.Evaluation) => true,
        (DayPhase.Evaluation, DayPhase.DaySummary) => true,
        _ => false
    };
}

