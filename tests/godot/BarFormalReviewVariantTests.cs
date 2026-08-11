using System;
using System.Linq;
using Godot;

namespace GlassesBar.Tests;

public partial class BarFormalReviewVariantTests : Node
{
    public override void _Ready() => CallDeferred(MethodName.RunDeferred);

    private void RunDeferred()
    {
        try
        {
            var packed = ResourceLoader.Load<PackedScene>(
                "res://scenes/environment/BarInteriorProductionPreview.tscn");
            Require(packed is not null, "Formal preview scene is missing.");
            var preview = packed!.Instantiate<Node3D>();
            AddChild(preview);
            var presenter = new BarFormalReviewPresentation(preview);
            var wear = preview.GetNode<Node3D>("WearOverlays");
            var meshes = preview.FindChildren("*", "MeshInstance3D", true, false)
                .OfType<MeshInstance3D>()
                .Where(mesh => !wear.IsAncestorOf(mesh))
                .ToArray();
            Require(meshes.Length > 0, "Formal preview has no reviewable meshes.");
            var instanceIds = meshes.Select(mesh => mesh.GetInstanceId()).ToArray();

            presenter.Apply(BarFormalReviewMode.RealityWarm);
            Require(!wear.Visible, "Reality preview must hide glasses-only wear overlays.");
            Require(meshes.All(HasNoSurfaceOverrides),
                "Reality preview must restore the imported material surfaces.");

            presenter.Apply(BarFormalReviewMode.NeutralClay);
            Require(!wear.Visible, "Neutral review must hide wear overlays.");
            Require(meshes.All(mesh => EveryOverrideHasPrefix(mesh, "ReviewNeutralClay")),
                "Neutral review must apply one deterministic clay override to every surface.");

            presenter.Apply(BarFormalReviewMode.GlassesCold);
            Require(wear.Visible, "Glasses preview must reveal the approved sparse wear overlays.");
            Require(meshes.All(mesh => EveryOverrideHasPrefix(mesh, "ReviewGlasses_")),
                "Glasses preview must apply deterministic cold/desaturated surface variants.");
            Require(instanceIds.SequenceEqual(meshes.Select(mesh => mesh.GetInstanceId())),
                "Review variants must not replace scene nodes or duplicate state.");

            presenter.Apply(BarFormalReviewMode.RealityWarm);
            Require(meshes.All(HasNoSurfaceOverrides),
                "Returning to reality must remove every review-only surface override.");
            GD.Print("BAR_FORMAL_REVIEW_VARIANT_PASS");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private static bool HasNoSurfaceOverrides(MeshInstance3D mesh)
    {
        for (var surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
        {
            if (mesh.GetSurfaceOverrideMaterial(surface) is not null)
                return false;
        }
        return true;
    }

    private static bool EveryOverrideHasPrefix(MeshInstance3D mesh, string prefix)
    {
        for (var surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
        {
            var material = mesh.GetSurfaceOverrideMaterial(surface);
            if (material is null || !material.ResourceName.StartsWith(prefix, StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
