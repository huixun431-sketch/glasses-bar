using System;
using System.Collections.Generic;
using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

public enum ToolLocation
{
    Counter,
    LeftHand,
    RightHand,
    Workboard
}

public sealed class ToolRuntimeState
{
    public required ToolSpec Spec { get; init; }
    public required ToolInteractable Node { get; init; }
    public required Vector3 InitialPosition { get; init; }
    public ToolLocation Location { get; set; } = ToolLocation.Counter;
    public int BoardSlot { get; set; } = -1;
    public bool ContentsAreWaste { get; set; }
    public double ContentCompletionRatio { get; set; } = 1d;
    public Dictionary<string, double> Contents { get; } = new(StringComparer.Ordinal);

    public double ContentAmount
    {
        get
        {
            var result = 0d;
            foreach (var amount in Contents.Values)
                result += Math.Max(0d, amount);
            return result;
        }
    }

    public void ClearContents()
    {
        Contents.Clear();
        ContentsAreWaste = false;
        ContentCompletionRatio = 1d;
    }
}
