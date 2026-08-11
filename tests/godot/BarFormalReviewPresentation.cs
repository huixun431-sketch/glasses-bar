using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GlassesBar.Tests;

public enum BarFormalReviewMode
{
    RealityWarm,
    NeutralClay,
    GlassesCold
}

public sealed class BarFormalReviewPresentation
{
    private readonly Node3D _wearOverlays;
    private readonly MeshInstance3D[] _meshes;
    private readonly StandardMaterial3D _neutralClay = new()
    {
        ResourceName = "ReviewNeutralClay",
        AlbedoColor = new Color(0.56f, 0.58f, 0.60f),
        Metallic = 0f,
        Roughness = 0.86f
    };
    private readonly Dictionary<Material, StandardMaterial3D> _glassesMaterials = new();

    public BarFormalReviewPresentation(Node3D preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        _wearOverlays = preview.GetNode<Node3D>("WearOverlays");
        _meshes = preview.FindChildren("*", "MeshInstance3D", true, false)
            .OfType<MeshInstance3D>()
            .Where(mesh => !_wearOverlays.IsAncestorOf(mesh))
            .ToArray();
    }

    public void Apply(BarFormalReviewMode mode)
    {
        _wearOverlays.Visible = mode == BarFormalReviewMode.GlassesCold;
        foreach (var mesh in _meshes)
        {
            if (mesh.Mesh is null)
                continue;

            for (var surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
            {
                Material? reviewMaterial = mode switch
                {
                    BarFormalReviewMode.RealityWarm => null,
                    BarFormalReviewMode.NeutralClay => _neutralClay,
                    BarFormalReviewMode.GlassesCold => GetGlassesMaterial(
                        mesh.Mesh.SurfaceGetMaterial(surface), mesh.Name, surface),
                    _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
                };
                mesh.SetSurfaceOverrideMaterial(surface, reviewMaterial);
            }
        }
    }

    private StandardMaterial3D GetGlassesMaterial(Material? source, StringName meshName, int surface)
    {
        if (source is not null && _glassesMaterials.TryGetValue(source, out var cached))
            return cached;

        var baseMaterial = source as StandardMaterial3D;
        var original = baseMaterial?.AlbedoColor ?? new Color(0.55f, 0.58f, 0.62f);
        var luminance = original.R * 0.2126f + original.G * 0.7152f + original.B * 0.0722f;
        var coldColor = new Color(
            Mathf.Lerp(luminance * 0.56f, original.R, 0.18f),
            Mathf.Lerp(luminance * 0.68f, original.G, 0.18f),
            Mathf.Lerp(luminance * 0.92f, original.B, 0.18f),
            original.A);
        var sourceName = string.IsNullOrWhiteSpace(source?.ResourceName)
            ? $"{meshName}_{surface}"
            : source!.ResourceName;
        var material = new StandardMaterial3D
        {
            ResourceName = $"ReviewGlasses_{sourceName}",
            AlbedoColor = coldColor,
            Metallic = baseMaterial?.Metallic ?? 0f,
            Roughness = Mathf.Clamp((baseMaterial?.Roughness ?? 0.65f) + 0.12f, 0f, 1f),
            Transparency = baseMaterial?.Transparency ?? BaseMaterial3D.TransparencyEnum.Disabled,
            CullMode = baseMaterial?.CullMode ?? BaseMaterial3D.CullModeEnum.Back
        };
        if (source is not null)
            _glassesMaterials[source] = material;
        return material;
    }
}
