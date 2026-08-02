using System;
using System.Collections.Generic;
using Godot;

namespace GlassesBar;

/// <summary>
/// Resolves approved visual wrappers without giving imported geometry ownership of
/// gameplay state, collision, or interaction behavior.
/// </summary>
internal static class ToolVisualLibrary
{
    private static readonly IReadOnlyDictionary<string, string> WrapperPaths =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["highball_glass"] = "res://scenes/assets/stage1/highball_glass.tscn",
            ["jigger_medium"] = "res://scenes/assets/stage1/jigger_medium.tscn",
            ["mortar"] = "res://scenes/assets/stage1/mortar.tscn",
            ["pestle"] = "res://scenes/assets/stage1/pestle.tscn",
            ["traditional_filter"] = "res://scenes/assets/stage2/traditional_filter.tscn",
            ["bean_scoop"] = "res://scenes/assets/stage2/bean_scoop.tscn",
            ["ice_tongs"] = "res://scenes/assets/stage2/ice_tongs.tscn",
            ["jigger_small"] = "res://scenes/assets/stage2/jigger_small.tscn",
            ["jigger_large"] = "res://scenes/assets/stage2/jigger_large.tscn"
        };

    private static readonly StandardMaterial3D GlassesMaterial = new()
    {
        AlbedoColor = new Color("5f8f8d"),
        Metallic = 0.18f,
        Roughness = 0.46f,
        EmissionEnabled = true,
        Emission = new Color("173b3b")
    };

    public static Node3D? Instantiate(string toolId)
    {
        if (!WrapperPaths.TryGetValue(toolId, out var path))
            return null;
        var scene = ResourceLoader.Load<PackedScene>(path);
        return scene?.Instantiate<Node3D>();
    }

    public static Node3D? FindAnchor(Node3D visual, string anchorName) =>
        visual.FindChild(anchorName, true, false) as Node3D;

    public static void ApplyHeldPose(Node3D visual, string toolId)
    {
        var scale = toolId switch
        {
            "mortar" => 0.68f,
            "pestle" => 0.72f,
            "highball_glass" => 0.84f,
            "jigger_medium" => 0.90f,
            "traditional_filter" => 0.72f,
            "bean_scoop" => 0.82f,
            "ice_tongs" => 0.78f,
            "jigger_small" => 0.94f,
            "jigger_large" => 0.82f,
            _ => 1f
        };
        visual.Scale = Vector3.One * scale;
        visual.RotationDegrees = toolId switch
        {
            "pestle" => new Vector3(0f, 0f, 12f),
            "jigger_medium" => new Vector3(0f, 0f, 7f),
            "traditional_filter" => new Vector3(0f, 0f, -8f),
            "bean_scoop" => new Vector3(0f, 0f, 8f),
            "ice_tongs" => new Vector3(0f, 0f, -6f),
            "jigger_small" or "jigger_large" => new Vector3(0f, 0f, 7f),
            _ => Vector3.Zero
        };
    }

    public static void ApplyWorldStyle(Node3D visual, WorldMode mode)
    {
        var material = mode == WorldMode.Glasses ? GlassesMaterial : null;
        foreach (var child in visual.FindChildren("*", "MeshInstance3D", true, false))
            ((MeshInstance3D)child).MaterialOverride = material;
    }
}
