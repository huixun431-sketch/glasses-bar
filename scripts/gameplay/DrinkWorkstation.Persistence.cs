using System;
using System.Collections.Generic;
using System.Linq;
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
            BoardToolIds = new List<string>(_boardToolIds),
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
            RepeatRecoveryCounts = new Dictionary<string, int>(_repeatRecoveryCounts, StringComparer.Ordinal)
        };
        foreach (var tool in _tools.Values)
        {
            state.Tools.Add(new ToolInstanceSnapshot
            {
                ToolId = tool.Id,
                Location = tool.Location,
                BoardSlot = tool.BoardSlot,
                Position = tool.Position,
                ContentsAreWaste = tool.ContentsAreWaste,
                ContentCompletionRatio = tool.ContentCompletionRatio,
                UseLargeMeasureSide = tool.UseLargeMeasureSide,
                Contents = new Dictionary<string, double>(tool.Contents, StringComparer.Ordinal)
            });
        }
        return state;
    }

    public void RestoreState(WorkstationSnapshot snapshot)
    {
        var incoming = snapshot.Tools.ToDictionary(tool => tool.ToolId, StringComparer.Ordinal);
        if (incoming.Count != _tools.Count || _tools.Keys.Any(id => !incoming.ContainsKey(id)))
            throw new InvalidOperationException("Save tool instances do not match the configured gameplay catalog.");

        LeftHandToolId = snapshot.LeftHandToolId;
        RightHandToolId = snapshot.RightHandToolId;
        _boardToolIds.Clear();
        _boardToolIds.AddRange(snapshot.BoardToolIds);
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
        _repeatRecoveryCounts.Clear();
        foreach (var recovery in snapshot.RepeatRecoveryCounts)
            _repeatRecoveryCounts[recovery.Key] = Math.Max(0, recovery.Value);

        foreach (var state in _tools.Values)
        {
            var saved = incoming[state.Id];
            state.Location = saved.Location;
            state.BoardSlot = saved.BoardSlot;
            state.Position = saved.Position;
            state.ContentsAreWaste = saved.ContentsAreWaste;
            state.ContentCompletionRatio = Math.Clamp(saved.ContentCompletionRatio, 0d, 1d);
            state.UseLargeMeasureSide = saved.UseLargeMeasureSide;
            state.Contents.Clear();
            foreach (var content in saved.Contents.Where(content => content.Value > 0d))
                state.Contents[content.Key] = content.Value;
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
