using System;
using System.Collections.Generic;
using GlassesBar.Domain;

namespace GlassesBar;

public partial class DrinkWorkstation
{
    public WorkstationSnapshot CaptureState()
    {
        var state = new WorkstationSnapshot
        {
            LeftHandToolId = LeftHandToolId,
            RightHandToolId = RightHandToolId,
            BoardToolIds = new List<string>(_inventory.BoardToolIds),
            Glass = new LiquidSnapshot
            {
                Capacity = Glass.Capacity,
                SpilledAmount = Glass.SpilledAmount,
                Ingredients = new Dictionary<string, double>(Glass.Ingredients, StringComparer.Ordinal)
            },
            HandsWashedToday = HandsWashedToday,
            KettleWaterAmountMl = KettleWaterAmountMl,
            ElapsedSeconds = _snapshot.ElapsedSeconds,
            WastedAmount = _snapshot.WastedAmount,
            FailedOperations = _snapshot.FailedOperations,
            CompletedSteps = new HashSet<string>(_snapshot.CompletedSteps, StringComparer.Ordinal),
            RepeatRecoveryCounts = _processes.CaptureRepeatRecoveryCounts(),
            Tools = _inventory.CaptureToolSnapshots()
        };
        return state;
    }

    public void RestoreState(WorkstationSnapshot snapshot)
    {
        _inventory.RestoreState(
            snapshot.Tools,
            snapshot.LeftHandToolId,
            snapshot.RightHandToolId,
            snapshot.BoardToolIds);
        HandsWashedToday = snapshot.HandsWashedToday;
        KettleWaterAmountMl = Math.Clamp(snapshot.KettleWaterAmountMl, 0d, PrototypeKettleCapacityMl);
        Glass = new LiquidContainer(snapshot.Glass.Capacity);
        Glass.Restore(snapshot.Glass.Ingredients, snapshot.Glass.SpilledAmount);

        _snapshot.CompletedSteps.Clear();
        _snapshot.CompletedSteps.UnionWith(snapshot.CompletedSteps);
        _snapshot.IngredientAmounts.Clear();
        _snapshot.ElapsedSeconds = Math.Max(0d, snapshot.ElapsedSeconds);
        _snapshot.WastedAmount = Math.Max(0d, snapshot.WastedAmount);
        _snapshot.SpilledAmount = Glass.SpilledAmount;
        _snapshot.CraftCompletionRatio = 1d;
        _snapshot.FailedOperations = Math.Max(0, snapshot.FailedOperations);
        _processes.RestoreRepeatRecoveryCounts(snapshot.RepeatRecoveryCounts);

        foreach (var state in _inventory.Tools.Values)
        {
            _toolPresentations[state.Id].Node.ApplyWorldState(
                ToVector3(state.Position),
                state.Location is ToolLocation.Counter or ToolLocation.Workboard);
        }

        _timing = GameSession.Instance.Flow.Current == DayPhase.Preparation;
        _nextAttemptRoll = null;
        LastOperationFeedback = string.Empty;
        LastProcessResult = null;
        EmitHandsAndState(string.Empty, false);
    }
}
