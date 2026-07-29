using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GlassesBar.Domain;

public sealed class ToolInstanceSnapshot
{
    public string ToolId { get; set; } = string.Empty;
    public ToolLocation Location { get; set; }
    public int BoardSlot { get; set; } = -1;
    public SpatialPosition Position { get; set; }
    public bool ContentsAreWaste { get; set; }
    public double ContentCompletionRatio { get; set; } = 1d;
    public bool UseLargeMeasureSide { get; set; } = true;
    public Dictionary<string, double> Contents { get; set; } = new(StringComparer.Ordinal);
}

public sealed class LiquidSnapshot
{
    public double Capacity { get; set; }
    public double SpilledAmount { get; set; }
    public Dictionary<string, double> Ingredients { get; set; } = new(StringComparer.Ordinal);
}

public sealed class WorkstationSnapshot
{
    public string LeftHandToolId { get; set; } = string.Empty;
    public string RightHandToolId { get; set; } = string.Empty;
    public List<string> BoardToolIds { get; set; } = new();
    public List<ToolInstanceSnapshot> Tools { get; set; } = new();
    public LiquidSnapshot Glass { get; set; } = new();
    public bool HandsWashedToday { get; set; }
    public double KettleWaterAmountMl { get; set; }
    public double ElapsedSeconds { get; set; }
    public double WastedAmount { get; set; }
    public int FailedOperations { get; set; }
    public HashSet<string> CompletedSteps { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> RepeatRecoveryCounts { get; set; } = new(StringComparer.Ordinal);
}

public sealed class PlayerSnapshot
{
    public SpatialPosition Position { get; set; }
    public SpatialPosition BodyRotation { get; set; }
    public SpatialPosition HeadRotation { get; set; }
}

/// <summary>
/// Versioned persistence boundary. It intentionally contains authoritative gameplay
/// state only; scene nodes, materials, tweens, prompts, and other presentation state
/// are reconstructed after restore.
/// </summary>
public sealed class GameSaveSnapshot
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public int CurrentDay { get; set; } = 1;
    public string RecipeId { get; set; } = "prototype_iced_americano";
    public DayPhase DayPhase { get; set; } = DayPhase.WaitingForOrder;
    public string WorldModeId { get; set; } = "reality";
    public bool GameStarted { get; set; }
    public bool RecipeObserved { get; set; }
    public WorkstationSnapshot Workstation { get; set; } = new();
    public PlayerSnapshot? Player { get; set; }
    public Dictionary<string, bool> StorageOpenStates { get; set; } = new(StringComparer.Ordinal);
}

public static class SaveGameSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static string Serialize(GameSaveSnapshot snapshot)
    {
        Validate(snapshot);
        return JsonSerializer.Serialize(snapshot, Options);
    }

    public static GameSaveSnapshot Deserialize(string json)
    {
        var snapshot = JsonSerializer.Deserialize<GameSaveSnapshot>(json, Options)
            ?? throw new InvalidDataException("Save payload is empty.");
        Validate(snapshot);
        return snapshot;
    }

    public static void Validate(GameSaveSnapshot snapshot)
    {
        if (snapshot.Workstation is null ||
            snapshot.Workstation.Glass is null ||
            snapshot.Workstation.Tools is null ||
            snapshot.Workstation.BoardToolIds is null ||
            snapshot.Workstation.CompletedSteps is null ||
            snapshot.Workstation.RepeatRecoveryCounts is null ||
            snapshot.Workstation.Glass.Ingredients is null ||
            snapshot.StorageOpenStates is null ||
            snapshot.Workstation.Tools.Any(tool => tool is null || tool.Contents is null))
            throw new InvalidDataException("Save payload is missing required state collections.");
        if (snapshot.SchemaVersion != GameSaveSnapshot.CurrentSchemaVersion)
            throw new InvalidDataException(
                $"Unsupported save schema {snapshot.SchemaVersion}; expected {GameSaveSnapshot.CurrentSchemaVersion}.");
        if (snapshot.CurrentDay is < 1 or > MyopiaProgression.CampaignDays)
            throw new InvalidDataException("Save current day is outside the current campaign.");
        if (string.IsNullOrWhiteSpace(snapshot.RecipeId))
            throw new InvalidDataException("Save recipe ID is required.");
        if (!Enum.IsDefined(snapshot.DayPhase))
            throw new InvalidDataException("Save contains an unknown day phase.");
        if (snapshot.WorldModeId is not ("reality" or "glasses"))
            throw new InvalidDataException($"Unknown world mode ID: {snapshot.WorldModeId}");
        if (snapshot.Workstation.KettleWaterAmountMl < 0d ||
            snapshot.Workstation.ElapsedSeconds < 0d ||
            snapshot.Workstation.WastedAmount < 0d ||
            snapshot.Workstation.FailedOperations < 0 ||
            snapshot.Workstation.RepeatRecoveryCounts.Any(recovery =>
                string.IsNullOrWhiteSpace(recovery.Key) || recovery.Value < 0) ||
            snapshot.Workstation.CompletedSteps.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("Save contains negative workstation totals.");
        if (snapshot.Workstation.Glass.Capacity <= 0d ||
            snapshot.Workstation.Glass.SpilledAmount < 0d ||
            snapshot.Workstation.Glass.Ingredients.Any(ingredient =>
                string.IsNullOrWhiteSpace(ingredient.Key) || ingredient.Value < 0d) ||
            snapshot.Workstation.Glass.Ingredients.Values.Sum() >
            snapshot.Workstation.Glass.Capacity + 0.000001d)
            throw new InvalidDataException("Save glass state is invalid.");

        var toolIds = snapshot.Workstation.Tools.Select(tool => tool.ToolId).ToArray();
        if (toolIds.Any(string.IsNullOrWhiteSpace) ||
            toolIds.Distinct(StringComparer.Ordinal).Count() != toolIds.Length)
            throw new InvalidDataException("Save tool instance IDs must be non-empty and unique.");
        var knownTools = toolIds.ToHashSet(StringComparer.Ordinal);
        if (!string.IsNullOrEmpty(snapshot.Workstation.LeftHandToolId) &&
            !knownTools.Contains(snapshot.Workstation.LeftHandToolId))
            throw new InvalidDataException("Save left-hand tool does not exist.");
        if (!string.IsNullOrEmpty(snapshot.Workstation.RightHandToolId) &&
            !knownTools.Contains(snapshot.Workstation.RightHandToolId))
            throw new InvalidDataException("Save right-hand tool does not exist.");
        if (snapshot.Workstation.BoardToolIds.Distinct(StringComparer.Ordinal).Count() !=
            snapshot.Workstation.BoardToolIds.Count ||
            snapshot.Workstation.BoardToolIds.Any(id => !knownTools.Contains(id)))
            throw new InvalidDataException("Save workboard tool list is invalid.");

        foreach (var tool in snapshot.Workstation.Tools)
        {
            if (!Enum.IsDefined(tool.Location) ||
                tool.ContentCompletionRatio is < 0d or > 1d ||
                tool.Contents.Any(content => string.IsNullOrWhiteSpace(content.Key) || content.Value < 0d))
                throw new InvalidDataException($"Save tool state is invalid: {tool.ToolId}");
        }
        var byId = snapshot.Workstation.Tools.ToDictionary(tool => tool.ToolId, StringComparer.Ordinal);
        if (!string.IsNullOrEmpty(snapshot.Workstation.LeftHandToolId) &&
            byId[snapshot.Workstation.LeftHandToolId].Location != ToolLocation.LeftHand)
            throw new InvalidDataException("Save left-hand slot disagrees with its tool instance.");
        if (!string.IsNullOrEmpty(snapshot.Workstation.RightHandToolId) &&
            byId[snapshot.Workstation.RightHandToolId].Location != ToolLocation.RightHand)
            throw new InvalidDataException("Save right-hand slot disagrees with its tool instance.");
        if (snapshot.Workstation.Tools.Count(tool => tool.Location == ToolLocation.LeftHand) !=
                (string.IsNullOrEmpty(snapshot.Workstation.LeftHandToolId) ? 0 : 1) ||
            snapshot.Workstation.Tools.Count(tool => tool.Location == ToolLocation.RightHand) !=
                (string.IsNullOrEmpty(snapshot.Workstation.RightHandToolId) ? 0 : 1))
            throw new InvalidDataException("Save hand locations disagree with hand slots.");
        var boardStates = snapshot.Workstation.Tools.Where(tool => tool.Location == ToolLocation.Workboard).ToArray();
        if (boardStates.Select(tool => tool.ToolId).ToHashSet(StringComparer.Ordinal)
                .SetEquals(snapshot.Workstation.BoardToolIds) == false ||
            boardStates.Any(tool => tool.BoardSlot < 0) ||
            boardStates.Select(tool => tool.BoardSlot).Distinct().Count() != boardStates.Length)
            throw new InvalidDataException("Save workboard slots disagree with tool instance locations.");
        if (snapshot.StorageOpenStates.Keys.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("Save storage IDs must be non-empty.");
        if (snapshot.StorageOpenStates.Values.Count(open => open) > 1)
            throw new InvalidDataException("Save violates the single-open-storage invariant.");
    }
}
