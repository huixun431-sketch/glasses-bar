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

public readonly record struct BarPolygonPrismLayout(
    string Name,
    IReadOnlyList<Vector2> Footprint,
    float BottomY,
    float TopY);

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

public readonly record struct BarStorageLayout(
    string Id,
    BarCabinetLayout Front,
    Vector3 HostPosition,
    Vector3 HostSize,
    bool MovesWithFront);

public readonly record struct BarItemStorageLayout(
    string ItemId,
    string StorageId,
    Vector3 LocalPlacement);

public sealed class BarBottleRackBayLayout
{
    public required string Id { get; init; }
    public required BarBoxLayout Back { get; init; }
    public required IReadOnlyList<BarBoxLayout> Shelves { get; init; }
}

public sealed class BarWorkboardLayout
{
    public required Vector3 Position { get; init; }
    public required Vector3 Size { get; init; }
    public required IReadOnlyList<Vector3> Slots { get; init; }
}

/// <summary>
/// Immutable coordinates, dimensions, stable IDs, and storage assignments for the
/// approved Z3/H3 production graybox. It contains no scene nodes and owns no gameplay state.
/// </summary>
public sealed class BarLayoutDefinition
{
    public const float RoomWidth = 16f;
    public const float RoomDepth = 10f;
    public const float RoomHeight = 4.5f;
    public const float WallThickness = 0.20f;
    public const float FrontOutlineWidth = 9.10f;
    public const float InternalClearSpan = 7.70f;
    public const float EastWetSideDepth = 0f;
    public const float WestDrySideDepth = 0.70f;
    public const float HandoffStripWidth = 1f;
    public const int FrontFacadeBayCount = 4;
    public const float FrontSectionDepth = 1.10f;
    public const float GuestSurfaceDepth = 0.24f;
    public const float PlayerSurfaceDepth = 0.86f;
    public const float FrontBarTopHeight = 1.38f;
    public const float PlayerWorktopHeight = 1.12f;
    public const float RearShelfTopHeight = 1.12f;
    public const float PlayerEyeHeight = 1.83f;
    public const float OperationAisleClearWidth = 1.55f;
    public const float MainCustomerRouteClearWidth = 1.40f;
    public const float SecondaryCustomerRouteClearWidth = 0.90f;
    public const float DrawerOpenTravel = 0.38f;
    public const float BottleRackLowerShelfHeight = 1.50f;
    public const float BottleRackUpperShelfHeight = 2.10f;
    public const float BottleRackBackTopHeight = 2.55f;
    public const float BottleRackTopHeight = BottleRackBackTopHeight;
    public const float UpperCabinetBottomHeight = 2.65f;
    public const float UpperCabinetTopHeight = 3.95f;
    public const float UpperCabinetCenterHeight = 3.30f;
    public const float BarCenterX = -2.80f;
    public const float FrontCounterZ = -1.40f;
    public const float RearShelfZ = -3.78f;
    public const float UpperCabinetFrontZ = -3.62f;
    public const float RearBarFrontZ = -3.50f;
    public const float FrontBarInnerEdgeZ = -1.95f;
    public const float PlayerStartZ = -2.72f;
    public const float FrontBayClearWidth = 1.40f;
    public const float FrontDividerWidth = 0.06f;
    public const float FrontChamferRun = 0.35f;
    public const float FrontSinkBayWidth = 1.50f;
    public const float RearBayWidth = 1.70f;
    public const float RearEndMargin = 0.30f;

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
        WainscotSections = BuildWainscotSections();

        RoomClearSize = new Vector3(RoomWidth, RoomHeight, RoomDepth);
        SouthMainEntry = new BarOpeningLayout(
            "south_main_entry",
            new Vector3(-0.65f, 1.05f, RoomDepth * 0.5f),
            new Vector3(1.40f, 2.10f, WallThickness),
            0f,
            Vector3.Forward,
            2);
        NorthEastServiceDoor = new BarOpeningLayout(
            "north_east_service_door",
            new Vector3(6.90f, 1.05f, -RoomDepth * 0.5f),
            new Vector3(0.90f, 2.10f, WallThickness),
            0f,
            Vector3.Forward,
            1);
        SouthWindows = Array.AsReadOnly(new[]
        {
            new BarOpeningLayout(
                "south_east_window",
                new Vector3(4.35f, 1.525f, RoomDepth * 0.5f),
                new Vector3(3.20f, 1.55f, WallThickness),
                0.75f,
                Vector3.Zero,
                0)
        });

        var westEdge = BarCenterX - FrontOutlineWidth * 0.5f;
        var eastEdge = BarCenterX + FrontOutlineWidth * 0.5f;
        var playerTopFootprint = Array.AsReadOnly(new[]
        {
            new Vector2(westEdge, -1.15f),
            new Vector2(eastEdge, -1.15f),
            new Vector2(eastEdge, -1.60f),
            new Vector2(eastEdge - FrontChamferRun, FrontBarInnerEdgeZ),
            new Vector2(westEdge + FrontChamferRun, FrontBarInnerEdgeZ),
            new Vector2(westEdge, -1.60f)
        });
        var guestTopFootprint = Array.AsReadOnly(new[]
        {
            new Vector2(westEdge, -0.85f),
            new Vector2(eastEdge, -0.85f),
            new Vector2(eastEdge, -1.15f),
            new Vector2(westEdge, -1.15f)
        });
        var bodyEastEdge = eastEdge - FrontChamferRun - FrontSinkBayWidth;
        var bodyFootprint = Array.AsReadOnly(new[]
        {
            new Vector2(westEdge, -0.85f),
            new Vector2(bodyEastEdge, -0.85f),
            new Vector2(bodyEastEdge, FrontBarInnerEdgeZ),
            new Vector2(westEdge + FrontChamferRun, FrontBarInnerEdgeZ),
            new Vector2(westEdge, -1.60f),
            new Vector2(westEdge, -1.20f)
        });
        FrontBodyFootprint = new BarPolygonPrismLayout(
            "FrontBarBody", bodyFootprint, 0f, PlayerWorktopHeight - 0.04f);
        FrontPlayerTopFootprint = new BarPolygonPrismLayout(
            "PlayerWorktop", playerTopFootprint, PlayerWorktopHeight - 0.04f, PlayerWorktopHeight);
        FrontGuestRiserFootprint = new BarPolygonPrismLayout(
            "GuestCounterRiser", guestTopFootprint, PlayerWorktopHeight, FrontBarTopHeight - 0.06f);
        FrontGuestTopFootprint = new BarPolygonPrismLayout(
            "GuestCounterTop", guestTopFootprint, FrontBarTopHeight - 0.06f, FrontBarTopHeight);

        FrontBarBody = BoundsOf(FrontBodyFootprint);
        FrontBarTop = BoundsOf(FrontGuestTopFootprint);
        PlayerWorktop = BoundsOf(FrontPlayerTopFootprint);
        FrontBarBodySections = Array.AsReadOnly(new[] { FrontBarBody });
        PlayerWorktopSections = Array.AsReadOnly(new[] { PlayerWorktop });
        FrontBarInnerChamfers = Array.AsReadOnly(Array.Empty<BarRotatedBoxLayout>());
        PlayerWorktopChamfers = Array.AsReadOnly(Array.Empty<BarRotatedBoxLayout>());

        RearWallShelf = new BarBoxLayout(
            "RearBarWorktop",
            new Vector3(BarCenterX, RearShelfTopHeight - 0.03f, RearShelfZ),
            new Vector3(FrontOutlineWidth, 0.06f, 0.56f));
        UpperBackCabinet = new BarBoxLayout(
            "UpperBackCabinet",
            new Vector3(BarCenterX, UpperCabinetCenterHeight, -3.84f),
            new Vector3(FrontOutlineWidth, UpperCabinetTopHeight - UpperCabinetBottomHeight, 0.42f));

        WestDryReturnEnvelope = new BarBoxLayout(
            "WestManualReturnEnvelope",
            new Vector3(westEdge + 0.35f, PlayerWorktopHeight * 0.5f, -2.72f),
            new Vector3(WestDrySideDepth, PlayerWorktopHeight, 1.54f));
        EastWetReturnEnvelope = new BarBoxLayout(
            "EastRemovedWetReturnEnvelope",
            new Vector3(eastEdge, 0f, -2.72f),
            Vector3.Zero);
        CounterReturns = Array.AsReadOnly(new[]
        {
            new BarBoxLayout("WestManualReturn", WestDryReturnEnvelope.Position, WestDryReturnEnvelope.Size)
        });
        CounterReturnTops = Array.AsReadOnly(new[]
        {
            new BarBoxLayout("WestManualReturnTop",
                new Vector3(WestDryReturnEnvelope.Position.X, PlayerWorktopHeight - 0.02f, WestDryReturnEnvelope.Position.Z),
                new Vector3(WestDryReturnEnvelope.Size.X, 0.04f, WestDryReturnEnvelope.Size.Z))
        });
        EastWasteModule = new BarBoxLayout(
            "EastWasteModule",
            new Vector3(eastEdge - 0.35f, 0.50f, -3.42f),
            new Vector3(0.70f, 1.0f, 0.76f));
        EastWasteModuleTop = new BarBoxLayout(
            "EastWasteModuleTop",
            new Vector3(EastWasteModule.Position.X, 1.02f, EastWasteModule.Position.Z),
            new Vector3(EastWasteModule.Size.X, 0.04f, EastWasteModule.Size.Z));
        EmployeeGate = new BarBoxLayout(
            "EmployeeGate",
            new Vector3(eastEdge - 0.35f, 0.49f, -2.62f),
            new Vector3(0.08f, 0.98f, 0.72f));
        SinkUnderClearVolume = new BarBoxLayout(
            "SinkUnderClearVolume",
            new Vector3(eastEdge - FrontChamferRun - FrontSinkBayWidth * 0.5f, 0.54f, -1.40f),
            new Vector3(FrontSinkBayWidth, 1.08f, 0.88f));
        SouthEntrySwingEnvelope = new BarBoxLayout(
            "SouthEntrySwingEnvelope",
            new Vector3(SouthMainEntry.Position.X, 1.05f, 4.25f),
            new Vector3(1.40f, 2.10f, 1.30f));
        SouthWindowAccessEnvelope = new BarBoxLayout(
            "SouthWindowAccessEnvelope",
            new Vector3(SouthWindows[0].Position.X, 1.0f, 4.45f),
            new Vector3(3.20f, 2.0f, 0.90f));

        BottleRackBays = BuildBottleRackBays();
        BottleRackBack = new BarBoxLayout(
            "BottleRackBackCompatibility",
            new Vector3(BarCenterX, (RearShelfTopHeight + BottleRackBackTopHeight) * 0.5f, -4.12f),
            new Vector3(FrontOutlineWidth, BottleRackBackTopHeight - RearShelfTopHeight, 0.05f));
        BottleRackShelves = Array.AsReadOnly(BottleRackBays.SelectMany(bay => bay.Shelves).ToArray());
        LiquorBottles = Array.AsReadOnly(Array.Empty<BarCylinderLayout>());

        CuttingBoard = new BarBoxLayout(
            "CuttingBoard",
            new Vector3(BarCenterX - 0.35f, PlayerWorktopHeight + 0.02f, -1.50f),
            new Vector3(2.05f, 0.04f, 0.52f));
        CuttingBoardLabelPosition = CuttingBoard.Position + new Vector3(0f, 0.35f, 0f);
        OperationManual = new BarBoxLayout(
            "OperationManual",
            new Vector3(westEdge + 0.35f, PlayerWorktopHeight + 0.06f, -2.72f),
            new Vector3(0.46f, 0.06f, 0.32f));
        OperationManualLabelPosition = OperationManual.Position + new Vector3(0.30f, 0.34f, 0f);
        ManualShelf = new BarBoxLayout(
            "ManualShelf",
            new Vector3(westEdge + 0.35f, PlayerWorktopHeight + 0.025f, -2.72f),
            new Vector3(0.52f, 0.05f, 0.38f));

        Booths = Array.AsReadOnly(Array.Empty<BarBoxLayout>());
        LoungeTables = Array.AsReadOnly(new[]
        {
            new BarCylinderLayout("LoungeTable1", new Vector3(4.35f, 0.71f, -2.15f), 0.40f, 0.08f, new Color("8a5a38")),
            new BarCylinderLayout("LoungeTable2", new Vector3(4.65f, 0.71f, 0.25f), 0.40f, 0.08f, new Color("8a5a38")),
            new BarCylinderLayout("LoungeTable3", new Vector3(4.35f, 0.71f, 2.65f), 0.40f, 0.08f, new Color("8a5a38"))
        });
        FrontStools = Array.AsReadOnly(
            Enumerable.Range(0, 6)
                .Select(index => new BarCylinderLayout(
                    $"FrontStool{index + 1}",
                    new Vector3(BarCenterX - 2.75f + index * 1.10f, 0.78f, -0.24f),
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
            new BarLightFixtureLayout("pendant_1", new Vector3(BarCenterX - 2.6f, 2.85f, FrontCounterZ), "front_pendant", true),
            new BarLightFixtureLayout("pendant_2", new Vector3(BarCenterX, 2.85f, FrontCounterZ), "front_pendant", true),
            new BarLightFixtureLayout("pendant_3", new Vector3(BarCenterX + 2.6f, 2.85f, FrontCounterZ), "front_pendant", true)
        });
        RearLinearFixtures = Array.AsReadOnly(new[]
        {
            new BarLightFixtureLayout("rear_linear_1", new Vector3(BarCenterX - 2.25f, 2.45f, -3.58f), "rear_linear", true),
            new BarLightFixtureLayout("rear_linear_2", new Vector3(BarCenterX + 2.25f, 2.45f, -3.58f), "rear_linear", true)
        });
        CustomerSconces = Array.AsReadOnly(new[]
        {
            new BarLightFixtureLayout("west_sconce_1", new Vector3(-7.88f, 2.15f, 0.15f), "customer_sconce", true),
            new BarLightFixtureLayout("west_sconce_2", new Vector3(-7.88f, 2.15f, 2.85f), "customer_sconce", true),
            new BarLightFixtureLayout("east_sconce_1", new Vector3(7.88f, 2.15f, 0.15f), "customer_sconce", true),
            new BarLightFixtureLayout("east_sconce_2", new Vector3(7.88f, 2.15f, 2.85f), "customer_sconce", true)
        });
        CustomerFillLights = Array.AsReadOnly(new[]
        {
            new BarLightFixtureLayout("customer_fill_north", new Vector3(4.4f, 4.10f, -1.2f), "customer_fill", false),
            new BarLightFixtureLayout("customer_fill_south", new Vector3(4.4f, 4.10f, 2.4f), "customer_fill", false)
        });

        var sinkPosition = new Vector3(SinkUnderClearVolume.Position.X, PlayerWorktopHeight + 0.06f, -1.40f);
        Stations = Array.AsReadOnly(new[]
        {
            new BarStationLayout("customer", StationKind.Customer, new Vector3(BarCenterX, 1.05f, 0.80f), new Vector3(0.65f, 1.95f, 0.65f), "客人"),
            new BarStationLayout("coffee_beans", StationKind.CoffeeBeans, Vector3.Zero, new Vector3(0.58f, 0.42f, 0.42f), "咖啡豆"),
            new BarStationLayout("hand_wash_sink", StationKind.HandWashSink, sinkPosition, new Vector3(0.72f, 0.16f, 0.52f), "每日洗手水槽"),
            new BarStationLayout("kettle", StationKind.Kettle, Vector3.Zero, new Vector3(0.44f, 0.48f, 0.40f), "水壶｜量酒器水源"),
            new BarStationLayout("waste_bin", StationKind.WasteBin, new Vector3(EastWasteModule.Position.X, 0.48f, EastWasteModule.Position.Z), new Vector3(0.58f, 0.90f, 0.58f), "弃物桶")
        });
        Tools = Array.AsReadOnly(new[]
        {
            Tool("highball_glass", "62a3bd"), Tool("mortar", "786859"), Tool("pestle", "6c5546"),
            Tool("traditional_filter", "aaa08b"), Tool("bean_scoop", "9a8b72"), Tool("ice_tongs", "8797a1"),
            Tool("jigger_small", "aab3b7"), Tool("jigger_medium", "909da3"), Tool("jigger_large", "76878f")
        });

        FrontCounterSurface = new BarCounterSurfaceLayout(
            "front_counter_surface",
            new Vector3(BarCenterX, PlayerWorktopHeight + 0.03f, FrontCounterZ),
            new Vector3(FrontOutlineWidth, 0.08f, PlayerSurfaceDepth));
        RearShelfSurface = new BarCounterSurfaceLayout(
            "rear_shelf_surface",
            new Vector3(BarCenterX, RearShelfTopHeight + 0.03f, RearShelfZ),
            new Vector3(FrontOutlineWidth, 0.08f, 0.56f));
        Workboard = new BarWorkboardLayout
        {
            Position = CuttingBoard.Position + new Vector3(0f, 0.02f, 0f),
            Size = new Vector3(2.05f, 0.08f, 0.52f),
            Slots = Array.AsReadOnly(new[]
            {
                CuttingBoard.Position + new Vector3(-0.62f, 0.08f, 0f),
                CuttingBoard.Position + new Vector3(0f, 0.08f, 0f),
                CuttingBoard.Position + new Vector3(0.62f, 0.08f, 0f)
            })
        };

        Cabinets = BuildCabinets();
        Storages = Array.AsReadOnly(Cabinets.Select(BuildStorage).ToArray());
        ItemStorageAssignments = Array.AsReadOnly(new[]
        {
            new BarItemStorageLayout("traditional_filter", "front_drawer_1_upper", new Vector3(-0.38f, 0f, 0f)),
            new BarItemStorageLayout("bean_scoop", "front_drawer_1_upper", Vector3.Zero),
            new BarItemStorageLayout("ice_tongs", "front_drawer_1_upper", new Vector3(0.38f, 0f, 0f)),
            new BarItemStorageLayout("mortar", "front_drawer_1_lower", new Vector3(-0.25f, 0f, 0f)),
            new BarItemStorageLayout("pestle", "front_drawer_1_lower", new Vector3(0.30f, 0f, 0f)),
            new BarItemStorageLayout("ice_bucket", "front_drawer_2_upper", Vector3.Zero),
            new BarItemStorageLayout("jigger_small", "front_drawer_3_upper", new Vector3(-0.35f, 0f, 0f)),
            new BarItemStorageLayout("jigger_medium", "front_drawer_3_upper", Vector3.Zero),
            new BarItemStorageLayout("jigger_large", "front_drawer_3_upper", new Vector3(0.35f, 0f, 0f)),
            new BarItemStorageLayout("highball_glass", "front_drawer_3_lower", Vector3.Zero),
            new BarItemStorageLayout("coffee_beans", "rear_lower_cabinet_1", Vector3.Zero),
            new BarItemStorageLayout("kettle", "rear_lower_cabinet_2", Vector3.Zero)
        });

        IceBucket = new BarStationLayout(
            "ice_bucket",
            StationKind.IceBucket,
            Vector3.Zero,
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
    public BarPolygonPrismLayout FrontBodyFootprint { get; }
    public BarPolygonPrismLayout FrontPlayerTopFootprint { get; }
    public BarPolygonPrismLayout FrontGuestRiserFootprint { get; }
    public BarPolygonPrismLayout FrontGuestTopFootprint { get; }
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
    public IReadOnlyList<BarBottleRackBayLayout> BottleRackBays { get; }
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
    public IReadOnlyList<BarStorageLayout> Storages { get; }
    public IReadOnlyList<BarItemStorageLayout> ItemStorageAssignments { get; }
    public BarStationLayout IceBucket { get; }
    public Vector3 PlayerFacingDirection { get; }

    public void Validate()
    {
        EnsureUnique(Stations.Select(station => station.Id), "station");
        EnsureUnique(Tools.Select(tool => tool.ToolId), "tool");
        EnsureUnique(Cabinets.Select(cabinet => cabinet.Id), "cabinet");
        EnsureUnique(Storages.Select(storage => storage.Id), "storage");
        EnsureUnique(ItemStorageAssignments.Select(item => item.ItemId), "stored item");
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
            throw new InvalidOperationException("Prototype room clear size must remain 16 by 10 by 4.5 metres.");
        var frontRecalculation = FrontChamferRun * 2f + FrontBayClearWidth * 4f +
                                 FrontDividerWidth * 5f + HandoffStripWidth + FrontSinkBayWidth;
        if (!Mathf.IsEqualApprox(frontRecalculation, FrontOutlineWidth))
            throw new InvalidOperationException($"Front modules must recalculate to 9.10 metres, got {frontRecalculation:0.###}.");
        if (!Mathf.IsEqualApprox(RearBayWidth * 5f + RearEndMargin * 2f, FrontOutlineWidth))
            throw new InvalidOperationException("Rear five-bay width and margins must recalculate to 9.10 metres.");
        if (FrontBodyFootprint.Footprint.Count < 3 ||
            FrontPlayerTopFootprint.Footprint.Count < 3 ||
            FrontGuestRiserFootprint.Footprint.Count < 3 ||
            FrontGuestTopFootprint.Footprint.Count < 3)
            throw new InvalidOperationException("Every front-bar polygon must have at least three points.");
        if (FrontBarInnerChamfers.Count != 0 || PlayerWorktopChamfers.Count != 0 || LiquorBottles.Count != 0)
            throw new InvalidOperationException("Obsolete overlay chamfers and placeholder bottles must remain absent.");
        if (BottleRackBays.Count != 5 || BottleRackBays.Any(bay => bay.Shelves.Count != 2))
            throw new InvalidOperationException("Bottle rack must contain five aligned two-level empty bays.");
        if (Workboard.Slots.Count != 3)
            throw new InvalidOperationException("Prototype workboard must retain exactly three placement slots.");
        if (Cabinets.Count(cabinet => cabinet.Kind == CabinetPartKind.Drawer) != 8 ||
            Cabinets.Count(cabinet => cabinet.ContainsIceBucket) != 1 ||
            Cabinets.Single(cabinet => cabinet.ContainsIceBucket).Id != "front_drawer_2_upper")
            throw new InvalidOperationException("Front storage must retain eight drawers and the stable ice drawer.");
        if (Cabinets.Count(cabinet => cabinet.Id.StartsWith("rear_lower_cabinet_", StringComparison.Ordinal)) != 5 ||
            Cabinets.Count(cabinet => cabinet.Id.StartsWith("back_cabinet_", StringComparison.Ordinal)) != 10)
            throw new InvalidOperationException("Rear storage must contain five lower fronts and ten upper leaves.");
        if (ItemStorageAssignments.Any(item => Storages.All(storage => storage.Id != item.StorageId)))
            throw new InvalidOperationException("Every stored item must resolve to one storage host.");
        if (Stations.Any(station => !HasPositiveSize(station.Size)) ||
            Cabinets.Any(cabinet => !HasPositiveSize(cabinet.Size)) ||
            Storages.Any(storage => !HasPositiveSize(storage.HostSize)) ||
            SouthWindows.Any(opening => !HasPositiveSize(opening.Size)) ||
            LoungeChairs.Any(chair => !HasPositiveSize(chair.Size)))
            throw new InvalidOperationException("Station, cabinet, storage, opening, and furniture dimensions must be positive.");
        if (SouthWindows.Count != 1 || SouthMainEntry.LeafCount != 2 ||
            !Mathf.IsEqualApprox(SouthMainEntry.Size.X, 1.40f) ||
            !Mathf.IsEqualApprox(NorthEastServiceDoor.Size.X, 0.90f))
            throw new InvalidOperationException("Prototype openings must keep one landscape window and the approved doors.");
        if (PlayerFacingDirection.Z <= 0.99f || RearBarFrontZ >= FrontBarInnerEdgeZ ||
            !Mathf.IsEqualApprox(FrontBarInnerEdgeZ - RearBarFrontZ, OperationAisleClearWidth))
            throw new InvalidOperationException("Player must face south inside the 1.55 metre bar aisle.");
        if (FrontStools.Count != 6 || LoungeTables.Count != 3 || LoungeChairs.Count != 12 || Booths.Count != 0)
            throw new InvalidOperationException("Prototype guest furniture counts must match the approved loose seating.");
        if (PendantFixtures.Count != 3 || RearLinearFixtures.Count != 2 ||
            CustomerSconces.Count != 4 || CustomerFillLights.Count != 2)
            throw new InvalidOperationException("Prototype light fixture groups must match the approved lighting plan.");
        if (FrontFootrails.Count != 0)
            throw new InvalidOperationException("The approved front bar has no attached footrail or fixed footboard.");
        if (!HasPositiveSize(EmployeeGate.Size) || EmployeeGate.Size.Y >= PlayerWorktopHeight)
            throw new InvalidOperationException("The east employee gate must remain half height.");
        if (Cabinets.Where(cabinet => cabinet.Kind == CabinetPartKind.Drawer)
            .Any(cabinet => !Mathf.IsEqualApprox(cabinet.OpenTravelDistance, DrawerOpenTravel)))
            throw new InvalidOperationException("Every front drawer must use the approved 0.38 metre travel.");
        if (Cabinets.Any(cabinet => BoxesOverlap(
                new BarBoxLayout(cabinet.Id, cabinet.Center, cabinet.Size), SinkUnderClearVolume)))
            throw new InvalidOperationException("The sink under-counter volume must remain free of cabinetry.");

        var barEastEdge = BarCenterX + FrontOutlineWidth * 0.5f;
        var pulledWestEdge = LoungeChairs.Min(chair => chair.PulledOutPosition.X - chair.Size.X * 0.5f);
        if (pulledWestEdge < barEastEdge + MainCustomerRouteClearWidth ||
            LoungeChairs.Any(chair => BoxesOverlap(
                new BarBoxLayout(chair.Id, chair.PulledOutPosition, chair.Size), SouthEntrySwingEnvelope)) ||
            LoungeChairs.Any(chair => BoxesOverlap(
                new BarBoxLayout(chair.Id, chair.PulledOutPosition, chair.Size), SouthWindowAccessEnvelope)))
            throw new InvalidOperationException("Pulled-out chairs must preserve the main route, entry swing, and window access.");
    }

    private static IReadOnlyList<BarBoxLayout> BuildWainscotSections()
    {
        var innerHalfWidth = RoomWidth * 0.5f - 0.11f;
        var innerHalfDepth = RoomDepth * 0.5f - 0.11f;
        return Array.AsReadOnly(new[]
        {
            new BarBoxLayout("WestWainscot", new Vector3(-innerHalfWidth, 0.525f, 0f), new Vector3(0.02f, 1.05f, RoomDepth)),
            new BarBoxLayout("EastWainscot", new Vector3(innerHalfWidth, 0.525f, 0f), new Vector3(0.02f, 1.05f, RoomDepth)),
            new BarBoxLayout("NorthWestWainscot", new Vector3(-0.55f, 0.525f, -innerHalfDepth), new Vector3(13.7f, 1.05f, 0.02f)),
            new BarBoxLayout("NorthEastWainscot", new Vector3(7.55f, 0.525f, -innerHalfDepth), new Vector3(0.70f, 1.05f, 0.02f)),
            new BarBoxLayout("SouthWestWainscot", new Vector3(-4.33f, 0.525f, innerHalfDepth), new Vector3(7.95f, 1.05f, 0.02f)),
            new BarBoxLayout("SouthCenterWainscot", new Vector3(1.75f, 0.525f, innerHalfDepth), new Vector3(2.90f, 1.05f, 0.02f)),
            new BarBoxLayout("SouthEastWainscot", new Vector3(7.18f, 0.525f, innerHalfDepth), new Vector3(1.42f, 1.05f, 0.02f)),
            new BarBoxLayout("SouthWindowSillWainscot", new Vector3(4.35f, 0.375f, innerHalfDepth), new Vector3(3.20f, 0.75f, 0.02f))
        });
    }

    private static IReadOnlyList<BarBottleRackBayLayout> BuildBottleRackBays()
    {
        var bays = new List<BarBottleRackBayLayout>();
        var westEdge = BarCenterX - FrontOutlineWidth * 0.5f + RearEndMargin;
        for (var index = 0; index < 5; index++)
        {
            var centerX = westEdge + RearBayWidth * (index + 0.5f);
            bays.Add(new BarBottleRackBayLayout
            {
                Id = $"bottle_rack_bay_{index + 1}",
                Back = new BarBoxLayout(
                    $"BottleRackBay{index + 1}Back",
                    new Vector3(centerX, (RearShelfTopHeight + BottleRackBackTopHeight) * 0.5f, -4.10f),
                    new Vector3(RearBayWidth - 0.08f, BottleRackBackTopHeight - RearShelfTopHeight, 0.05f)),
                Shelves = Array.AsReadOnly(new[]
                {
                    new BarBoxLayout($"BottleRackBay{index + 1}LowerShelf",
                        new Vector3(centerX, BottleRackLowerShelfHeight - 0.02f, -3.84f),
                        new Vector3(RearBayWidth - 0.08f, 0.04f, 0.48f)),
                    new BarBoxLayout($"BottleRackBay{index + 1}UpperShelf",
                        new Vector3(centerX, BottleRackUpperShelfHeight - 0.02f, -3.84f),
                        new Vector3(RearBayWidth - 0.08f, 0.04f, 0.48f))
                })
            });
        }
        return bays.AsReadOnly();
    }

    private static IReadOnlyList<BarCabinetLayout> BuildCabinets()
    {
        var cabinets = new List<BarCabinetLayout>();
        var westEdge = BarCenterX - FrontOutlineWidth * 0.5f + FrontChamferRun;
        var cursor = westEdge + FrontDividerWidth;
        for (var bay = 0; bay < 4; bay++)
        {
            var centerX = cursor + FrontBayClearWidth * 0.5f;
            for (var level = 0; level < 2; level++)
            {
                var upper = level == 0;
                var id = $"front_drawer_{bay + 1}_{(upper ? "upper" : "lower")}";
                var center = new Vector3(centerX, upper ? 0.83f : 0.39f, -1.91f);
                cabinets.Add(new BarCabinetLayout(
                    id,
                    CabinetPartKind.Drawer,
                    center,
                    new Vector3(1.30f, 0.36f, 0.08f),
                    false,
                    Vector3.Forward,
                    0.62f,
                    DrawerOpenTravel,
                    new BarBoxLayout(id + "_cavity", center + new Vector3(0f, 0f, 0.32f), new Vector3(1.30f, 0.36f, 0.62f)),
                    bay == 1 && upper));
            }
            cursor += FrontBayClearWidth + FrontDividerWidth;
        }

        var rearWest = BarCenterX - FrontOutlineWidth * 0.5f + RearEndMargin;
        for (var bay = 0; bay < 5; bay++)
        {
            var centerX = rearWest + RearBayWidth * (bay + 0.5f);
            cabinets.Add(new BarCabinetLayout(
                $"rear_lower_cabinet_{bay + 1}",
                CabinetPartKind.Door,
                new Vector3(centerX, 0.52f, RearBarFrontZ),
                new Vector3(RearBayWidth - 0.12f, 0.96f, 0.06f),
                bay % 2 == 0,
                Vector3.Back,
                0.52f,
                0f,
                null,
                false));
            for (var leaf = 0; leaf < 2; leaf++)
            {
                var left = leaf == 0;
                cabinets.Add(new BarCabinetLayout(
                    $"back_cabinet_{bay + 1}_{(left ? "left" : "right")}",
                    CabinetPartKind.Door,
                    new Vector3(centerX + (left ? -0.405f : 0.405f), UpperCabinetCenterHeight, UpperCabinetFrontZ),
                    new Vector3(0.81f, 1.22f, 0.06f),
                    left,
                    Vector3.Back,
                    0.34f,
                    0f,
                    null,
                    false));
            }
        }
        return cabinets.AsReadOnly();
    }

    private static BarStorageLayout BuildStorage(BarCabinetLayout front)
    {
        var inward = -front.OutwardDirection.Normalized();
        var hostPosition = front.Center + inward * (front.StorageDepth * 0.52f);
        return new BarStorageLayout(
            front.Id,
            front,
            hostPosition,
            new Vector3(front.Size.X * 0.86f, Math.Max(0.20f, front.Size.Y * 0.72f), front.StorageDepth * 0.84f),
            front.Kind == CabinetPartKind.Drawer);
    }

    private static BarToolLayout Tool(string id, string color) =>
        new(id, Vector3.Zero, new Color(color));

    private static BarBoxLayout BoundsOf(BarPolygonPrismLayout prism)
    {
        var minX = prism.Footprint.Min(point => point.X);
        var maxX = prism.Footprint.Max(point => point.X);
        var minZ = prism.Footprint.Min(point => point.Y);
        var maxZ = prism.Footprint.Max(point => point.Y);
        return new BarBoxLayout(
            prism.Name,
            new Vector3((minX + maxX) * 0.5f, (prism.BottomY + prism.TopY) * 0.5f, (minZ + maxZ) * 0.5f),
            new Vector3(maxX - minX, prism.TopY - prism.BottomY, maxZ - minZ));
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

    private static IReadOnlyList<BarChairLayout> BuildLoungeChairs(IReadOnlyList<BarCylinderLayout> tables)
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
}
