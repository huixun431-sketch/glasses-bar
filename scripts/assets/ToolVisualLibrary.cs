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
            ["pestle"] = "res://scenes/assets/stage1/pestle.tscn"
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

    public static void ApplyWorldStyle(Node3D visual, WorldMode mode)
    {
        var material = mode == WorldMode.Glasses ? GlassesMaterial : null;
        foreach (var child in visual.FindChildren("*", "MeshInstance3D", true, false))
            ((MeshInstance3D)child).MaterialOverride = material;
    }
}
