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
    BarBoxLayout? Cavity,
    bool ContainsIceBucket);

/// <summary>
/// Immutable coordinates, dimensions, and stable node IDs for the current graybox.
/// It contains no scene nodes and does not own gameplay state.
/// </summary>
public sealed class BarLayoutDefinition
{
    public const float FrontBarTopHeight = 1.42f;
    public const float RearShelfTopHeight = 1.58f;
    public const float PlayerEyeHeight = 2f;
    public const float OperationAisleClearWidth = 2.31f;
    public const float BottleRackTopHeight = 3.2f;
    public const float UpperCabinetCenterHeight = 3.9f;
    public const float FrontCounterZ = 0.2f;
    public const float RearShelfZ = -2.92f;
    public const float UpperCabinetFrontZ = -2.69f;

    public static BarLayoutDefinition Prototype { get; } = new();

    private BarLayoutDefinition()
    {
        Floor = new BarBoxLayout(
            "ExpandedFloor",
            new Vector3(0f, -0.15f, 3.35f),
            new Vector3(14f, 0.3f, 13.3f));
        Walls = Array.AsReadOnly(new[]
        {
            new BarBoxLayout("BackWall", new Vector3(0f, 2.4f, -3.3f), new Vector3(14f, 5f, 0.25f)),
            new BarBoxLayout("LeftWall", new Vector3(-7f, 2.4f, 3.35f), new Vector3(0.25f, 5f, 13.3f)),
            new BarBoxLayout("RightWall", new Vector3(7f, 2.4f, 3.35f), new Vector3(0.25f, 5f, 13.3f)),
            new BarBoxLayout("FrontWall", new Vector3(0f, 2.4f, 10f), new Vector3(14f, 5f, 0.25f))
        });

        var frontBodySize = new Vector3(10.8f, 1.32f, 1f);
        FrontBarBody = new BarBoxLayout(
            "RaisedFrontBar",
            new Vector3(0f, frontBodySize.Y * 0.5f, FrontCounterZ),
            frontBodySize);
        FrontBarTop = new BarBoxLayout(
            "RaisedFrontBarTop",
            new Vector3(0f, FrontBarTopHeight - 0.07f, FrontCounterZ),
            new Vector3(11.1f, 0.14f, 1.08f));

        var rearShelfSize = new Vector3(10.6f, 0.12f, 0.44f);
        RearWallShelf = new BarBoxLayout(
            "RearWallShelf",
            new Vector3(0f, RearShelfTopHeight - rearShelfSize.Y * 0.5f, RearShelfZ),
            rearShelfSize);
        UpperBackCabinet = new BarBoxLayout(
            "UpperBackCabinet",
            new Vector3(0f, UpperCabinetCenterHeight, -2.95f),
            new Vector3(10.6f, 1.12f, 0.48f));

        CounterReturns = Array.AsReadOnly(new[]
        {
            new BarBoxLayout("RightCounterReturn", new Vector3(-5.25f, 0.66f, -0.96f), new Vector3(0.5f, 1.32f, 2.52f)),
            new BarBoxLayout("LeftCounterReturn", new Vector3(5.25f, 0.66f, -0.96f), new Vector3(0.5f, 1.32f, 2.52f))
        });
        CounterReturnTops = Array.AsReadOnly(new[]
        {
            new BarBoxLayout("RightCounterReturnTop", new Vector3(-5.25f, FrontBarTopHeight - 0.07f, -0.96f), new Vector3(0.58f, 0.14f, 2.64f)),
            new BarBoxLayout("LeftCounterReturnTop", new Vector3(5.25f, FrontBarTopHeight - 0.07f, -0.96f), new Vector3(0.58f, 0.14f, 2.64f))
        });

        BottleRackBack = new BarBoxLayout(
            "MergedBottleRackBack",
            new Vector3(0f, 2.15f, -3.13f),
            new Vector3(10.5f, 2.1f, 0.12f));
        BottleRackShelves = Array.AsReadOnly(
            Enumerable.Range(0, 3)
                .Select(row => new BarBoxLayout(
                    $"MergedShelf{row}",
                    new Vector3(0f, 1.32f + row * 0.55f, -2.96f),
                    new Vector3(10.2f, 0.09f, 0.42f)))
                .ToArray());
        LiquorBottles = Array.AsReadOnly(
            Enumerable.Range(0, 14)
                .Select(index => new BarCylinderLayout(
                    $"BackLiquor{index}",
                    new Vector3(-4.55f + index % 7 * 1.52f, 1.55f + index / 7 * 0.56f, -2.87f),
                    0.11f,
                    0.38f,
                    BottleColor(index)))
                .ToArray());

        CuttingBoard = new BarBoxLayout(
            "CuttingBoard",
            new Vector3(0.35f, 1.46f, FrontCounterZ),
            new Vector3(2.25f, 0.08f, 0.82f));
        CuttingBoardLabelPosition = new Vector3(0.35f, 1.96f, FrontCounterZ);
        OperationManual = new BarBoxLayout(
            "OperationManual",
            new Vector3(-5.25f, 1.49f, -1.04f),
            new Vector3(0.42f, 0.07f, 0.72f));
        OperationManualLabelPosition = new Vector3(-5.25f, 1.9f, -1.04f);

        Booths = Array.AsReadOnly(new[]
        {
            new BarBoxLayout("RightBooth", new Vector3(-4.4f, 0.62f, 5.1f), new Vector3(2.4f, 1.15f, 0.9f)),
            new BarBoxLayout("LeftBooth", new Vector3(4.4f, 0.62f, 5.1f), new Vector3(2.4f, 1.15f, 0.9f)),
            new BarBoxLayout("RearBooth", new Vector3(0f, 0.62f, 8.25f), new Vector3(3.2f, 1.15f, 0.9f))
        });
        LoungeTables = Array.AsReadOnly(new[]
        {
            new BarCylinderLayout("RightTable", new Vector3(-2.7f, 0.72f, 4.8f), 0.65f, 0.12f, new Color("5c3929")),
            new BarCylinderLayout("LeftTable", new Vector3(2.7f, 0.72f, 4.8f), 0.65f, 0.12f, new Color("5c3929")),
            new BarCylinderLayout("RearTable", new Vector3(0f, 0.72f, 7f), 0.72f, 0.12f, new Color("5c3929"))
        });
        FrontStools = Array.AsReadOnly(
            Enumerable.Range(0, 4)
                .Select(index => new BarCylinderLayout(
                    $"FrontStool{index}",
                    new Vector3(-3.2f + index * 2.1f, 0.94f, 1.35f),
                    0.27f,
                    0.12f,
                    new Color("5c3929")))
                .ToArray());
        NightWindows = Array.AsReadOnly(
            Enumerable.Range(0, 3)
                .Select(index => new BarBoxLayout(
                    $"NightWindow{index}",
                    new Vector3(-3.7f + index * 3.7f, 3f, 9.82f),
                    new Vector3(2.9f, 2.5f, 0.05f)))
                .ToArray());

        Stations = Array.AsReadOnly(new[]
        {
            new BarStationLayout("customer", StationKind.Customer, new Vector3(0f, 1.05f, 2.6f), new Vector3(0.65f, 1.95f, 0.65f), "客人"),
            new BarStationLayout("coffee_beans", StationKind.CoffeeBeans, new Vector3(-3.75f, 1.79f, RearShelfZ), new Vector3(0.58f, 0.42f, 0.42f), "咖啡豆"),
            new BarStationLayout("hand_wash_sink", StationKind.HandWashSink, new Vector3(4.35f, 1.5f, FrontCounterZ), new Vector3(1.45f, 0.16f, 0.62f), "每日洗手水槽"),
            new BarStationLayout("kettle", StationKind.Kettle, new Vector3(-4.75f, 1.66f, FrontCounterZ), new Vector3(0.44f, 0.48f, 0.4f), "水壶｜量酒器水源"),
            new BarStationLayout("waste_bin", StationKind.WasteBin, new Vector3(5.05f, 0.58f, -0.86f), new Vector3(0.72f, 1.1f, 0.72f), "弃物桶")
        });
        Tools = Array.AsReadOnly(new[]
        {
            new BarToolLayout("highball_glass", new Vector3(3.45f, 1.6f, FrontCounterZ), new Color(0.62f, 0.82f, 0.94f, 0.62f)),
            new BarToolLayout("mortar", new Vector3(2.05f, 1.6f, FrontCounterZ), new Color("786859")),
            new BarToolLayout("pestle", new Vector3(2.75f, 1.62f, FrontCounterZ), new Color("6c5546")),
            new BarToolLayout("traditional_filter", new Vector3(-1.25f, 1.64f, FrontCounterZ), new Color("aaa08b")),
            new BarToolLayout("bean_scoop", new Vector3(-2.05f, 1.58f, FrontCounterZ), new Color("9a8b72")),
            new BarToolLayout("ice_tongs", new Vector3(-2.7f, 1.58f, FrontCounterZ), new Color("8797a1")),
            new BarToolLayout("jigger_small", new Vector3(-3.3f, 1.58f, FrontCounterZ), new Color("aab3b7")),
            new BarToolLayout("jigger_medium", new Vector3(-3.75f, 1.59f, FrontCounterZ), new Color("909da3")),
            new BarToolLayout("jigger_large", new Vector3(-4.2f, 1.6f, FrontCounterZ), new Color("76878f"))
        });

        FrontCounterSurface = new BarCounterSurfaceLayout(
            "front_counter_surface",
            new Vector3(0f, FrontBarTopHeight + 0.03f, FrontCounterZ),
            new Vector3(10.3f, 0.08f, 0.84f));
        RearShelfSurface = new BarCounterSurfaceLayout(
            "rear_shelf_surface",
            new Vector3(0f, RearShelfTopHeight + 0.03f, RearShelfZ),
            new Vector3(10.2f, 0.08f, 0.38f));
        Workboard = new BarWorkboardLayout
        {
            Position = new Vector3(0.35f, 1.5f, FrontCounterZ),
            Size = new Vector3(2.05f, 0.14f, 0.72f),
            Slots = Array.AsReadOnly(new[]
            {
                new Vector3(-0.35f, 1.67f, FrontCounterZ),
                new Vector3(0.35f, 1.67f, FrontCounterZ),
                new Vector3(1.05f, 1.67f, FrontCounterZ)
            })
        };

        var cabinets = new List<BarCabinetLayout>();
        var drawerModuleCenters = new[] { -4f, -2f, 0f, 2f };
        for (var moduleIndex = 0; moduleIndex < drawerModuleCenters.Length; moduleIndex++)
        for (var layerIndex = 0; layerIndex < 2; layerIndex++)
        {
            var upper = layerIndex == 0;
            var id = $"front_drawer_{moduleIndex + 1}_{(upper ? "upper" : "lower")}";
            var center = new Vector3(drawerModuleCenters[moduleIndex], upper ? 1.02f : 0.57f, -0.34f);
            cabinets.Add(new BarCabinetLayout(
                id,
                CabinetPartKind.Drawer,
                center,
                new Vector3(1.72f, 0.38f, 0.1f),
                false,
                Vector3.Forward,
                0.76f,
                new BarBoxLayout(
                    id + "_cavity",
                    center + new Vector3(0f, 0f, 0.055f),
                    new Vector3(1.72f, 0.38f, 0.05f)),
                moduleIndex == 1 && upper));
        }

        var cabinetModuleCenters = new[] { -3.5f, 0f, 3.5f };
        for (var moduleIndex = 0; moduleIndex < cabinetModuleCenters.Length; moduleIndex++)
        for (var leafIndex = 0; leafIndex < 2; leafIndex++)
        {
            var leftLeaf = leafIndex == 0;
            var leafCenter = cabinetModuleCenters[moduleIndex] + (leftLeaf ? -0.76f : 0.76f);
            cabinets.Add(new BarCabinetLayout(
                $"back_cabinet_{moduleIndex + 1}_{(leftLeaf ? "left" : "right")}",
                CabinetPartKind.Door,
                new Vector3(leafCenter, UpperCabinetCenterHeight, UpperCabinetFrontZ),
                new Vector3(1.48f, 0.92f, 0.08f),
                leftLeaf,
                Vector3.Back,
                0.72f,
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
    }

    public BarBoxLayout Floor { get; }
    public IReadOnlyList<BarBoxLayout> Walls { get; }
    public BarBoxLayout FrontBarBody { get; }
    public BarBoxLayout FrontBarTop { get; }
    public BarBoxLayout RearWallShelf { get; }
    public BarBoxLayout UpperBackCabinet { get; }
    public IReadOnlyList<BarBoxLayout> CounterReturns { get; }
    public IReadOnlyList<BarBoxLayout> CounterReturnTops { get; }
    public BarBoxLayout BottleRackBack { get; }
    public IReadOnlyList<BarBoxLayout> BottleRackShelves { get; }
    public IReadOnlyList<BarCylinderLayout> LiquorBottles { get; }
    public BarBoxLayout CuttingBoard { get; }
    public Vector3 CuttingBoardLabelPosition { get; }
    public BarBoxLayout OperationManual { get; }
    public Vector3 OperationManualLabelPosition { get; }
    public IReadOnlyList<BarBoxLayout> Booths { get; }
    public IReadOnlyList<BarCylinderLayout> LoungeTables { get; }
    public IReadOnlyList<BarCylinderLayout> FrontStools { get; }
    public IReadOnlyList<BarBoxLayout> NightWindows { get; }
    public IReadOnlyList<BarStationLayout> Stations { get; }
    public IReadOnlyList<BarToolLayout> Tools { get; }
    public BarCounterSurfaceLayout FrontCounterSurface { get; }
    public BarCounterSurfaceLayout RearShelfSurface { get; }
    public BarWorkboardLayout Workboard { get; }
    public IReadOnlyList<BarCabinetLayout> Cabinets { get; }
    public BarStationLayout IceBucket { get; }

    public void Validate()
    {
        EnsureUnique(Stations.Select(station => station.Id), "station");
        EnsureUnique(Tools.Select(tool => tool.ToolId), "tool");
        EnsureUnique(Cabinets.Select(cabinet => cabinet.Id), "cabinet");
        if (Workboard.Slots.Count != 3)
            throw new InvalidOperationException("Prototype workboard must retain exactly three placement slots.");
        if (Cabinets.Count(cabinet => cabinet.ContainsIceBucket) != 1)
            throw new InvalidOperationException("Prototype layout must contain exactly one ice-bucket cabinet.");
        if (Stations.Any(station => !HasPositiveSize(station.Size)) ||
            Cabinets.Any(cabinet => !HasPositiveSize(cabinet.Size)))
            throw new InvalidOperationException("Station and cabinet dimensions must be positive.");

        var sink = Stations.Single(station => station.Kind == StationKind.HandWashSink);
        if (Cabinets
            .Where(cabinet => cabinet.Kind == CabinetPartKind.Drawer)
            .Any(cabinet =>
                Math.Abs(cabinet.Center.X - sink.Position.X) <
                (cabinet.Size.X + sink.Size.X) * 0.5f))
            throw new InvalidOperationException("The wash-sink bay must remain clear of front drawers.");
    }

    private static bool HasPositiveSize(Vector3 size) =>
        size.X > 0f && size.Y > 0f && size.Z > 0f;

    private static void EnsureUnique(IEnumerable<string> ids, string kind)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in ids)
            if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                throw new InvalidOperationException($"Prototype {kind} IDs must be non-empty and unique: {id}");
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
