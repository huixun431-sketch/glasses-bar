using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GlassesBar;

public sealed class BarEnvironmentVisualSet
{
    public BarEnvironmentVisualSet(
        IReadOnlyDictionary<string, Node3D> neutralModules,
        IReadOnlyDictionary<string, Node3D> realityModules,
        IReadOnlyDictionary<string, Node3D> glassesModules)
    {
        NeutralModules = neutralModules;
        RealityModules = realityModules;
        GlassesModules = glassesModules;
    }

    public IReadOnlyDictionary<string, Node3D> NeutralModules { get; }
    public IReadOnlyDictionary<string, Node3D> RealityModules { get; }
    public IReadOnlyDictionary<string, Node3D> GlassesModules { get; }

    public IEnumerable<Node3D> AllRoots =>
        NeutralModules.Values.Concat(RealityModules.Values).Concat(GlassesModules.Values);
}

/// <summary>
/// Loads the complete production environment as one transaction. Imported scenes own
/// replaceable meshes only; gameplay state, collision, and real lights stay project-owned.
/// </summary>
public sealed class BarEnvironmentVisualLoader
{
    private enum ModuleOwner
    {
        Neutral,
        PerWorld
    }

    private sealed record ModuleDefinition(string AssetId, string ScenePath, ModuleOwner Owner);

    private static readonly ModuleDefinition[] Modules =
    {
        new("bar_architecture", "res://scenes/environment/modules/bar_architecture.tscn", ModuleOwner.PerWorld),
        new("bar_counter", "res://scenes/environment/modules/bar_counter.tscn", ModuleOwner.Neutral),
        new("bar_backbar", "res://scenes/environment/modules/bar_backbar.tscn", ModuleOwner.Neutral),
        new("bar_furniture", "res://scenes/environment/modules/bar_furniture.tscn", ModuleOwner.PerWorld),
        new("bar_lighting", "res://scenes/environment/modules/bar_lighting.tscn", ModuleOwner.PerWorld),
        new("bar_wear_overlays", "res://scenes/environment/modules/bar_wear_overlays.tscn", ModuleOwner.Neutral)
    };

    private readonly string? _forcedMissingAssetId;

    public BarEnvironmentVisualLoader(string? forcedMissingAssetId = null)
    {
        _forcedMissingAssetId = forcedMissingAssetId;
    }

    public bool TryInstantiate(
        Node3D neutralGameplay,
        Node3D realityWorld,
        Node3D glassesWorld,
        out BarEnvironmentVisualSet visualSet)
    {
        ArgumentNullException.ThrowIfNull(neutralGameplay);
        ArgumentNullException.ThrowIfNull(realityWorld);
        ArgumentNullException.ThrowIfNull(glassesWorld);

        var neutral = new Dictionary<string, Node3D>(StringComparer.Ordinal);
        var reality = new Dictionary<string, Node3D>(StringComparer.Ordinal);
        var glasses = new Dictionary<string, Node3D>(StringComparer.Ordinal);
        var pending = new List<(Node3D Parent, string AssetId, Node3D Instance)>();

        try
        {
            foreach (var module in Modules)
            {
                if (string.Equals(module.AssetId, _forcedMissingAssetId, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Forced missing module '{module.AssetId}'.");

                var scene = ResourceLoader.Load<PackedScene>(module.ScenePath) ??
                    throw new InvalidOperationException($"Missing production wrapper '{module.ScenePath}'.");
                if (module.Owner == ModuleOwner.Neutral)
                {
                    var instance = InstantiateValidated(scene, module.AssetId);
                    neutral.Add(module.AssetId, instance);
                    pending.Add((neutralGameplay, module.AssetId, instance));
                    continue;
                }

                var realityInstance = InstantiateValidated(scene, module.AssetId);
                var glassesInstance = InstantiateValidated(scene, module.AssetId);
                reality.Add(module.AssetId, realityInstance);
                glasses.Add(module.AssetId, glassesInstance);
                pending.Add((realityWorld, module.AssetId, realityInstance));
                pending.Add((glassesWorld, module.AssetId, glassesInstance));
            }

            foreach (var item in pending)
            {
                item.Instance.Name = ProductionNodeName(item.AssetId);
                item.Parent.AddChild(item.Instance);
            }

            visualSet = new BarEnvironmentVisualSet(neutral, reality, glasses);
            return true;
        }
        catch (Exception exception)
        {
            foreach (var item in pending)
            {
                if (item.Instance.GetParent() is Node parent)
                    parent.RemoveChild(item.Instance);
                item.Instance.Free();
            }
            if (_forcedMissingAssetId is null)
                GD.PushWarning($"Production bar visuals unavailable; using complete graybox fallback. {exception.Message}");
            visualSet = null!;
            return false;
        }
    }

    private static Node3D InstantiateValidated(PackedScene scene, string assetId)
    {
        var instance = scene.Instantiate<Node3D>();
        try
        {
            if (instance.GetMeta("asset_id").AsString() != assetId)
                throw new InvalidOperationException($"Wrapper asset ID mismatch for '{assetId}'.");
            if (instance.FindChild(assetId, true, false) is not Node3D)
                throw new InvalidOperationException($"Wrapper '{assetId}' has no imported visual root.");
            if (instance.FindChildren("*", "CollisionObject3D", true, false).Count != 0)
                throw new InvalidOperationException($"Imported visual '{assetId}' contains collision.");
            if (instance.FindChildren("*", "Light3D", true, false).Count != 0)
                throw new InvalidOperationException($"Imported visual '{assetId}' contains real lights.");
            return instance;
        }
        catch
        {
            instance.Free();
            throw;
        }
    }

    private static string ProductionNodeName(string assetId) => assetId switch
    {
        "bar_architecture" => "ProductionArchitecture",
        "bar_counter" => "ProductionCounter",
        "bar_backbar" => "ProductionBackbar",
        "bar_furniture" => "ProductionFurniture",
        "bar_lighting" => "ProductionLighting",
        "bar_wear_overlays" => "ProductionWearOverlays",
        _ => throw new ArgumentOutOfRangeException(nameof(assetId), assetId, null)
    };
}
