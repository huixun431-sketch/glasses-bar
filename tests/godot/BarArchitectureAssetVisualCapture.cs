using System;
using System.Collections.Generic;
using Godot;

namespace GlassesBar.Tests;

// Generated capture skeleton for bar-architecture. Replace the stop marker with a
// deterministic non-headless Forward+ sequence using only approved views and poses.
public partial class BarArchitectureAssetVisualCapture : Node
{
    public override void _Ready()
    {
        CallDeferred(MethodName.Prepare);
    }

    private async void Prepare()
    {
        try
        {
            GetNode<Node3D>("Main").Visible = false;
            var scene = ResourceLoader.Load<PackedScene>(
                "res://scenes/environment/modules/bar_architecture.tscn") ??
                throw new InvalidOperationException("Missing bar architecture wrapper.");
            AddChild(scene.Instantiate<Node3D>());
            var camera = new Camera3D { Current = true, Fov = 70f };
            AddChild(camera);
            AddChild(new DirectionalLight3D
            {
                RotationDegrees = new Vector3(-48f, -35f, 0f),
                LightEnergy = 1.3f,
                ShadowEnabled = false
            });
            AddChild(new DirectionalLight3D
            {
                RotationDegrees = new Vector3(42f, 145f, 0f),
                LightColor = new Color(0.78f, 0.86f, 1f),
                LightEnergy = 0.7f,
                ShadowEnabled = false
            });

            var output = ProjectSettings.GlobalizePath("res://artifacts/bar-architecture-forward-plus");
            DirAccess.MakeDirRecursiveAbsolute(output);
            var views = new List<(string Name, Vector3 Position, Vector3 Target)>
            {
                ("01_south_door_clean", new Vector3(0f, 1.90f, -3.85f), new Vector3(1.8f, 1.65f, 4.75f)),
                ("02_north_openings", new Vector3(0f, 1.90f, 3.85f), new Vector3(1.4f, 1.65f, -4.75f)),
                ("03_three_quarter_scale", new Vector3(5.8f, 3.0f, 3.8f), new Vector3(-1.0f, 1.25f, -2.6f))
            };
            foreach (var view in views)
            {
                camera.LookAtFromPosition(view.Position, view.Target, Vector3.Up);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var image = GetViewport().GetTexture().GetImage();
                if (image.GetWidth() != 1920 || image.GetHeight() != 1080)
                    throw new InvalidOperationException(
                        $"Architecture review frame must be 1920x1080, got {image.GetWidth()}x{image.GetHeight()}.");
                var path = System.IO.Path.Combine(output, view.Name + ".png");
                var error = image.SavePng(path);
                if (error != Error.Ok)
                    throw new InvalidOperationException($"Could not save {view.Name}: {error}");
                GD.Print($"BAR_ARCHITECTURE_FORWARD_PLUS_CAPTURE {view.Name} {path}");
            }
            GD.Print("BAR_ARCHITECTURE_FORWARD_PLUS_CAPTURE_PASS");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }
}
