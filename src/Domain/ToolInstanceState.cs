using System;
using System.Collections.Generic;

namespace GlassesBar.Domain;

public enum ToolLocation
{
    Counter,
    LeftHand,
    RightHand,
    Workboard
}

public readonly record struct SpatialPosition(double X, double Y, double Z);

/// <summary>
/// Mutable state for one tool instance. This class deliberately has no Godot node,
/// transform, mesh, or material reference so gameplay state can be tested and saved
/// independently from its presentation.
/// </summary>
public sealed class ToolInstanceState
{
    public required ToolSpec Definition { get; init; }
    public required SpatialPosition InitialPosition { get; init; }
    public string Id => Definition.Id;
    public SpatialPosition Position { get; set; }
    public ToolLocation Location { get; set; } = ToolLocation.Counter;
    public int BoardSlot { get; set; } = -1;
    public bool ContentsAreWaste { get; set; }
    public double ContentCompletionRatio { get; set; } = 1d;
    public bool UseLargeMeasureSide { get; set; } = true;
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
