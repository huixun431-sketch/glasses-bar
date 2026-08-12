using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GlassesBar;

/// <summary>
/// Owns real Godot lights for production visuals. Imported fixture GLBs remain mesh-only.
/// </summary>
public sealed class BarLightRigController
{
    private readonly BarLayoutDefinition _layout;
    private readonly Node3D _reality;
    private readonly Node3D _glasses;

    public BarLightRigController(
        BarLayoutDefinition layout,
        Node3D reality,
        Node3D glasses)
    {
        _layout = layout;
        _reality = reality;
        _glasses = glasses;
    }

    public void Build()
    {
        BuildWorldRig(_reality, false);
        BuildWorldRig(_glasses, true);
    }

    private void BuildWorldRig(Node3D world, bool glasses)
    {
        if (world.HasNode("BarLightRig"))
            throw new InvalidOperationException($"{world.Name} already owns a bar light rig.");

        var rig = new Node3D { Name = "BarLightRig" };
        world.AddChild(rig);
        foreach (var fixture in AllFixtures())
        {
            var root = new Node3D { Name = fixture.Id, Position = fixture.Position };
            rig.AddChild(root);
            root.AddChild(CreateLight(fixture, glasses));
        }
    }

    private IEnumerable<BarLightFixtureLayout> AllFixtures() =>
        _layout.PendantFixtures
            .Concat(_layout.LoungePendantFixtures)
            .Concat(_layout.RearLinearFixtures)
            .Concat(_layout.CustomerSconces)
            .Concat(_layout.CustomerFillLights);

    private static Light3D CreateLight(BarLightFixtureLayout fixture, bool glasses)
    {
        var color = glasses ? new Color("9fc8d2") : new Color("ffd8b5");
        if (fixture.Group == "customer_fill")
        {
            var northKey = fixture.Id.EndsWith("north", StringComparison.Ordinal);
            return new DirectionalLight3D
            {
                Name = "Light",
                RotationDegrees = northKey
                    ? new Vector3(-48f, -34f, 0f)
                    : new Vector3(32f, 146f, 0f),
                LightColor = color,
                LightEnergy = northKey ? 0.38f : 0.22f,
                ShadowEnabled = false
            };
        }

        if (fixture.Group is "front_pendant" or "lounge_pendant")
        {
            return new SpotLight3D
            {
                Name = "Light",
                RotationDegrees = new Vector3(-90f, 0f, 0f),
                LightColor = color,
                LightEnergy = fixture.Group == "lounge_pendant" ? 2.0f : 4.0f,
                SpotRange = fixture.Group == "lounge_pendant" ? 2.8f : 3.4f,
                SpotAngle = fixture.Group == "lounge_pendant" ? 40f : 44f,
                SpotAngleAttenuation = 0.82f,
                ShadowEnabled = false
            };
        }

        var energy = fixture.Group switch
        {
            "rear_linear" => 3.0f,
            _ => 1.1f
        };
        return new OmniLight3D
        {
            Name = "Light",
            LightColor = color,
            LightEnergy = energy,
            OmniRange = fixture.Group switch
            {
                "rear_linear" => 3.5f,
                _ => 2.8f
            },
            ShadowEnabled = false
        };
    }
}
