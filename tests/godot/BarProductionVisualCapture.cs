using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GlassesBar.Tests;

public partial class BarProductionVisualCapture : Node
{
    private readonly record struct CaptureState(
        string Name,
        Vector3 CameraPosition,
        Vector3 Target,
        bool Glasses = false,
        string? OpenStorageId = null,
        bool PullOutChairs = false,
        bool ShowDiagnostics = false,
        Vector3? Up = null);

    private Node3D _main = null!;
    private Camera3D _camera = null!;
    private CabinetInteractable[] _storage = [];
    private Node3D _diagnostics = null!;
    private Node3D _technicalFillLights = null!;

    public override void _Ready() => CallDeferred(MethodName.CaptureAll);

    private async void CaptureAll()
    {
        try
        {
            _main = GetNode<Node3D>("Main");
            _main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Start")
                .EmitSignal(Button.SignalName.Pressed);
            _main.GetNode<HudController>("HUD").Visible = false;
            _main.GetNode<MyopiaEffectController>("MyopiaEffectController").SetMyopiaDegrees(0f, false);
            _main.GetNode<CanvasLayer>("RealityEffects").Visible = false;
            _main.GetNode<CanvasLayer>("GlassesInfo").Visible = false;

            _camera = new Camera3D
            {
                Name = "BarProductionReviewCamera",
                Current = true,
                Fov = 66f
            };
            _main.AddChild(_camera);
            _storage = _main.GetTree().GetNodesInGroup("cabinet_storage")
                .OfType<CabinetInteractable>()
                .ToArray();
            _diagnostics = BuildDiagnostics();
            _main.AddChild(_diagnostics);
            _technicalFillLights = BuildTechnicalFillLights();
            _main.AddChild(_technicalFillLights);

            var outputDirectory = ProjectSettings.GlobalizePath(
                "res://artifacts/visual_review_bar_graybox_z3_h3_detail_fix");
            DirAccess.MakeDirRecursiveAbsolute(outputDirectory);
            var states = new List<CaptureState>
            {
                new("01_overhead_9m10_span", new Vector3(0f, 12.5f, 0f), Vector3.Zero, Up: Vector3.Forward),
                new("02_player_eye_customer_view", new Vector3(-2.8f, 1.83f, -2.72f), new Vector3(-2.8f, 1.18f, 1.0f)),
                new("03_west_manual_only", new Vector3(-5.65f, 2.15f, -1.05f), new Vector3(-7.0f, 1.12f, -2.72f)),
                new("04_east_waste_and_gate", new Vector3(3.05f, 2.25f, -0.65f), new Vector3(1.35f, 0.82f, -3.0f)),
                new("05_east_sink_open_underbay", new Vector3(0.65f, 1.65f, -3.25f), new Vector3(0.65f, 0.66f, -1.38f)),
                new("06_west_chamfer_close", new Vector3(-5.95f, 1.75f, -3.0f), new Vector3(-7.0f, 1.10f, -1.60f)),
                new("07_east_chamfer_close", new Vector3(0.25f, 1.75f, -3.05f), new Vector3(1.40f, 1.10f, -1.60f)),
                new("08_all_front_storage_closed", new Vector3(-2.80f, 1.90f, -3.25f), new Vector3(-2.80f, 0.72f, -1.82f)),
                new("09_tool_storage_open", new Vector3(-5.30f, 1.85f, -3.15f), new Vector3(-6.24f, 0.82f, -1.82f), OpenStorageId: "front_drawer_1_upper"),
                new("10_ice_drawer_fully_open", new Vector3(-3.95f, 1.80f, -3.25f), new Vector3(-4.78f, 0.82f, -1.82f), OpenStorageId: "front_drawer_2_upper"),
                new("11_five_bay_empty_rack_front", new Vector3(-2.80f, 2.42f, -1.85f), new Vector3(-2.80f, 1.85f, -4.02f)),
                new("12_coffee_kettle_cabinets_open", new Vector3(-4.75f, 1.85f, -1.82f), new Vector3(-6.30f, 0.58f, -3.82f), OpenStorageId: "rear_lower_cabinet_1"),
                new("13_customer_chairs_pulled", new Vector3(7.15f, 4.35f, 4.25f), new Vector3(4.35f, 0.72f, 0.40f), PullOutChairs: true),
                new("14_reality_lighting", new Vector3(6.80f, 3.35f, 3.40f), new Vector3(-2.40f, 1.45f, -2.25f)),
                new("15_glasses_lighting", new Vector3(6.80f, 3.35f, 3.40f), new Vector3(-2.40f, 1.45f, -2.25f), Glasses: true),
                new("16_runtime_aabb_overview", new Vector3(3.75f, 8.40f, 5.65f), new Vector3(-2.25f, 1.25f, -2.15f), ShowDiagnostics: true),
                new("17_guest_counter_extension_close", new Vector3(-2.80f, 1.75f, 0.25f), new Vector3(-2.80f, 1.28f, -0.86f)),
                new("18_front_drawer_carcass_clearance", new Vector3(-0.65f, 1.45f, -3.20f), new Vector3(-1.86f, 0.58f, -1.70f), OpenStorageId: "front_drawer_4_lower"),
                new("19_rear_sliding_door_open", new Vector3(-5.05f, 1.45f, -1.90f), new Vector3(-6.20f, 0.52f, -3.58f), OpenStorageId: "rear_lower_cabinet_1"),
                new("20_sink_exposed_plumbing_close", new Vector3(0.65f, 1.20f, -2.85f), new Vector3(0.65f, 0.56f, -1.42f)),
                new("21_player_worktop_surface_continuity", new Vector3(-2.80f, 2.05f, -2.85f), new Vector3(-2.80f, 1.12f, -1.35f)),
                new("22_guest_top_surface_continuity", new Vector3(-2.80f, 2.18f, 0.35f), new Vector3(-2.80f, 1.38f, -0.82f))
            };

            foreach (var state in states)
            {
                ApplyState(state);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var image = GetViewport().GetTexture().GetImage();
                if (image.GetWidth() != 1920 || image.GetHeight() != 1080)
                    throw new InvalidOperationException(
                        $"Review frame must be 1920x1080, got {image.GetWidth()}x{image.GetHeight()}.");
                var path = System.IO.Path.Combine(outputDirectory, state.Name + ".png");
                var error = image.SavePng(path);
                if (error != Error.Ok)
                    throw new InvalidOperationException($"Could not save review frame {state.Name}: {error}");
                GD.Print($"BAR_GRAYBOX_CAPTURE {state.Name} {path}");
            }

            GD.Print("BAR_PRODUCTION_VISUAL_CAPTURE_PASS");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private void ApplyState(CaptureState state)
    {
        if ((GameSession.Instance.WorldMode == WorldMode.Glasses) != state.Glasses)
            GameSession.Instance.ToggleWorld();
        foreach (var cabinet in _storage)
            cabinet.SetOpen(false, false);
        if (!string.IsNullOrEmpty(state.OpenStorageId))
            _main.GetNode<CabinetInteractable>($"NeutralGameplay/{state.OpenStorageId}")
                .SetOpen(true, false);
        SetChairPositions("RealityWorld/LoungeChairs", state.PullOutChairs);
        SetChairPositions("GlassesWorld/LoungeChairs", state.PullOutChairs);
        _diagnostics.Visible = state.ShowDiagnostics;
        _technicalFillLights.Visible = state.Name is not "14_reality_lighting" and not "15_glasses_lighting";
        var showCeiling = state.Name is not "01_overhead_9m10_span" and not "16_runtime_aabb_overview";
        _main.GetNode<MeshInstance3D>("RealityWorld/Ceiling").Visible = showCeiling;
        _main.GetNode<MeshInstance3D>("GlassesWorld/Ceiling").Visible = showCeiling;
        _camera.LookAtFromPosition(state.CameraPosition, state.Target, state.Up ?? Vector3.Up);
    }

    private static Node3D BuildTechnicalFillLights()
    {
        var root = new Node3D { Name = "TechnicalReviewFillLights" };
        root.AddChild(new DirectionalLight3D
        {
            Name = "Key",
            RotationDegrees = new Vector3(-52f, -32f, 0f),
            LightColor = new Color(1f, 0.94f, 0.88f),
            LightEnergy = 1.15f,
            ShadowEnabled = false
        });
        root.AddChild(new DirectionalLight3D
        {
            Name = "Fill",
            RotationDegrees = new Vector3(38f, 148f, 0f),
            LightColor = new Color(0.82f, 0.9f, 1f),
            LightEnergy = 0.75f,
            ShadowEnabled = false
        });
        return root;
    }

    private Node3D BuildDiagnostics()
    {
        var root = new Node3D { Name = "RuntimeAabbDiagnostics", Visible = false };
        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.1f, 0.85f, 0.75f, 0.16f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };
        foreach (var storage in BarLayoutDefinition.Prototype.Storages)
            root.AddChild(new MeshInstance3D
            {
                Name = storage.Id + "_Envelope",
                Position = storage.HostPosition,
                Mesh = new BoxMesh { Size = storage.HostSize },
                MaterialOverride = material
            });
        var sinkClear = BarLayoutDefinition.Prototype.SinkUnderClearVolume;
        root.AddChild(new MeshInstance3D
        {
            Name = "SinkUnderClearEnvelope",
            Position = sinkClear.Position,
            Mesh = new BoxMesh { Size = sinkClear.Size },
            MaterialOverride = material
        });
        return root;
    }

    private void SetChairPositions(string path, bool pulledOut)
    {
        var group = _main.GetNode<Node3D>(path);
        var chairs = BarLayoutDefinition.Prototype.LoungeChairs;
        for (var index = 0; index < chairs.Count; index++)
            group.GetNode<Node3D>(chairs[index].Id).Position =
                pulledOut ? chairs[index].PulledOutPosition : chairs[index].Position;
    }
}
