using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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

    private readonly record struct ReviewPass(
        string Folder,
        BarFormalReviewMode Mode,
        Color KeyColor,
        Color FillColor,
        Color Background,
        Color Ambient,
        float AmbientEnergy,
        Color TableLightColor,
        float TableLightEnergy);

    public override void _Ready() => CallDeferred(MethodName.Capture);

    private async void Capture()
    {
        try
        {
            var preview = GetNode<Node3D>("Preview");
            var presenter = new BarFormalReviewPresentation(preview);
            var camera = new Camera3D { Current = true, Fov = 70f };
            AddChild(camera);
            var keyLight = new DirectionalLight3D
            {
                RotationDegrees = new Vector3(-52f, -34f, 0f),
                LightColor = new Color(1f, 0.90f, 0.80f),
                LightEnergy = 1.35f,
                ShadowEnabled = false
            };
            AddChild(keyLight);
            var fillLight = new DirectionalLight3D
            {
                RotationDegrees = new Vector3(40f, 146f, 0f),
                LightColor = new Color(0.74f, 0.84f, 1f),
                LightEnergy = 0.72f,
                ShadowEnabled = false
            };
            AddChild(fillLight);
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
            var tableLights = new List<SpotLight3D>();
            foreach (var position in new[]
                     {
                         new Vector3(4.35f, 2.55f, -2.15f),
                         new Vector3(4.65f, 2.55f, 0.25f),
                         new Vector3(4.35f, 2.55f, 2.65f)
                     })
            {
                var tableLight = new SpotLight3D
                {
                    Position = position,
                    RotationDegrees = new Vector3(-90f, 0f, 0f),
                    LightColor = new Color(1f, 0.72f, 0.48f),
                    LightEnergy = 8.0f,
                    SpotRange = 3.4f,
                    SpotAngle = 48f,
                    SpotAngleAttenuation = 0.72f,
                    ShadowEnabled = false
                };
                AddChild(tableLight);
                tableLights.Add(tableLight);
            }

            var ceiling = preview.FindChild("ceiling", true, false) as GeometryInstance3D;
            var views = new List<View>
            {
                new("01_player_eye_backbar", new Vector3(0.55f, 1.83f, -2.08f), new Vector3(-2.80f, 1.58f, -4.08f), 84f),
                new("02_customer_eye_front", new Vector3(-2.80f, 1.62f, 1.10f), new Vector3(-2.80f, 1.08f, -1.55f), 80f),
                new("03_east_sink_waste_gate", new Vector3(3.10f, 2.15f, -0.75f), new Vector3(0.90f, 0.90f, -2.15f)),
                new("04_west_manual_only", new Vector3(-5.80f, 2.05f, -1.10f), new Vector3(-7.0f, 1.12f, -2.72f)),
                new("05_customer_lounge_and_lights", new Vector3(7.0f, 3.0f, 4.0f), new Vector3(3.8f, 1.0f, 0.25f)),
                new("06_overhead_layout", new Vector3(0f, 12.5f, 0f), Vector3.Zero, 70f, true),
                new("07_south_entry_clean", new Vector3(-2.35f, 1.82f, 1.95f),
                    new Vector3(-0.65f, 1.20f, 4.98f), 66f),
                new("08_sink_bowl_closeup", new Vector3(1.90f, 2.45f, -0.62f),
                    new Vector3(0.65f, 1.02f, -1.53f), 48f)
            };
            var passes = new[]
            {
                new ReviewPass("neutral", BarFormalReviewMode.NeutralClay,
                    new Color(1f, 1f, 1f), new Color(0.92f, 0.95f, 1f),
                    new Color(0.055f, 0.060f, 0.068f), new Color(0.82f, 0.84f, 0.88f), 0.62f,
                    new Color(1f, 0.96f, 0.90f), 4.0f),
                new ReviewPass("reality", BarFormalReviewMode.RealityWarm,
                    new Color(1f, 0.90f, 0.80f), new Color(0.74f, 0.84f, 1f),
                    new Color(0.012f, 0.016f, 0.024f), new Color(0.46f, 0.50f, 0.58f), 0.48f,
                    new Color(1f, 0.70f, 0.42f), 8.0f),
                new ReviewPass("glasses", BarFormalReviewMode.GlassesCold,
                    new Color(0.68f, 0.82f, 1f), new Color(0.34f, 0.56f, 0.78f),
                    new Color(0.006f, 0.014f, 0.028f), new Color(0.24f, 0.42f, 0.62f), 0.52f,
                    new Color(0.48f, 0.72f, 1f), 6.0f)
            };
            var outputRoot = ProjectSettings.GlobalizePath("res://artifacts/bar-interior-formal-preintegration");
            foreach (var pass in passes)
            {
                presenter.Apply(pass.Mode);
                keyLight.LightColor = pass.KeyColor;
                fillLight.LightColor = pass.FillColor;
                world.Environment.BackgroundColor = pass.Background;
                world.Environment.AmbientLightColor = pass.Ambient;
                world.Environment.AmbientLightEnergy = pass.AmbientEnergy;
                foreach (var tableLight in tableLights)
                {
                    tableLight.LightColor = pass.TableLightColor;
                    tableLight.LightEnergy = pass.TableLightEnergy;
                }
                var output = Path.Combine(outputRoot, pass.Folder);
                DirAccess.MakeDirRecursiveAbsolute(output);
                foreach (var view in views)
                    await CaptureView(camera, ceiling, view, output, pass.Folder);
            }

            presenter.Apply(BarFormalReviewMode.RealityWarm);
            keyLight.LightColor = passes[1].KeyColor;
            fillLight.LightColor = passes[1].FillColor;
            world.Environment.BackgroundColor = passes[1].Background;
            world.Environment.AmbientLightColor = passes[1].Ambient;
            world.Environment.AmbientLightEnergy = passes[1].AmbientEnergy;
            foreach (var tableLight in tableLights)
            {
                tableLight.LightColor = passes[1].TableLightColor;
                tableLight.LightEnergy = passes[1].TableLightEnergy;
            }
            var openStateOutput = Path.Combine(outputRoot, "open-states");
            DirAccess.MakeDirRecursiveAbsolute(openStateOutput);
            preview.GetNode<Node3D>("Lighting").Visible = false;

            var drawers = new List<Node3D>();
            for (var bay = 1; bay <= 4; bay++)
            {
                var drawer = RequireNode(preview, $"front_drawer_{bay}_upper");
                drawer.Position += new Vector3(0f, 0f, -0.38f);
                drawers.Add(drawer);
            }
            await CaptureView(camera, ceiling,
                new View("09_open_drawer_clearance", new Vector3(-3.65f, 1.82f, -3.45f),
                    new Vector3(-3.65f, 0.72f, -1.80f), 78f), openStateOutput, "open-state");
            foreach (var drawer in drawers)
                drawer.Position -= new Vector3(0f, 0f, -0.38f);

            var sliders = new List<Node3D>();
            for (var bay = 1; bay <= 5; bay++)
            {
                var slider = RequireNode(preview, $"rear_lower_cabinet_{bay}_moving");
                slider.Position += new Vector3(-0.79f, 0f, 0f);
                sliders.Add(slider);
            }
            await CaptureView(camera, ceiling,
                new View("10_all_rear_sliding_doors_open", new Vector3(-2.80f, 1.70f, -1.48f),
                    new Vector3(-2.80f, 0.58f, -3.57f), 76f), openStateOutput, "open-state");
            foreach (var slider in sliders)
                slider.Position -= new Vector3(-0.79f, 0f, 0f);

            for (var bay = 1; bay <= 5; bay++)
            {
                RequireNode(preview, $"back_cabinet_{bay}_left").RotationDegrees += new Vector3(0f, -85f, 0f);
                RequireNode(preview, $"back_cabinet_{bay}_right").RotationDegrees += new Vector3(0f, 85f, 0f);
            }
            await CaptureView(camera, ceiling,
                new View("11_all_upper_doors_open", new Vector3(-2.80f, 3.08f, -1.10f),
                    new Vector3(-2.80f, 3.28f, -3.70f), 78f), openStateOutput, "open-state");

            GD.Print("BAR_INTERIOR_FORMAL_PREINTEGRATION_CAPTURE_PASS");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private async Task CaptureView(
        Camera3D camera,
        GeometryInstance3D? ceiling,
        View view,
        string output,
        string passName)
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
        var path = Path.Combine(output, view.Name + ".png");
        var error = image.SavePng(path);
        if (error != Error.Ok)
            throw new InvalidOperationException($"Could not save {passName}/{view.Name}: {error}");
        GD.Print($"BAR_INTERIOR_FORMAL_CAPTURE {passName}/{view.Name} {path}");
    }

    private static Node3D RequireNode(Node3D preview, string name) =>
        preview.FindChild(name, true, false) as Node3D
        ?? throw new InvalidOperationException($"Formal preview is missing {name}.");
}
