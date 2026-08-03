using System;
using System.Collections.Generic;
using Godot;

namespace GlassesBar.Tests;

public partial class BarInteriorFormalVisualCapture : Node
{
    private readonly record struct View(
        string Name,
        Vector3 Position,
        Vector3 Target,
        float Fov = 70f,
        bool HideCeiling = false);

    public override void _Ready() => CallDeferred(MethodName.Capture);

    private async void Capture()
    {
        try
        {
            var preview = GetNode<Node3D>("Preview");
            var camera = new Camera3D { Current = true, Fov = 70f };
            AddChild(camera);
            AddChild(new DirectionalLight3D
            {
                RotationDegrees = new Vector3(-52f, -34f, 0f),
                LightColor = new Color(1f, 0.90f, 0.80f),
                LightEnergy = 1.35f,
                ShadowEnabled = false
            });
            AddChild(new DirectionalLight3D
            {
                RotationDegrees = new Vector3(40f, 146f, 0f),
                LightColor = new Color(0.74f, 0.84f, 1f),
                LightEnergy = 0.72f,
                ShadowEnabled = false
            });
            var world = new WorldEnvironment
            {
                Environment = new Godot.Environment
                {
                    BackgroundMode = Godot.Environment.BGMode.Color,
                    BackgroundColor = new Color(0.012f, 0.016f, 0.024f),
                    AmbientLightSource = Godot.Environment.AmbientSource.Color,
                    AmbientLightColor = new Color(0.46f, 0.50f, 0.58f),
                    AmbientLightEnergy = 0.48f,
                    TonemapMode = Godot.Environment.ToneMapper.Filmic
                }
            };
            AddChild(world);

            var ceiling = preview.FindChild("ceiling", true, false) as GeometryInstance3D;
            var views = new List<View>
            {
                new("01_player_eye_backbar", new Vector3(0.55f, 1.83f, -2.08f), new Vector3(-2.80f, 1.58f, -4.08f), 84f),
                new("02_customer_eye_front", new Vector3(-2.80f, 1.62f, 1.10f), new Vector3(-2.80f, 1.08f, -1.55f), 80f),
                new("03_east_sink_waste_gate", new Vector3(3.10f, 2.15f, -0.75f), new Vector3(0.90f, 0.90f, -2.15f)),
                new("04_west_manual_only", new Vector3(-5.80f, 2.05f, -1.10f), new Vector3(-7.0f, 1.12f, -2.72f)),
                new("05_customer_lounge_and_lights", new Vector3(7.0f, 3.0f, 4.0f), new Vector3(3.8f, 1.0f, 0.25f)),
                new("06_overhead_layout", new Vector3(0f, 12.5f, 0f), Vector3.Zero, 70f, true)
            };
            var output = ProjectSettings.GlobalizePath("res://artifacts/bar-interior-formal-forward-plus");
            DirAccess.MakeDirRecursiveAbsolute(output);
            foreach (var view in views)
            {
                if (ceiling is not null)
                    ceiling.Visible = !view.HideCeiling;
                camera.Fov = view.Fov;
                camera.LookAtFromPosition(view.Position, view.Target,
                    view.HideCeiling ? Vector3.Forward : Vector3.Up);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var image = GetViewport().GetTexture().GetImage();
                if (image.GetWidth() != 1920 || image.GetHeight() != 1080)
                    throw new InvalidOperationException($"Expected 1920x1080, got {image.GetWidth()}x{image.GetHeight()}.");
                var path = System.IO.Path.Combine(output, view.Name + ".png");
                var error = image.SavePng(path);
                if (error != Error.Ok)
                    throw new InvalidOperationException($"Could not save {view.Name}: {error}");
                GD.Print($"BAR_INTERIOR_FORMAL_CAPTURE {view.Name} {path}");
            }
            GD.Print("BAR_INTERIOR_FORMAL_FORWARD_PLUS_CAPTURE_PASS");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }
}
