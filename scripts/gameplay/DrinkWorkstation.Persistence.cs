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
            Glass = _assembly.CaptureGlassSnapshot(),
            HandsWashedToday = HandsWashedToday,
            KettleWaterAmountMl = KettleWaterAmountMl,
            ElapsedSeconds = _assembly.ElapsedSeconds,
            WastedAmount = _assembly.WastedAmount,
            FailedOperations = _assembly.FailedOperations,
            CompletedSteps = _assembly.CaptureCompletedSteps(),
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
        _assembly.Restore(
            snapshot.Glass,
            snapshot.ElapsedSeconds,
            snapshot.WastedAmount,
            snapshot.FailedOperations,
            snapshot.CompletedSteps);
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
