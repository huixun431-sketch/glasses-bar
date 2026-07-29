using Godot;

namespace GlassesBar;

/// <summary>
/// Builds graybox presentation and static collision from immutable layout data.
/// It never reads or writes gameplay state.
/// </summary>
public sealed class GrayboxArchitectureBuilder
{
    private readonly BarLayoutDefinition _layout;
    private readonly Node3D _neutral;
    private readonly Node3D _reality;
    private readonly Node3D _glasses;

    public GrayboxArchitectureBuilder(
        BarLayoutDefinition layout,
        Node3D neutral,
        Node3D reality,
        Node3D glasses)
    {
        _layout = layout;
        _neutral = neutral;
        _reality = reality;
        _glasses = glasses;
    }

    public void Build()
    {
        AddStaticBox(_neutral, "FloorCollider", _layout.Floor.Position, _layout.Floor.Size);
        CreateBox(_reality, _layout.Floor, new Color("2d2424"));
        CreateBox(_glasses, _layout.Floor, new Color("071d29"), true);

        foreach (var wall in _layout.Walls)
        {
            AddStaticBox(_neutral, wall.Name + "Collider", wall.Position, wall.Size);
            CreateBox(_reality, wall, new Color("201d24"));
            CreateBox(_glasses, wall, new Color("052b3a"), true);
        }

        AddStaticBox(_neutral, "FrontBarCollider", _layout.FrontBarBody.Position, _layout.FrontBarBody.Size, 2);
        CreateBox(_reality, _layout.FrontBarBody, new Color("5b3524"));
        CreateBox(_glasses, _layout.FrontBarBody, new Color("075366"), true);
        CreateBox(_reality, _layout.FrontBarTop, new Color("8b5634"));
        CreateBox(_glasses, _layout.FrontBarTop, new Color("0f98a4"), true);

        AddStaticBox(_neutral, "RearWallShelfCollider", _layout.RearWallShelf.Position, _layout.RearWallShelf.Size, 2);
        CreateBox(_reality, _layout.RearWallShelf, new Color("76503a"));
        CreateBox(_glasses, _layout.RearWallShelf, new Color("0b8d9a"), true);

        AddStaticBox(_neutral, "UpperBackCabinetCollider", _layout.UpperBackCabinet.Position, _layout.UpperBackCabinet.Size, 2);
        CreateBox(_reality, _layout.UpperBackCabinet, new Color("3f2924"));
        CreateBox(_glasses, _layout.UpperBackCabinet, new Color("073f50"), true);

        for (var index = 0; index < _layout.CounterReturns.Count; index++)
        {
            var body = _layout.CounterReturns[index];
            var top = _layout.CounterReturnTops[index];
            AddStaticBox(_neutral, body.Name.Replace("Counter", string.Empty) + "Collider", body.Position, body.Size, 2);
            CreateBox(_reality, body, new Color("4b3027"));
            CreateBox(_glasses, body, new Color("075064"), true);
            CreateBox(_reality, top, new Color("76503a"));
            CreateBox(_glasses, top, new Color("0b8d9a"), true);
        }

        BuildMergedBackRack(_reality, false);
        BuildMergedBackRack(_glasses, true);
        BuildFrontWorktop(_reality, false);
        BuildFrontWorktop(_glasses, true);
        BuildExpandedLounge(_reality, false);
        BuildExpandedLounge(_glasses, true);
    }

    public void CreateStationVisual(BarStationLayout station, bool glasses)
    {
        var parent = glasses ? _glasses : _reality;
        var holder = new Node3D { Name = station.Id, Position = station.Position };
        parent.AddChild(holder);
        holder.AddChild(new MeshInstance3D
        {
            Name = "Visual",
            Mesh = station.Kind == StationKind.Customer
                ? new CapsuleMesh { Radius = 0.32f, Height = 1.65f }
                : station.Kind is StationKind.CoffeeBeans or StationKind.Kettle or StationKind.WasteBin
                    ? new CylinderMesh
                    {
                        TopRadius = station.Size.X * 0.36f,
                        BottomRadius = station.Size.X * 0.46f,
                        Height = station.Size.Y
                    }
                    : new BoxMesh { Size = station.Size },
            MaterialOverride = MakeMaterial(
                glasses ? new Color("2dd4bf") : RealityColor(station.Kind),
                glasses)
        });
        if (glasses)
            holder.AddChild(new Label3D
            {
                Name = "InformationLabel",
                Text = station.Label,
                Position = new Vector3(0f, station.Size.Y * 0.65f + 0.25f, 0f),
                FontSize = 30,
                OutlineSize = 8,
                PixelSize = 0.002f,
                Modulate = new Color("b8fff4"),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                NoDepthTest = true
            });
    }

    private void BuildMergedBackRack(Node3D parent, bool glasses)
    {
        CreateBox(
            parent,
            _layout.BottleRackBack,
            glasses ? new Color("074a5e") : new Color("402821"),
            glasses);
        foreach (var shelf in _layout.BottleRackShelves)
            CreateBox(
                parent,
                shelf,
                glasses ? new Color("0ba0a8") : new Color("795038"),
                glasses);
        if (glasses)
            return;
        foreach (var bottle in _layout.LiquorBottles)
            CreateCylinder(
                parent,
                bottle.Name,
                bottle.Position,
                bottle.Radius,
                bottle.Height,
                bottle.RealityColor);
    }

    private void BuildFrontWorktop(Node3D parent, bool glasses)
    {
        CreateBox(
            parent,
            _layout.CuttingBoard,
            glasses ? new Color("28d5c6") : new Color("845531"),
            glasses);
        if (!glasses)
            return;

        parent.AddChild(new Label3D
        {
            Name = "CuttingBoardLabel",
            Text = "砧板｜能力由已放置工具组合决定",
            Position = _layout.CuttingBoardLabelPosition,
            FontSize = 25,
            OutlineSize = 8,
            PixelSize = 0.002f,
            Modulate = new Color("c6fff5"),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true
        });
        CreateBox(parent, _layout.OperationManual, new Color("45f1d4"), true);
        parent.AddChild(new Label3D
        {
            Name = "OperationManualLabel",
            Text = "操作手册",
            Position = _layout.OperationManualLabelPosition,
            FontSize = 28,
            OutlineSize = 8,
            PixelSize = 0.0022f,
            Modulate = new Color("c6fff5"),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true
        });
    }

    private void BuildExpandedLounge(Node3D parent, bool glasses)
    {
        for (var index = 0; index < 2; index++)
        {
            var booth = _layout.Booths[index];
            var table = _layout.LoungeTables[index];
            CreateBox(
                parent,
                booth,
                glasses ? new Color("093f58") : new Color("4f2630"),
                glasses);
            CreateCylinder(
                parent,
                table.Name,
                table.Position,
                table.Radius,
                table.Height,
                glasses ? new Color("087a83") : table.RealityColor,
                glasses);
        }

        CreateBox(
            parent,
            _layout.Booths[2],
            glasses ? new Color("093f58") : new Color("4f2630"),
            glasses);
        var rearTable = _layout.LoungeTables[2];
        CreateCylinder(
            parent,
            rearTable.Name,
            rearTable.Position,
            rearTable.Radius,
            rearTable.Height,
            glasses ? new Color("087a83") : rearTable.RealityColor,
            glasses);
        foreach (var stool in _layout.FrontStools)
            CreateCylinder(
                parent,
                stool.Name,
                stool.Position,
                stool.Radius,
                stool.Height,
                glasses ? new Color("087a83") : stool.RealityColor,
                glasses);
        foreach (var window in _layout.NightWindows)
            CreateBox(
                parent,
                window,
                glasses ? new Color("063a52") : new Color("07172c"),
                true);
    }

    internal static void AddStaticBox(
        Node3D parent,
        string name,
        Vector3 position,
        Vector3 size,
        uint collisionLayer = 1)
    {
        var body = new StaticBody3D { Name = name, Position = position, CollisionLayer = collisionLayer };
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        parent.AddChild(body);
    }

    internal static void CreateBox(
        Node3D parent,
        BarBoxLayout layout,
        Color color,
        bool emissive = false) =>
        parent.AddChild(new MeshInstance3D
        {
            Name = layout.Name,
            Position = layout.Position,
            Mesh = new BoxMesh { Size = layout.Size },
            MaterialOverride = MakeMaterial(color, emissive)
        });

    internal static void CreateCylinder(
        Node3D parent,
        string name,
        Vector3 position,
        float radius,
        float height,
        Color color,
        bool emissive = false) =>
        parent.AddChild(new MeshInstance3D
        {
            Name = name,
            Position = position,
            Mesh = new CylinderMesh
            {
                TopRadius = radius * 0.82f,
                BottomRadius = radius,
                Height = height
            },
            MaterialOverride = MakeMaterial(color, emissive)
        });

    internal static StandardMaterial3D MakeMaterial(Color color, bool emissive = false)
    {
        var material = new StandardMaterial3D
        {
            AlbedoColor = color,
            Roughness = emissive ? 0.5f : 0.8f,
            Metallic = emissive ? 0.25f : 0.05f
        };
        if (color.A < 1f)
            material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        if (emissive)
        {
            material.EmissionEnabled = true;
            material.Emission = color * 0.45f;
        }
        return material;
    }

    private static Color RealityColor(StationKind kind) => kind switch
    {
        StationKind.Customer => new Color("705667"),
        StationKind.IceBucket => new Color("8da4b8"),
        StationKind.HandWashSink => new Color("6385a5"),
        StationKind.Kettle => new Color("8a8175"),
        StationKind.CoffeeBeans => new Color("463127"),
        StationKind.WasteBin => new Color("4b5456"),
        _ => Colors.Gray
    };
}
