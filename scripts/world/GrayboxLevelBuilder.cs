using Godot;

namespace GlassesBar;

/// <summary>
/// Godot composition root for the current graybox scene. Layout data, architecture
/// presentation, cabinet assembly, and gameplay bindings live in dedicated collaborators.
/// </summary>
public partial class GrayboxLevelBuilder : Node3D
{
    // Compatibility constants remain here because integration tests and scene setup
    // already use this public surface. The authoritative values live in the layout definition.
    public const float FrontBarTopHeight = BarLayoutDefinition.FrontBarTopHeight;
    public const float RearShelfTopHeight = BarLayoutDefinition.RearShelfTopHeight;
    public const float PlayerEyeHeight = BarLayoutDefinition.PlayerEyeHeight;
    public const float OperationAisleClearWidth = BarLayoutDefinition.OperationAisleClearWidth;
    public const float BottleRackTopHeight = BarLayoutDefinition.BottleRackTopHeight;
    public const float UpperCabinetCenterHeight = BarLayoutDefinition.UpperCabinetCenterHeight;

    public override void _Ready()
    {
        var layout = BarLayoutDefinition.Prototype;
        layout.Validate();

        var neutral = GetNode<Node3D>("NeutralGameplay");
        var reality = GetNode<Node3D>("RealityWorld");
        var glasses = GetNode<Node3D>("GlassesWorld");

        var architecture = new GrayboxArchitectureBuilder(
            layout,
            neutral,
            reality,
            glasses);
        architecture.BuildCollisions();
        architecture.BuildGrayboxVisuals();

        var gameplay = new GameplaySceneComposer(
            this,
            layout,
            neutral,
            architecture);
        var workstation = gameplay.CreateWorkstation();
        gameplay.BuildCounterSurfaces(workstation);

        var cabinetry = new CabinetBuilder(layout, neutral);
        cabinetry.Build();

        gameplay.BuildStations(cabinetry);
        gameplay.BuildWorkboard(workstation);
        gameplay.BuildTools(workstation, cabinetry);
        gameplay.BindRuntime(workstation, cabinetry.ResetAll);
    }
}
