using System;
using System.Collections.Generic;
using Godot;

namespace GlassesBar.Tests;

public partial class BarProductionVisualCapture : Node
{
    private readonly record struct CaptureState(
        string Name,
        Vector3 CameraPosition,
        Vector3 Target,
        bool Glasses = false,
        bool OpenIceDrawer = false,
        bool OpenRearDoor = false,
        bool PullOutChairs = false,
        Vector3? Up = null);

    private Node3D _main = null!;
    private Camera3D _camera = null!;
    private CabinetInteractable _iceDrawer = null!;
    private CabinetInteractable _rearDoor = null!;

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

            _camera = new Camera3D { Name = "BarProductionReviewCamera", Current = true, Fov = 66f };
            _main.AddChild(_camera);
            _iceDrawer = _main.GetNode<CabinetInteractable>("NeutralGameplay/front_drawer_2_upper");
            _rearDoor = _main.GetNode<CabinetInteractable>("NeutralGameplay/back_cabinet_2_left");

            var outputDirectory = ProjectSettings.GlobalizePath("res://artifacts/visual_review_bar_graybox");
            DirAccess.MakeDirRecursiveAbsolute(outputDirectory);
            var states = new List<CaptureState>
            {
                new("01_overhead_orientation", new Vector3(0f, 9.2f, 0f), new Vector3(0f, 0f, 0f), Up: Vector3.Forward),
                new("02_player_eye_south", new Vector3(-2.4f, 1.83f, -2.7f), new Vector3(-2.4f, 1.10f, 0.65f)),
                new("03_west_north_u_interior", new Vector3(-5.45f, 2.85f, -0.25f), new Vector3(-2.35f, 1.05f, -2.75f)),
                new("04_east_wet_side", new Vector3(1.35f, 1.85f, -1.45f), new Vector3(-0.05f, 0.92f, -2.55f)),
                new("05_west_manual_shelf", new Vector3(-3.55f, 1.65f, -1.70f), new Vector3(-4.86f, 1.10f, -2.62f)),
                new("06_rear_bar_open_door", new Vector3(-2.40f, 2.35f, -0.65f), new Vector3(-2.40f, 2.25f, -3.72f), OpenRearDoor: true),
                new("07_open_ice_drawer_reach", new Vector3(-1.15f, 1.65f, -2.78f), new Vector3(-3.25f, 0.68f, -2.18f), OpenIceDrawer: true),
                new("08_customer_chairs_pulled", new Vector3(5.25f, 3.10f, 3.85f), new Vector3(2.45f, 0.72f, 1.25f), PullOutChairs: true),
                new("09_south_entry_window", new Vector3(0.0f, 2.60f, -1.0f), new Vector3(1.25f, 1.45f, 4.50f)),
                new("10_reality_lighting", new Vector3(3.85f, 2.45f, 2.20f), new Vector3(-2.25f, 1.45f, -2.35f)),
                new("11_glasses_lighting", new Vector3(3.85f, 2.45f, 2.20f), new Vector3(-2.25f, 1.45f, -2.35f), Glasses: true)
            };

            foreach (var state in states)
            {
                ApplyState(state);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var image = GetViewport().GetTexture().GetImage();
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
        _iceDrawer.SetOpen(state.OpenIceDrawer, false);
        _rearDoor.SetOpen(state.OpenRearDoor, false);
        SetChairPositions("RealityWorld/LoungeChairs", state.PullOutChairs);
        SetChairPositions("GlassesWorld/LoungeChairs", state.PullOutChairs);
        var showCeiling = state.Name != "01_overhead_orientation";
        _main.GetNode<MeshInstance3D>("RealityWorld/Ceiling").Visible = showCeiling;
        _main.GetNode<MeshInstance3D>("GlassesWorld/Ceiling").Visible = showCeiling;
        _camera.LookAtFromPosition(state.CameraPosition, state.Target, state.Up ?? Vector3.Up);
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
