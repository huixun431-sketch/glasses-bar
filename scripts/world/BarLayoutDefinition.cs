using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GlassesBar;

public readonly record struct BarBoxLayout(string Name, Vector3 Position, Vector3 Size);

public readonly record struct BarCylinderLayout(
    string Name,
    Vector3 Position,
    float Radius,
    float Height,
    Color RealityColor);

public readonly record struct BarStationLayout(
    string Id,
    StationKind Kind,
    Vector3 Position,
    Vector3 Size,
    string Label);

public readonly record struct BarToolLayout(
    string ToolId,
    Vector3 Position,
    Color Color);

public readonly record struct BarCounterSurfaceLayout(
    string Id,
    Vector3 Position,
    Vector3 Size);

public readonly record struct BarOpeningLayout(
    string Id,
    Vector3 Position,
    Vector3 Size,
    float SillHeight,
    Vector3 OpenDirection,
    int LeafCount);

public readonly record struct BarChairLayout(
    string Id,
    Vector3 Position,
    Vector3 Size,
    Vector3 PulledOutPosition);

public readonly record struct BarLightFixtureLayout(
    string Id,
    Vector3 Position,
    string Group,
    bool HasVisibleGeometry);

public readonly record struct BarRotatedBoxLayout(
    string Name,
    Vector3 Position,
    Vector3 Size,
    Vector3 RotationDegrees);

public sealed class BarWorkboardLayout
{
    public required Vector3 Position { get; init; }
    public required Vector3 Size { get; init; }
    public required IReadOnlyList<Vector3> Slots { get; init; }
}

public readonly record struct BarCabinetLayout(
    string Id,
    CabinetPartKind Kind,
    Vector3 Center,
    Vector3 Size,
    bool HingeOnLeft,
    Vector3 OutwardDirection,
    float StorageDepth,
    float OpenTravelDistance,
    BarBoxLayout? Cavity,
    bool ContainsIceBucket);

/// <summary>
/// Immutable coordinates, dimensions, and stable node IDs for the current graybox.
/// It contains no scene nodes and does not own gameplay state.
/// </summary>
public sealed class BarLayoutDefinition
{
    public const float RoomWidth = 12f;
    public const float RoomDepth = 9f;
    public const float RoomHeight = 3.5f;
    public const float WallThickness = 0.20f;
    public const float FrontOutlineWidth = 5.60f;
    public const float InternalClearSpan = 4.15f;
    public const float EastWetSideDepth = 0.80f;
    public const float WestDrySideDepth = 0.65f;
    public const float HandoffStripWidth = 0.90f;
    public const int FrontFacadeBayCount = 3;
    public const float FrontSectionDepth = 0.80f;
    public const float GuestSurfaceDepth = 0.18f;
    public const float PlayerSurfaceDepth = 0.62f;
    public const float FrontBarTopHeight = 1.20f;
    public const float PlayerWorktopHeight = 0.96f;
    public const float RearShelfTopHeight = PlayerWorktopHeight;
    public const float PlayerEyeHeight = 1.83f;
    public const float OperationAisleClearWidth = 1.40f;
    public const float MainCustomerRouteClearWidth = 1.20f;
    public const float SecondaryCustomerRouteClearWidth = 0.90f;
    public const float DrawerOpenTravel = 0.38f;
    public const float BottleRackTopHeight = 1.68f;
    public const float UpperCabinetCenterHeight = 2.60f;
    public const float BarCenterX = -2.40f;
    public const float FrontCounterZ = -1.60f;
    public const float RearShelfZ = -3.65f;
    public const float UpperCabinetFrontZ = -3.52f;
    public const float RearBarFrontZ = -3.40f;
    public const float FrontBarInnerEdgeZ = -2.00f;
    public const float PlayerStartZ = -2.70f;

    public static BarLayoutDefinition Prototype { get; } = new();

    private BarLayoutDefinition()
    {
        Floor = new BarBoxLayout(
            "BarFloor",
            new Vector3(0f, -0.15f, 0f),
            new Vector3(RoomWidth, 0.3f, RoomDepth));
        Walls = Array.AsReadOnly(new[]
        {
            new BarBoxLayout("NorthWall", new Vector3(0f, RoomHeight * 0.5f, -RoomDepth * 0.5f), new Vector3(RoomWidth, RoomHeight, WallThickness)),
            new BarBoxLayout("WestWall", new Vector3(-RoomWidth * 0.5f, RoomHeight * 0.5f, 0f), new Vector3(WallThickness, RoomHeight, RoomDepth)),
            new BarBoxLayout("EastWall", new Vector3(RoomWidth * 0.5f, RoomHeight * 0.5f, 0f), new Vector3(WallThickness, RoomHeight, RoomDepth)),
            new BarBoxLayout("SouthWall", new Vector3(0f, RoomHeight * 0.5f, RoomDepth * 0.5f), new Vector3(RoomWidth, RoomHeight, WallThickness))
        });
        Ceiling = new BarBoxLayout(
            "Ceiling",
            new Vector3(0f, RoomHeight + WallThickness * 0.5f, 0f),
            new Vector3(RoomWidth, WallThickness, RoomDepth));
        WainscotSections = Array.AsReadOnly(new[]
        {
            new BarBoxLayout("WestWainscot", new Vector3(-5.89f, 0.525f, 0f), new Vector3(0.02f, 1.05f, RoomDepth)),
            new BarBoxLayout("EastWainscot", new Vector3(5.89f, 0.525f, 0f), new Vector3(0.02f, 1.05f, RoomDepth)),
            new BarBoxLayout("NorthWestWainscot", new Vector3(-0.675f, 0.525f, -4.39f), new Vector3(10.65f, 1.05f, 0.02f)),
            new BarBoxLayout("NorthEastWainscot", new Vector3(5.775f, 0.525f, -4.39f), new Vector3(0.45f, 1.05f, 0.02f)),
            new BarBoxLayout("SouthWestWainscot", new Vector3(-3.85f, 0.525f, 4.39f), new Vector3(4.30f, 1.05f, 0.02f)),
            new BarBoxLayout("SouthCenterWainscot", new Vector3(0.65f, 0.525f, 4.39f), new Vector3(1.90f, 1.05f, 0.02f)),
            new BarBoxLayout("SouthEastWainscot", new Vector3(5.40f, 0.525f, 4.39f), new Vector3(1.20f, 1.05f, 0.02f)),
            new BarBoxLayout("SouthWindowSillWainscot", new Vector3(3.20f, 0.375f, 4.39f), new Vector3(3.20f, 0.75f, 0.02f))
        });

        RoomClearSize = new Vector3(RoomWidth, RoomHeight, RoomDepth);
        SouthMainEntry = new BarOpeningLayout(
            "south_main_entry",
            new Vector3(-1f, 1.05f, RoomDepth * 0.5f),
            new Vector3(1.40f, 2.10f, WallThickness),
            0f,
            Vector3.Forward,
            2);
        NorthEastServiceDoor = new BarOpeningLayout(
            "north_east_service_door",
            new Vector3(5.10f, 1.05f, -RoomDepth * 0.5f),
            new Vector3(0.90f, 2.10f, WallThickness),
            0f,
            Vector3.Forward,
            1);
        SouthWindows = Array.AsReadOnly(new[]
        {
            new BarOpeningLayout(
                "south_east_window",
                new Vector3(3.20f, 1.525f, RoomDepth * 0.5f),
                new Vector3(3.20f, 1.55f, WallThickness),
                0.75f,
                Vector3.Zero,
                0)
        });

        var frontBodySize = new Vector3(FrontOutlineWidth, PlayerWorktopHeight, FrontSectionDepth);
        FrontBarBody = new BarBoxLayout(
            "FrontBarBody",
            new Vector3(BarCenterX, frontBodySize.Y * 0.5f, FrontCounterZ),
            frontBodySize);
        FrontBarBodySections = Array.AsReadOnly(new[]
        {
            new BarBoxLayout("FrontBarSouthBody", new Vector3(BarCenterX, frontBodySize.Y * 0.5f, -1.425f), new Vector3(FrontOutlineWidth, frontBodySize.Y, 0.45f)),
            new BarBoxLayout("FrontBarCenterRearBody", new Vector3(-2.475f, frontBodySize.Y * 0.5f, -1.825f), new Vector3(3.45f, frontBodySize.Y, 0.35f)),
            new BarBoxLayout("FrontBarWestRearBody", new Vector3(-4.875f, frontBodySize.Y * 0.5f, -1.825f), new Vector3(WestDrySideDepth, frontBodySize.Y, 0.35f)),
            new BarBoxLayout("FrontBarEastRearBody", new Vector3(0f, frontBodySize.Y * 0.5f, -1.825f), new Vector3(EastWetSideDepth, frontBodySize.Y, 0.35f))
        });
        FrontBarInnerChamfers = Array.AsReadOnly(new[]
        {
            new BarRotatedBoxLayout("FrontBarWestChamfer", new Vector3(-4.375f, frontBodySize.Y * 0.5f, -1.825f), new Vector3(0.495f, frontBodySize.Y, 0.12f), new Vector3(0f, -45f, 0f)),
            new BarRotatedBoxLayout("FrontBarEastChamfer", new Vector3(-0.575f, frontBodySize.Y * 0.5f, -1.825f), new Vector3(0.495f, frontBodySize.Y, 0.12f), new Vector3(0f, 45f, 0f))
        });
        FrontBarTop = new BarBoxLayout(
            "GuestCounterTop",
            new Vector3(BarCenterX, FrontBarTopHeight - 0.03f, FrontCounterZ + 0.31f),
            new Vector3(FrontOutlineWidth, 0.06f, GuestSurfaceDepth));
        PlayerWorktop = new BarBoxLayout(
            "PlayerWorktop",
            new Vector3(BarCenterX, PlayerWorktopHeight - 0.02f, FrontCounterZ - 0.09f),
            new Vector3(FrontOutlineWidth, 0.04f, PlayerSurfaceDepth));
        PlayerWorktopSections = Array.AsReadOnly(new[]
        {
            new BarBoxLayout("PlayerWorktopSouth", new Vector3(BarCenterX, PlayerWorktopHeight - 0.02f, -1.515f), new Vector3(FrontOutlineWidth, 0.04f, 0.27f)),
            new BarBoxLayout("PlayerWorktopCenterRear", new Vector3(-2.475f, PlayerWorktopHeight - 0.02f, -1.825f), new Vector3(3.45f, 0.04f, 0.35f)),
            new BarBoxLayout("PlayerWorktopWestRear", new Vector3(-4.875f, PlayerWorktopHeight - 0.02f, -1.825f), new Vector3(WestDrySideDepth, 0.04f, 0.35f)),
            new BarBoxLayout("PlayerWorktopEastRear", new Vector3(0f, PlayerWorktopHeight - 0.02f, -1.825f), new Vector3(EastWetSideDepth, 0.04f, 0.35f))
        });
        PlayerWorktopChamfers = Array.AsReadOnly(FrontBarInnerChamfers
            .Select(chamfer => new BarRotatedBoxLayout(
                chamfer.Name.Replace("FrontBar", "PlayerWorktop"),
                new Vector3(chamfer.Position.X, PlayerWorktopHeight - 0.02f, chamfer.Position.Z),
                new Vector3(chamfer.Size.X, 0.04f, chamfer.Size.Z),
                chamfer.RotationDegrees))
            .ToArray());

        var rearShelfSize = new Vector3(FrontOutlineWidth, 0.04f, 0.50f);
        RearWallShelf = new BarBoxLayout(
            "RearBarWorktop",
            new Vector3(BarCenterX, RearShelfTopHeight - rearShelfSize.Y * 0.5f, RearShelfZ),
            rearShelfSize);
        UpperBackCabinet = new BarBoxLayout(
            "UpperBackCabinet",
            new Vector3(BarCenterX, UpperCabinetCenterHeight, -3.71f),
            new Vector3(FrontOutlineWidth, 1f, 0.38f));

        WestDryReturnEnvelope = new BarBoxLayout(
            "WestDryReturnEnvelope",
            new Vector3(-4.875f, PlayerWorktopHeight * 0.5f, -2.50f),
            new Vector3(WestDrySideDepth, PlayerWorktopHeight, 1.80f));
        EastWetReturnEnvelope = new BarBoxLayout(
            "EastWetReturnEnvelope",
            new Vector3(0f, PlayerWorktopHeight * 0.5f, -1.90f),
            new Vector3(EastWetSideDepth, PlayerWorktopHeight, 0.60f));
        CounterReturns = Array.AsReadOnly(new[]
        {
            new BarBoxLayout("WestDryReturn", WestDryReturnEnvelope.Position, WestDryReturnEnvelope.Size),
            new BarBoxLayout("EastWetOuterSupport", new Vector3(0.36f, PlayerWorktopHeight * 0.5f, -1.90f), new Vector3(0.08f, PlayerWorktopHeight, 0.60f)),
            new BarBoxLayout("EastWetInnerSupport", new Vector3(-0.36f, PlayerWorktopHeight * 0.5f, -1.90f), new Vector3(0.08f, PlayerWorktopHeight, 0.60f))
        });
        CounterReturnTops = Array.AsReadOnly(new[]
        {
            new BarBoxLayout("WestDryReturnTop", new Vector3(-4.875f, PlayerWorktopHeight - 0.02f, -2.50f), new Vector3(0.65f, 0.04f, 1.80f)),
            new BarBoxLayout("EastWetReturnTop", new Vector3(0f, PlayerWorktopHeight - 0.02f, -1.90f), new Vector3(0.80f, 0.04f, 0.60f))
        });
        EastWasteModule = new BarBoxLayout(
            "EastWasteModule",
            new Vector3(0f, PlayerWorktopHeight * 0.5f, -3.10f),
            new Vector3(EastWetSideDepth, PlayerWorktopHeight, 0.60f));
        EastWasteModuleTop = new BarBoxLayout(
            "EastWasteModuleTop",
            new Vector3(0f, PlayerWorktopHeight - 0.02f, -3.10f),
            new Vector3(EastWetSideDepth, 0.04f, 0.60f));
        EmployeeGate = new BarBoxLayout(
            "EmployeeGate",
            new Vector3(0f, 0.43f, -2.50f),
            new Vector3(0.08f, 0.86f, 0.60f));
        SinkUnderClearVolume = new BarBoxLayout(
            "SinkUnderClearVolume",
            new Vector3(0f, 0.44f, -2.05f),
            new Vector3(0.44f, 0.84f, 0.52f));
        SouthEntrySwingEnvelope = new BarBoxLayout(
            "SouthEntrySwingEnvelope",
            new Vector3(-1f, 1.05f, 3.75f),
            new Vector3(1.40f, 2.10f, 1.30f));
        SouthWindowAccessEnvelope = new BarBoxLayout(
            "SouthWindowAccessEnvelope",
            new Vector3(3.20f, 1.0f, 3.95f),
            new Vector3(3.20f, 2.0f, 0.90f));

        BottleRackBack = new BarBoxLayout(
            "MergedBottleRackBack",
            new Vector3(BarCenterX, 1.50f, -3.90f),
            new Vector3(FrontOutlineWidth, 0.80f, 0.08f));
        BottleRackShelves = Array.AsReadOnly(
            Enumerable.Range(0, 2)
                .Select(row => new BarBoxLayout(
                    $"MergedShelf{row}",
                    new Vector3(BarCenterX, 1.34f + row * 0.34f, -3.65f),
                    new Vector3(FrontOutlineWidth, 0.04f, 0.28f)))
                .ToArray());
        LiquorBottles = Array.AsReadOnly(
            Enumerable.Range(0, 14)
                .Select(index => new BarCylinderLayout(
                    $"BackLiquor{index}",
                    new Vector3(-4.85f + index % 7 * 0.82f, 1.52f + index / 7 * 0.34f, -3.58f),
                    0.11f,
                    0.38f,
                    BottleColor(index)))
                .ToArray());

        CuttingBoard = new BarBoxLayout(
            "CuttingBoard",
            new Vector3(BarCenterX, PlayerWorktopHeight + 0.02f, FrontCounterZ - 0.08f),
            new Vector3(2.05f, 0.04f, 0.52f));
        CuttingBoardLabelPosition = new Vector3(BarCenterX, 1.35f, FrontCounterZ - 0.08f);
        OperationManual = new BarBoxLayout(
            "OperationManual",
            new Vector3(-4.82f, 1.08f, -2.62f),
            new Vector3(0.46f, 0.06f, 0.32f));
        OperationManualLabelPosition = new Vector3(-4.55f, 1.42f, -2.62f);
        ManualShelf = new BarBoxLayout(
            "ManualShelf",
            new Vector3(-4.88f, 1.105f, -2.62f),
            new Vector3(0.50f, 0.05f, 0.36f));

        Booths = Array.AsReadOnly(Array.Empty<BarBoxLayout>());
        LoungeTables = Array.AsReadOnly(new[]
        {
            new BarCylinderLayout("LoungeTable1", new Vector3(2.75f, 0.71f, -2.35f), 0.40f, 0.08f, new Color("8a5a38")),
            new BarCylinderLayout("LoungeTable2", new Vector3(3.05f, 0.71f, 0f), 0.40f, 0.08f, new Color("8a5a38")),
            new BarCylinderLayout("LoungeTable3", new Vector3(2.75f, 0.71f, 2.35f), 0.40f, 0.08f, new Color("8a5a38"))
        });
        FrontStools = Array.AsReadOnly(
            Enumerable.Range(0, 6)
                .Select(index => new BarCylinderLayout(
                    $"FrontStool{index + 1}",
                    new Vector3(BarCenterX - 1.625f + index * 0.65f, 0.78f, -0.72f),
                    0.20f,
                    0.12f,
                    new Color("8a5a38")))
                .ToArray());
        LoungeChairs = BuildLoungeChairs(LoungeTables);
        NightWindows = Array.AsReadOnly(SouthWindows
            .Select(window => new BarBoxLayout(window.Id, window.Position, window.Size))
            .ToArray());
        FrontFootrails = Array.AsReadOnly(Array.Empty<BarBoxLayout>());
        PendantFixtures = Array.AsReadOnly(new[]
        {
            new BarLightFixtureLayout("pendant_1", new Vector3(BarCenterX - 1.40f, 2.40f, FrontCounterZ), "front_pendant", true),
            new BarLightFixtureLayout("pendant_2", new Vector3(BarCenterX, 2.40f, FrontCounterZ), "front_pendant", true),
            new BarLightFixtureLayout("pendant_3", new Vector3(BarCenterX + 1.40f, 2.40f, FrontCounterZ), "front_pendant", true)
        });
        RearLinearFixtures = Array.AsReadOnly(new[]
        {
            new BarLightFixtureLayout("rear_linear_1", new Vector3(BarCenterX, 2.04f, -3.52f), "rear_linear", true),
            new BarLightFixtureLayout("rear_linear_2", new Vector3(BarCenterX, 1.64f, -3.52f), "rear_linear", true)
        });
        CustomerSconces = Array.AsReadOnly(new[]
        {
            new BarLightFixtureLayout("west_sconce_1", new Vector3(-5.88f, 2.15f, 0.15f), "customer_sconce", true),
            new BarLightFixtureLayout("west_sconce_2", new Vector3(-5.88f, 2.15f, 2.65f), "customer_sconce", true),
            new BarLightFixtureLayout("east_sconce_1", new Vector3(5.88f, 2.15f, 0.15f), "customer_sconce", true),
            new BarLightFixtureLayout("east_sconce_2", new Vector3(5.88f, 2.15f, 2.65f), "customer_sconce", true)
        });
        CustomerFillLights = Array.AsReadOnly(new[]
        {
            new BarLightFixtureLayout("customer_fill_north", new Vector3(2.6f, 3.25f, 0.0f), "customer_fill", false),
            new BarLightFixtureLayout("customer_fill_south", new Vector3(2.6f, 3.25f, 2.8f), "customer_fill", false)
        });

        Stations = Array.AsReadOnly(new[]
        {
            new BarStationLayout("customer", StationKind.Customer, new Vector3(BarCenterX, 1.05f, 0.65f), new Vector3(0.65f, 1.95f, 0.65f), "客人"),
            new BarStationLayout("coffee_beans", StationKind.CoffeeBeans, new Vector3(-4.20f, 1.18f, RearShelfZ), new Vector3(0.58f, 0.42f, 0.42f), "咖啡豆"),
            new BarStationLayout("hand_wash_sink", StationKind.HandWashSink, new Vector3(0f, 0.98f, -2.05f), new Vector3(0.40f, 0.16f, 0.50f), "每日洗手水槽"),
            new BarStationLayout("kettle", StationKind.Kettle, new Vector3(-0.20f, 1.20f, RearShelfZ), new Vector3(0.44f, 0.48f, 0.40f), "水壶｜量酒器水源"),
            new BarStationLayout("waste_bin", StationKind.WasteBin, new Vector3(0f, 0.48f, -2.92f), new Vector3(0.60f, 0.92f, 0.60f), "弃物桶")
        });
        Tools = Array.AsReadOnly(new[]
        {
            new BarToolLayout("highball_glass", new Vector3(-0.95f, 1.10f, -1.68f), new Color(0.62f, 0.82f, 0.94f, 0.62f)),
            new BarToolLayout("mortar", new Vector3(-1.75f, 1.10f, -1.68f), new Color("786859")),
            new BarToolLayout("pestle", new Vector3(-1.38f, 1.12f, -1.68f), new Color("6c5546")),
            new BarToolLayout("traditional_filter", new Vector3(-3.02f, 1.12f, -1.68f), new Color("aaa08b")),
            new BarToolLayout("bean_scoop", new Vector3(-2.68f, 1.08f, -1.68f), new Color("9a8b72")),
            new BarToolLayout("ice_tongs", new Vector3(-2.30f, 1.08f, -1.68f), new Color("8797a1")),
            new BarToolLayout("jigger_small", new Vector3(-4.05f, 1.08f, -1.68f), new Color("aab3b7")),
            new BarToolLayout("jigger_medium", new Vector3(-3.70f, 1.09f, -1.68f), new Color("909da3")),
            new BarToolLayout("jigger_large", new Vector3(-3.35f, 1.10f, -1.68f), new Color("76878f"))
        });

        FrontCounterSurface = new BarCounterSurfaceLayout(
            "front_counter_surface",
            new Vector3(BarCenterX, PlayerWorktopHeight + 0.03f, FrontCounterZ - 0.09f),
            new Vector3(FrontOutlineWidth, 0.08f, PlayerSurfaceDepth));
        RearShelfSurface = new BarCounterSurfaceLayout(
            "rear_shelf_surface",
            new Vector3(BarCenterX, RearShelfTopHeight + 0.03f, RearShelfZ),
            new Vector3(FrontOutlineWidth, 0.08f, 0.50f));
        Workboard = new BarWorkboardLayout
        {
            Position = new Vector3(BarCenterX, PlayerWorktopHeight + 0.04f, FrontCounterZ - 0.09f),
            Size = new Vector3(2.05f, 0.08f, 0.52f),
            Slots = Array.AsReadOnly(new[]
            {
                new Vector3(BarCenterX - 0.70f, 1.08f, FrontCounterZ - 0.09f),
                new Vector3(BarCenterX, 1.08f, FrontCounterZ - 0.09f),
                new Vector3(BarCenterX + 0.70f, 1.08f, FrontCounterZ - 0.09f)
            })
        };

        var cabinets = new List<BarCabinetLayout>();
        var drawerModuleCenters = new[] { -4.50f, -3.25f, -2.00f, -0.75f };
        for (var moduleIndex = 0; moduleIndex < drawerModuleCenters.Length; moduleIndex++)
        for (var layerIndex = 0; layerIndex < 2; layerIndex++)
        {
            var upper = layerIndex == 0;
            var id = $"front_drawer_{moduleIndex + 1}_{(upper ? "upper" : "lower")}";
            var center = new Vector3(drawerModuleCenters[moduleIndex], upper ? 0.72f : 0.30f, FrontBarInnerEdgeZ + 0.05f);
            cabinets.Add(new BarCabinetLayout(
                id,
                CabinetPartKind.Drawer,
                center,
                new Vector3(1.08f, 0.34f, 0.08f),
                false,
                Vector3.Forward,
                0.52f,
                DrawerOpenTravel,
                new BarBoxLayout(
                    id + "_cavity",
                    center + new Vector3(0f, 0f, 0.055f),
                    new Vector3(1.08f, 0.34f, 0.05f)),
                moduleIndex == 1 && upper));
        }

        var cabinetModuleCenters = new[] { BarCenterX - 1.87f, BarCenterX, BarCenterX + 1.87f };
        for (var moduleIndex = 0; moduleIndex < cabinetModuleCenters.Length; moduleIndex++)
        for (var leafIndex = 0; leafIndex < 2; leafIndex++)
        {
            var leftLeaf = leafIndex == 0;
            var leafCenter = cabinetModuleCenters[moduleIndex] + (leftLeaf ? -0.45f : 0.45f);
            cabinets.Add(new BarCabinetLayout(
                $"back_cabinet_{moduleIndex + 1}_{(leftLeaf ? "left" : "right")}",
                CabinetPartKind.Door,
                new Vector3(leafCenter, UpperCabinetCenterHeight, UpperCabinetFrontZ),
                new Vector3(0.90f, 0.92f, 0.06f),
                leftLeaf,
                Vector3.Back,
                0.32f,
                0f,
                null,
                false));
        }
        Cabinets = cabinets.AsReadOnly();

        IceBucket = new BarStationLayout(
            "ice_bucket",
            StationKind.IceBucket,
            new Vector3(0f, 0.1f, 0.04f),
            new Vector3(0.62f, 0.25f, 0.48f),
            "冰桶");
        PlayerFacingDirection = Vector3.Back;
    }

    public Vector3 RoomClearSize { get; }
    public BarBoxLayout Floor { get; }
    public IReadOnlyList<BarBoxLayout> Walls { get; }
    public BarBoxLayout Ceiling { get; }
    public IReadOnlyList<BarBoxLayout> WainscotSections { get; }
    public BarOpeningLayout SouthMainEntry { get; }
    public BarOpeningLayout NorthEastServiceDoor { get; }
    public IReadOnlyList<BarOpeningLayout> SouthWindows { get; }
    public BarBoxLayout FrontBarBody { get; }
    public IReadOnlyList<BarBoxLayout> FrontBarBodySections { get; }
    public IReadOnlyList<BarRotatedBoxLayout> FrontBarInnerChamfers { get; }
    public BarBoxLayout FrontBarTop { get; }
    public BarBoxLayout PlayerWorktop { get; }
    public IReadOnlyList<BarBoxLayout> PlayerWorktopSections { get; }
    public IReadOnlyList<BarRotatedBoxLayout> PlayerWorktopChamfers { get; }
    public BarBoxLayout RearWallShelf { get; }
    public BarBoxLayout UpperBackCabinet { get; }
    public IReadOnlyList<BarBoxLayout> CounterReturns { get; }
    public IReadOnlyList<BarBoxLayout> CounterReturnTops { get; }
    public BarBoxLayout WestDryReturnEnvelope { get; }
    public BarBoxLayout EastWetReturnEnvelope { get; }
    public BarBoxLayout EastWasteModule { get; }
    public BarBoxLayout EastWasteModuleTop { get; }
    public BarBoxLayout EmployeeGate { get; }
    public BarBoxLayout SinkUnderClearVolume { get; }
    public BarBoxLayout SouthEntrySwingEnvelope { get; }
    public BarBoxLayout SouthWindowAccessEnvelope { get; }
    public BarBoxLayout BottleRackBack { get; }
    public IReadOnlyList<BarBoxLayout> BottleRackShelves { get; }
    public IReadOnlyList<BarCylinderLayout> LiquorBottles { get; }
    public BarBoxLayout CuttingBoard { get; }
    public Vector3 CuttingBoardLabelPosition { get; }
    public BarBoxLayout OperationManual { get; }
    public BarBoxLayout ManualShelf { get; }
    public Vector3 OperationManualLabelPosition { get; }
    public IReadOnlyList<BarBoxLayout> Booths { get; }
    public IReadOnlyList<BarCylinderLayout> LoungeTables { get; }
    public IReadOnlyList<BarCylinderLayout> FrontStools { get; }
    public IReadOnlyList<BarChairLayout> LoungeChairs { get; }
    public IReadOnlyList<BarBoxLayout> FrontFootrails { get; }
    public IReadOnlyList<BarBoxLayout> NightWindows { get; }
    public IReadOnlyList<BarLightFixtureLayout> PendantFixtures { get; }
    public IReadOnlyList<BarLightFixtureLayout> RearLinearFixtures { get; }
    public IReadOnlyList<BarLightFixtureLayout> CustomerSconces { get; }
    public IReadOnlyList<BarLightFixtureLayout> CustomerFillLights { get; }
    public IReadOnlyList<BarStationLayout> Stations { get; }
    public IReadOnlyList<BarToolLayout> Tools { get; }
    public BarCounterSurfaceLayout FrontCounterSurface { get; }
    public BarCounterSurfaceLayout RearShelfSurface { get; }
    public BarWorkboardLayout Workboard { get; }
    public IReadOnlyList<BarCabinetLayout> Cabinets { get; }
    public BarStationLayout IceBucket { get; }
    public Vector3 PlayerFacingDirection { get; }

    public void Validate()
    {
        EnsureUnique(Stations.Select(station => station.Id), "station");
        EnsureUnique(Tools.Select(tool => tool.ToolId), "tool");
        EnsureUnique(Cabinets.Select(cabinet => cabinet.Id), "cabinet");
        EnsureUnique(SouthWindows.Select(opening => opening.Id)
            .Append(SouthMainEntry.Id)
            .Append(NorthEastServiceDoor.Id), "opening");
        EnsureUnique(LoungeChairs.Select(chair => chair.Id), "chair");
        EnsureUnique(PendantFixtures
            .Concat(RearLinearFixtures)
            .Concat(CustomerSconces)
            .Concat(CustomerFillLights)
            .Select(fixture => fixture.Id), "light fixture");
        if (!RoomClearSize.IsEqualApprox(new Vector3(RoomWidth, RoomHeight, RoomDepth)))
            throw new InvalidOperationException("Prototype room clear size must remain 12 by 9 by 3.5 metres.");
        if (Workboard.Slots.Count != 3)
            throw new InvalidOperationException("Prototype workboard must retain exactly three placement slots.");
        if (Cabinets.Count(cabinet => cabinet.Kind == CabinetPartKind.Drawer) != 8 ||
            Cabinets.Count(cabinet => cabinet.ContainsIceBucket) != 1 ||
            Cabinets.Single(cabinet => cabinet.ContainsIceBucket).Id != "front_drawer_2_upper")
            throw new InvalidOperationException("Prototype layout must contain exactly one ice-bucket cabinet.");
        if (Stations.Any(station => !HasPositiveSize(station.Size)) ||
            Cabinets.Any(cabinet => !HasPositiveSize(cabinet.Size)) ||
            SouthWindows.Any(opening => !HasPositiveSize(opening.Size)) ||
            LoungeChairs.Any(chair => !HasPositiveSize(chair.Size)))
            throw new InvalidOperationException("Station, cabinet, opening, and furniture dimensions must be positive.");
        if (SouthWindows.Count != 1 || SouthMainEntry.LeafCount != 2 ||
            !Mathf.IsEqualApprox(SouthMainEntry.Size.X, 1.40f) ||
            !Mathf.IsEqualApprox(NorthEastServiceDoor.Size.X, 0.90f))
            throw new InvalidOperationException("Prototype openings must keep one landscape window and the approved doors.");
        if (PlayerFacingDirection.Z <= 0.99f || RearBarFrontZ >= FrontBarInnerEdgeZ ||
            !Mathf.IsEqualApprox(FrontBarInnerEdgeZ - RearBarFrontZ, OperationAisleClearWidth))
            throw new InvalidOperationException("Player must face south inside the 1.40 metre U-bar aisle.");
        var westInnerEdge = WestDryReturnEnvelope.Position.X + WestDryReturnEnvelope.Size.X * 0.5f;
        var eastInnerEdge = EastWetReturnEnvelope.Position.X - EastWetReturnEnvelope.Size.X * 0.5f;
        if (!Mathf.IsEqualApprox(eastInnerEdge - westInnerEdge, InternalClearSpan))
            throw new InvalidOperationException("The asymmetric side counters must preserve the 4.15 metre clear span.");
        if (FrontStools.Count != 6 || LoungeTables.Count != 3 || LoungeChairs.Count != 12 || Booths.Count != 0)
            throw new InvalidOperationException("Prototype guest furniture counts must match the approved loose seating.");
        if (PendantFixtures.Count != 3 || RearLinearFixtures.Count != 2 ||
            CustomerSconces.Count != 4 || CustomerFillLights.Count != 2)
            throw new InvalidOperationException("Prototype light fixture groups must match the approved lighting plan.");
        if (FrontFootrails.Count != 0)
            throw new InvalidOperationException("The approved front bar has no attached footrail or fixed footboard.");
        if (FrontBarInnerChamfers.Count != 2 ||
            FrontBarInnerChamfers.Any(chamfer =>
                !Mathf.IsEqualApprox(Math.Abs(chamfer.RotationDegrees.Y), 45f)))
            throw new InvalidOperationException("The front bar must retain both 45-degree inner chamfers.");
        if (!HasPositiveSize(EmployeeGate.Size) || EmployeeGate.Position.X < -0.01f ||
            EmployeeGate.Size.Y >= PlayerWorktopHeight)
            throw new InvalidOperationException("The east employee gate must remain closed and half height.");
        if (Cabinets.Where(cabinet => cabinet.Kind == CabinetPartKind.Drawer)
            .Any(cabinet => !Mathf.IsEqualApprox(cabinet.OpenTravelDistance, DrawerOpenTravel)))
            throw new InvalidOperationException("Every front drawer must use the approved 0.38 metre travel.");

        var sink = Stations.Single(station => station.Kind == StationKind.HandWashSink);
        if (Cabinets
            .Where(cabinet => cabinet.Kind == CabinetPartKind.Drawer)
            .Any(cabinet =>
                Math.Abs(cabinet.Center.X - sink.Position.X) <
                (cabinet.Size.X + sink.Size.X) * 0.5f))
            throw new InvalidOperationException("The wash-sink bay must remain clear of front drawers.");
        if (CounterReturns.Any(body => BoxesOverlap(body, SinkUnderClearVolume)))
            throw new InvalidOperationException("The wash-sink under-counter volume must remain unobstructed.");

        var roomInnerHalfWidth = RoomWidth * 0.5f - WallThickness * 0.5f;
        var roomInnerHalfDepth = RoomDepth * 0.5f - WallThickness * 0.5f;
        var barEastOuterEdge = EastWetReturnEnvelope.Position.X + EastWetReturnEnvelope.Size.X * 0.5f;
        var pulledWestEdge = LoungeChairs.Min(chair => chair.PulledOutPosition.X - chair.Size.X * 0.5f);
        var pulledEastEdge = LoungeChairs.Max(chair => chair.PulledOutPosition.X + chair.Size.X * 0.5f);
        var pulledNorthEdge = LoungeChairs.Min(chair => chair.PulledOutPosition.Z - chair.Size.Z * 0.5f);
        var pulledSouthEdge = LoungeChairs.Max(chair => chair.PulledOutPosition.Z + chair.Size.Z * 0.5f);
        if (pulledWestEdge < barEastOuterEdge + MainCustomerRouteClearWidth ||
            pulledEastEdge > roomInnerHalfWidth - SecondaryCustomerRouteClearWidth ||
            pulledNorthEdge < -roomInnerHalfDepth + SecondaryCustomerRouteClearWidth ||
            pulledSouthEdge > roomInnerHalfDepth - SecondaryCustomerRouteClearWidth ||
            LoungeChairs.Any(chair => BoxesOverlap(
                new BarBoxLayout(chair.Id, chair.PulledOutPosition, chair.Size), SouthEntrySwingEnvelope)) ||
            LoungeChairs.Any(chair => BoxesOverlap(
                new BarBoxLayout(chair.Id, chair.PulledOutPosition, chair.Size), SouthWindowAccessEnvelope)))
            throw new InvalidOperationException(
                "Pulled-out chairs must preserve the main and secondary routes, entry swing, and window access.");
    }

    private static bool HasPositiveSize(Vector3 size) =>
        size.X > 0f && size.Y > 0f && size.Z > 0f;

    private static bool BoxesOverlap(BarBoxLayout first, BarBoxLayout second) =>
        Math.Abs(first.Position.X - second.Position.X) * 2f < first.Size.X + second.Size.X &&
        Math.Abs(first.Position.Y - second.Position.Y) * 2f < first.Size.Y + second.Size.Y &&
        Math.Abs(first.Position.Z - second.Position.Z) * 2f < first.Size.Z + second.Size.Z;

    private static void EnsureUnique(IEnumerable<string> ids, string kind)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in ids)
            if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                throw new InvalidOperationException($"Prototype {kind} IDs must be non-empty and unique: {id}");
    }

    private static IReadOnlyList<BarChairLayout> BuildLoungeChairs(
        IReadOnlyList<BarCylinderLayout> tables)
    {
        var chairs = new List<BarChairLayout>();
        var directions = new[] { Vector3.Forward, Vector3.Back, Vector3.Left, Vector3.Right };
        for (var tableIndex = 0; tableIndex < tables.Count; tableIndex++)
        for (var chairIndex = 0; chairIndex < directions.Length; chairIndex++)
        {
            var direction = directions[chairIndex];
            var position = tables[tableIndex].Position + direction * 0.68f;
            position.Y = 0.43f;
            var pulledOut = tables[tableIndex].Position + direction * 0.90f;
            pulledOut.Y = 0.43f;
            chairs.Add(new BarChairLayout(
                $"LoungeChair{tableIndex * 4 + chairIndex + 1}",
                position,
                new Vector3(0.46f, 0.86f, 0.50f),
                pulledOut));
        }
        return chairs.AsReadOnly();
    }

    private static Color BottleColor(int index) => (index % 5) switch
    {
        0 => new Color("506b37"),
        1 => new Color("8d5a32"),
        2 => new Color("305a55"),
        3 => new Color("6d3b32"),
        _ => new Color("aaa087")
    };
}
