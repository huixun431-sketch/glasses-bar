using System;
using System.Collections.Generic;
using Godot;

namespace GlassesBar;

/// <summary>
/// Moves imported moving meshes under their authoritative gameplay nodes. This class
/// never creates cabinet state or collision and performs a complete preflight first.
/// </summary>
public sealed class BarGameplayVisualBinder
{
    private sealed record Binding(CabinetInteractable Cabinet, Node3D[] Visuals);

    public void Bind(
        BarLayoutDefinition layout,
        Node3D neutralGameplay,
        BarEnvironmentVisualSet visualSet)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(neutralGameplay);
        ArgumentNullException.ThrowIfNull(visualSet);

        var counter = visualSet.NeutralModules["bar_counter"];
        var backbar = visualSet.NeutralModules["bar_backbar"];
        var bindings = new List<Binding>();

        foreach (var storage in layout.Storages)
        {
            var id = storage.Front.Id;
            var cabinet = neutralGameplay.GetNodeOrNull<CabinetInteractable>(id) ??
                throw new InvalidOperationException($"Missing authoritative cabinet '{id}'.");
            if (storage.Front.Kind == CabinetPartKind.SlidingDoor)
            {
                bindings.Add(new Binding(cabinet, new[]
                {
                    RequireVisual(backbar, id + "_fixed"),
                    RequireVisual(backbar, id + "_moving")
                }));
                continue;
            }

            var source = id.StartsWith("front_drawer_", StringComparison.Ordinal)
                ? counter
                : backbar;
            bindings.Add(new Binding(cabinet, new[] { RequireVisual(source, id) }));
        }

        foreach (var binding in bindings)
        {
            Node3D productionVisual;
            if (binding.Visuals.Length == 1)
            {
                productionVisual = binding.Visuals[0];
                productionVisual.Reparent(binding.Cabinet, true);
            }
            else
            {
                productionVisual = new Node3D { Name = "ProductionVisual" };
                neutralGameplay.AddChild(productionVisual);
                binding.Visuals[0].Reparent(productionVisual, true);
                binding.Visuals[0].Name = "FixedProductionVisual";
                binding.Visuals[1].Reparent(productionVisual, true);
                binding.Visuals[1].Name = "MovingProductionVisual";
                productionVisual.Reparent(binding.Cabinet, true);
            }
            binding.Cabinet.SetProductionVisual(productionVisual);
        }
    }

    private static Node3D RequireVisual(Node root, string name) =>
        root.FindChild(name, true, false) as Node3D ??
        throw new InvalidOperationException($"Production visual node '{name}' is missing.");
}
