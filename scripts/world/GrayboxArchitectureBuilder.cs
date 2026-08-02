using System.Collections.Generic;
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
        BuildCollisions();
        BuildGrayboxVisuals();
    }

    public void BuildCollisions()
    {
        AddStaticBox(_neutral, "FloorCollider", _layout.Floor.Position, _layout.Floor.Size);
        AddStaticBox(_neutral, "CeilingCollider", _layout.Ceiling.Position, _layout.Ceiling.Size);
        foreach (var wall in BuildWallSegments())
            AddStaticBox(_neutral, wall.Name + "Collider", wall.Position, wall.Size);
        BarPolygonGeometry.CreateCollisionBody(
            _neutral, "FrontBarBodyCollider", _layout.FrontBodyFootprint, 2);
        BarPolygonGeometry.CreateCollisionBody(
            _neutral, "PlayerWorktopCollider", _layout.FrontPlayerTopFootprint, 2);
        BarPolygonGeometry.CreateCollisionBody(
            _neutral, "GuestCounterRiserCollider", _layout.FrontGuestRiserFootprint, 2);
        BarPolygonGeometry.CreateCollisionBody(
            _neutral, "GuestCounterTopCollider", _layout.FrontGuestTopFootprint, 2);
        AddStaticBox(_neutral, "RearWallShelfCollider", _layout.RearWallShelf.Position, _layout.RearWallShelf.Size, 2);
        for (var index = 0; index < _layout.CounterReturns.Count; index++)
        {
            var body = _layout.CounterReturns[index];
            AddStaticBox(_neutral, body.Name.Replace("Counter", string.Empty) + "Collider", body.Position, body.Size, 2);
        }
        AddStaticBox(_neutral, "EastWasteModuleCollider", _layout.EastWasteModule.Position, _layout.EastWasteModule.Size, 2);

        AddDoorLeafCollisions();
        BuildAuthoritativeSharedParts();
    }

    public void BuildGrayboxVisuals()
    {
        CreateBox(_reality, _layout.Floor, new Color("2d2424"));
        CreateBox(_glasses, _layout.Floor, new Color("071d29"), true);
        CreateBox(_reality, _layout.Ceiling, new Color("4b4640"));
        CreateBox(_glasses, _layout.Ceiling, new Color("07303a"), true);
        foreach (var wall in BuildWallSegments())
        {
            CreateBox(_reality, wall, new Color("201d24"));
            CreateBox(_glasses, wall, new Color("052b3a"), true);
        }
        BuildOpeningVisuals(_reality, false);
        BuildOpeningVisuals(_glasses, true);
        BuildWainscot(_reality, false);
        BuildWainscot(_glasses, true);

        BarPolygonGeometry.CreateVisual(
            _reality, _layout.FrontBodyFootprint, MakeMaterial(new Color("5b3524")));
        BarPolygonGeometry.CreateVisual(
            _glasses, _layout.FrontBodyFootprint, MakeMaterial(new Color("075366"), true));
        BarPolygonGeometry.CreateVisual(
            _reality, _layout.FrontPlayerTopFootprint, MakeMaterial(new Color("76503a")));
        BarPolygonGeometry.CreateVisual(
            _glasses, _layout.FrontPlayerTopFootprint, MakeMaterial(new Color("0b8d9a"), true));
        BarPolygonGeometry.CreateVisual(
            _reality, _layout.FrontGuestRiserFootprint, MakeMaterial(new Color("5b3524")));
        BarPolygonGeometry.CreateVisual(
            _glasses, _layout.FrontGuestRiserFootprint, MakeMaterial(new Color("075366"), true));
        BarPolygonGeometry.CreateVisual(
            _reality, _layout.FrontGuestTopFootprint, MakeMaterial(new Color("8b5634")));
        BarPolygonGeometry.CreateVisual(
            _glasses, _layout.FrontGuestTopFootprint, MakeMaterial(new Color("0f98a4"), true));

        CreateBox(_reality, _layout.RearWallShelf, new Color("76503a"));
        CreateBox(_glasses, _layout.RearWallShelf, new Color("0b8d9a"), true);
        BuildUpperCabinetShells(_reality, false);
        BuildUpperCabinetShells(_glasses, true);

        foreach (var body in _layout.CounterReturns)
        {
            CreateBox(_reality, body, new Color("4b3027"));
            CreateBox(_glasses, body, new Color("075064"), true);
        }
        foreach (var top in _layout.CounterReturnTops)
        {
            CreateBox(_reality, top, new Color("76503a"));
            CreateBox(_glasses, top, new Color("0b8d9a"), true);
        }
        CreateBox(_reality, _layout.EastWasteModule, new Color("4b3027"));
        CreateBox(_glasses, _layout.EastWasteModule, new Color("075064"), true);
        CreateBox(_reality, _layout.EastWasteModuleTop, new Color("76503a"));
        CreateBox(_glasses, _layout.EastWasteModuleTop, new Color("0b8d9a"), true);

        BuildBottleRackBays(_reality, false);
        BuildBottleRackBays(_glasses, true);
        BuildFrontWorktop(_reality, false);
        BuildFrontWorktop(_glasses, true);
        BuildCounterDetails(_reality, false);
        BuildCounterDetails(_glasses, true);
        BuildExpandedLounge(_reality, false);
        BuildExpandedLounge(_glasses, true);
        BuildLightRig(_reality, false);
        BuildLightRig(_glasses, true);
    }

    private void BuildAuthoritativeSharedParts()
    {
        var gate = new StaticBody3D
        {
            Name = _layout.EmployeeGate.Name,
            Position = _layout.EmployeeGate.Position,
            CollisionLayer = 2
        };
        gate.AddChild(new MeshInstance3D
        {
            Name = "Panel",
            Mesh = new BoxMesh { Size = _layout.EmployeeGate.Size },
            MaterialOverride = MakeMaterial(new Color("60432f"))
        });
        gate.AddChild(new CollisionShape3D
        {
            Name = "CollisionShape3D",
            Shape = new BoxShape3D { Size = _layout.EmployeeGate.Size }
        });
        gate.AddChild(new Marker3D
        {
            Name = "Hinge",
            Position = new Vector3(0f, 0f, -_layout.EmployeeGate.Size.Z * 0.5f)
        });
        _neutral.AddChild(gate);

        var manual = new StaticBody3D
        {
            Name = _layout.OperationManual.Name,
            Position = _layout.OperationManual.Position,
            RotationDegrees = new Vector3(0f, 0f, -20f),
            CollisionLayer = 1
        };
        manual.AddChild(new MeshInstance3D
        {
            Name = "Cover",
            Mesh = new BoxMesh { Size = _layout.OperationManual.Size },
            MaterialOverride = MakeMaterial(new Color("8a4b34"))
        });
        manual.AddChild(new CollisionShape3D
        {
            Name = "CollisionShape3D",
            Shape = new BoxShape3D { Size = _layout.OperationManual.Size }
        });
        manual.AddChild(new Marker3D { Name = "Grip", Position = new Vector3(0.12f, 0.05f, 0f) });
        manual.AddChild(new Marker3D { Name = "Placement" });
        manual.AddChild(new Marker3D { Name = "CoverPivot", Position = new Vector3(-0.23f, 0f, 0f) });
        manual.AddChild(new Marker3D { Name = "PagePivotLeft", Position = new Vector3(-0.02f, 0.035f, 0f) });
        manual.AddChild(new Marker3D { Name = "PagePivotRight", Position = new Vector3(0.02f, 0.035f, 0f) });
        _neutral.AddChild(manual);
    }

    private IReadOnlyList<BarBoxLayout> BuildWallSegments()
    {
        var halfWidth = BarLayoutDefinition.RoomWidth * 0.5f;
        var halfDepth = BarLayoutDefinition.RoomDepth * 0.5f;
        var thickness = BarLayoutDefinition.WallThickness;
        var height = BarLayoutDefinition.RoomHeight;
        return new[]
        {
            new BarBoxLayout("WestWall", new Vector3(-halfWidth, height * 0.5f, 0f), new Vector3(thickness, height, BarLayoutDefinition.RoomDepth)),
            new BarBoxLayout("EastWall", new Vector3(halfWidth, height * 0.5f, 0f), new Vector3(thickness, height, BarLayoutDefinition.RoomDepth)),

            new BarBoxLayout("NorthWallWest", new Vector3(-0.675f, height * 0.5f, -halfDepth), new Vector3(10.65f, height, thickness)),
            new BarBoxLayout("NorthWallEast", new Vector3(5.775f, height * 0.5f, -halfDepth), new Vector3(0.45f, height, thickness)),
            new BarBoxLayout("NorthServiceHeader", new Vector3(5.10f, 2.80f, -halfDepth), new Vector3(0.90f, 1.40f, thickness)),

            new BarBoxLayout("SouthWallWest", new Vector3(-3.85f, height * 0.5f, halfDepth), new Vector3(4.30f, height, thickness)),
            new BarBoxLayout("SouthWallCenter", new Vector3(0.65f, height * 0.5f, halfDepth), new Vector3(1.90f, height, thickness)),
            new BarBoxLayout("SouthWallEast", new Vector3(5.40f, height * 0.5f, halfDepth), new Vector3(1.20f, height, thickness)),
            new BarBoxLayout("SouthEntryHeader", new Vector3(-1f, 2.80f, halfDepth), new Vector3(1.40f, 1.40f, thickness)),
            new BarBoxLayout("SouthWindowSill", new Vector3(3.20f, 0.375f, halfDepth), new Vector3(3.20f, 0.75f, thickness)),
            new BarBoxLayout("SouthWindowHeader", new Vector3(3.20f, 2.90f, halfDepth), new Vector3(3.20f, 1.20f, thickness))
        };
    }

    private void BuildWainscot(Node3D parent, bool glasses)
    {
        var group = new Node3D { Name = "Wainscot" };
        parent.AddChild(group);
        foreach (var section in _layout.WainscotSections)
            CreateBox(group, section,
                glasses ? new Color("06465a") : new Color("4f2420"), glasses);
    }

    private void AddDoorLeafCollisions()
    {
        var entry = _layout.SouthMainEntry;
        var leafWidth = entry.Size.X * 0.5f;
        AddStaticBox(_neutral, "SouthMainDoorLeftCollider",
            entry.Position + new Vector3(-leafWidth * 0.5f, 0f, -0.06f),
            new Vector3(leafWidth, entry.Size.Y, 0.08f));
        AddStaticBox(_neutral, "SouthMainDoorRightCollider",
            entry.Position + new Vector3(leafWidth * 0.5f, 0f, -0.06f),
            new Vector3(leafWidth, entry.Size.Y, 0.08f));
        var service = _layout.NorthEastServiceDoor;
        AddStaticBox(_neutral, "NorthEastServiceDoorCollider",
            service.Position + new Vector3(0f, 0f, 0.06f),
            new Vector3(service.Size.X, service.Size.Y, 0.08f));
    }

    private void BuildOpeningVisuals(Node3D parent, bool glasses)
    {
        var windowGroup = new Node3D { Name = "SouthWindows" };
        parent.AddChild(windowGroup);
        foreach (var window in _layout.SouthWindows)
            CreateBox(windowGroup,
                new BarBoxLayout(window.Id, window.Position, window.Size),
                glasses ? new Color("126078") : new Color("10233b"), true);

        var doorGroup = new Node3D { Name = "Doors" };
        parent.AddChild(doorGroup);
        var entry = _layout.SouthMainEntry;
        var leafWidth = entry.Size.X * 0.5f;
        CreateBox(doorGroup,
            new BarBoxLayout("SouthMainDoorLeft", entry.Position + new Vector3(-leafWidth * 0.5f, 0f, -0.06f), new Vector3(leafWidth, entry.Size.Y, 0.08f)),
            glasses ? new Color("075064") : new Color("5a3829"), glasses);
        CreateBox(doorGroup,
            new BarBoxLayout("SouthMainDoorRight", entry.Position + new Vector3(leafWidth * 0.5f, 0f, -0.06f), new Vector3(leafWidth, entry.Size.Y, 0.08f)),
            glasses ? new Color("075064") : new Color("5a3829"), glasses);
        var handleColor = glasses ? new Color("3de1d4") : new Color("c79b58");
        CreateBox(doorGroup,
            new BarBoxLayout("SouthMainDoorLeftHandle", entry.Position + new Vector3(-0.10f, 0f, -0.12f), new Vector3(0.035f, 0.28f, 0.04f)),
            handleColor, glasses);
        CreateBox(doorGroup,
            new BarBoxLayout("SouthMainDoorRightHandle", entry.Position + new Vector3(0.10f, 0f, -0.12f), new Vector3(0.035f, 0.28f, 0.04f)),
            handleColor, glasses);
        var service = _layout.NorthEastServiceDoor;
        CreateBox(doorGroup,
            new BarBoxLayout("NorthEastServiceDoor", service.Position + new Vector3(0f, 0f, 0.06f), new Vector3(service.Size.X, service.Size.Y, 0.08f)),
            glasses ? new Color("075064") : new Color("4b3027"), glasses);
        CreateBox(doorGroup,
            new BarBoxLayout("NorthEastServiceDoorHandle", service.Position + new Vector3(-0.28f, 0f, 0.12f), new Vector3(0.035f, 0.24f, 0.04f)),
            handleColor, glasses);
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

    private void BuildBottleRackBays(Node3D parent, bool glasses)
    {
        foreach (var bay in _layout.BottleRackBays)
        {
            CreateBox(
                parent,
                bay.Back,
                glasses ? new Color("074a5e") : new Color("402821"),
                glasses);
            foreach (var shelf in bay.Shelves)
                CreateBox(
                    parent,
                    shelf,
                    glasses ? new Color("0ba0a8") : new Color("795038"),
                    glasses);
        }
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

    private void BuildCounterDetails(Node3D parent, bool glasses)
    {
        var dark = glasses ? new Color("052c38") : new Color("211817");
        var trim = glasses ? new Color("20aeb4") : new Color("9a6843");
        var metal = glasses ? new Color("2bbac1") : new Color("72777b");

        var facade = new Node3D { Name = "FrontFacadeDetails" };
        parent.AddChild(facade);
        CreateBox(facade, new BarBoxLayout("HandoffStrip",
            new Vector3(-0.60f, BarLayoutDefinition.FrontBarTopHeight + 0.006f, -1.00f),
            new Vector3(BarLayoutDefinition.HandoffStripWidth, 0.012f, 0.20f)), trim, glasses);

        var workMarks = new Node3D { Name = "WorkboardParkingMarks" };
        parent.AddChild(workMarks);
        for (var index = 0; index < _layout.Workboard.Slots.Count; index++)
            CreateBox(workMarks, new BarBoxLayout($"ParkingMark{index + 1}",
                _layout.Workboard.Slots[index] + new Vector3(0f, -0.075f, 0f),
                new Vector3(0.44f, 0.003f, 0.28f)), trim, glasses);

        var sink = _layout.Stations[2];
        var sinkFixtures = new Node3D { Name = "FrontEastSinkFixtures" };
        parent.AddChild(sinkFixtures);
        CreateBox(sinkFixtures, new BarBoxLayout("SinkOpening",
            new Vector3(sink.Position.X, BarLayoutDefinition.PlayerWorktopHeight + 0.002f, sink.Position.Z),
            new Vector3(0.76f, 0.012f, 0.56f)), dark, glasses);
        CreateBox(sinkFixtures, new BarBoxLayout("SinkBowl",
            new Vector3(sink.Position.X, BarLayoutDefinition.PlayerWorktopHeight - 0.13f, sink.Position.Z),
            new Vector3(0.70f, 0.24f, 0.50f)), metal, glasses);
        CreateBox(sinkFixtures, new BarBoxLayout("FaucetPost",
            new Vector3(sink.Position.X + 0.32f, BarLayoutDefinition.PlayerWorktopHeight + 0.18f, sink.Position.Z - 0.18f),
            new Vector3(0.055f, 0.36f, 0.055f)), metal, glasses);
        CreateBox(sinkFixtures, new BarBoxLayout("FaucetReach",
            new Vector3(sink.Position.X + 0.20f, BarLayoutDefinition.PlayerWorktopHeight + 0.34f, sink.Position.Z - 0.07f),
            new Vector3(0.24f, 0.045f, 0.22f)), metal, glasses);

        var dry = new Node3D { Name = "WestManualShelf" };
        parent.AddChild(dry);
        CreateRotatedBox(dry, _layout.ManualShelf, new Vector3(0f, 0f, -20f), trim, glasses);
        CreateRotatedBox(dry, new BarBoxLayout("ManualNorthStop",
            _layout.ManualShelf.Position + new Vector3(0f, 0.035f, -0.145f), new Vector3(0.07f, 0.025f, 0.07f)),
            new Vector3(0f, 0f, -20f), trim, glasses);
        CreateRotatedBox(dry, new BarBoxLayout("ManualSouthStop",
            _layout.ManualShelf.Position + new Vector3(0f, 0.035f, 0.145f), new Vector3(0.07f, 0.025f, 0.07f)),
            new Vector3(0f, 0f, -20f), trim, glasses);

        var lower = new Node3D { Name = "RearLowerCabinetBays" };
        parent.AddChild(lower);
        for (var bay = 0; bay < _layout.BottleRackBays.Count; bay++)
        {
            var centerX = _layout.BottleRackBays[bay].Back.Position.X;
            CreateBox(lower, new BarBoxLayout($"RearLowerBay{bay + 1}Back",
                new Vector3(centerX, 0.52f, -4.20f),
                new Vector3(BarLayoutDefinition.RearBayWidth - 0.10f, 1.0f, 0.04f)), dark, glasses);
            CreateBox(lower, new BarBoxLayout($"RearLowerBay{bay + 1}Base",
                new Vector3(centerX, 0.04f, -3.86f),
                new Vector3(BarLayoutDefinition.RearBayWidth - 0.10f, 0.08f, 0.66f)), dark, glasses);
            foreach (var side in new[] { -1f, 1f })
                CreateBox(lower, new BarBoxLayout(
                    $"RearLowerBay{bay + 1}{(side < 0f ? "Left" : "Right")}Side",
                    new Vector3(centerX + side * (BarLayoutDefinition.RearBayWidth * 0.5f - 0.07f), 0.52f, -3.86f),
                    new Vector3(0.04f, 1.0f, 0.66f)), trim, glasses);
        }
    }

    private void BuildUpperCabinetShells(Node3D parent, bool glasses)
    {
        var root = new Node3D { Name = "UpperBackCabinet" };
        parent.AddChild(root);
        var body = glasses ? new Color("073f50") : new Color("3f2924");
        var trim = glasses ? new Color("0b8290") : new Color("6e4935");
        for (var bay = 0; bay < _layout.BottleRackBays.Count; bay++)
        {
            var centerX = _layout.BottleRackBays[bay].Back.Position.X;
            var clearWidth = BarLayoutDefinition.RearBayWidth - 0.10f;
            CreateBox(root, new BarBoxLayout($"UpperBay{bay + 1}Back",
                new Vector3(centerX, BarLayoutDefinition.UpperCabinetCenterHeight, -3.99f),
                new Vector3(clearWidth, 1.30f, 0.04f)), body, glasses);
            CreateBox(root, new BarBoxLayout($"UpperBay{bay + 1}Bottom",
                new Vector3(centerX, BarLayoutDefinition.UpperCabinetBottomHeight + 0.02f, -3.80f),
                new Vector3(clearWidth, 0.04f, 0.38f)), body, glasses);
            CreateBox(root, new BarBoxLayout($"UpperBay{bay + 1}Top",
                new Vector3(centerX, BarLayoutDefinition.UpperCabinetTopHeight - 0.02f, -3.80f),
                new Vector3(clearWidth, 0.04f, 0.38f)), body, glasses);
            foreach (var side in new[] { -1f, 1f })
                CreateBox(root, new BarBoxLayout(
                    $"UpperBay{bay + 1}{(side < 0f ? "Left" : "Right")}Side",
                    new Vector3(centerX + side * (BarLayoutDefinition.RearBayWidth * 0.5f - 0.07f),
                        BarLayoutDefinition.UpperCabinetCenterHeight, -3.80f),
                    new Vector3(0.04f, 1.30f, 0.38f)), trim, glasses);
        }
    }

    private void BuildExpandedLounge(Node3D parent, bool glasses)
    {
        var tables = new Node3D { Name = "LoungeTables" };
        var chairs = new Node3D { Name = "LoungeChairs" };
        var stools = new Node3D { Name = "FrontStools" };
        parent.AddChild(tables);
        parent.AddChild(chairs);
        parent.AddChild(stools);
        foreach (var table in _layout.LoungeTables)
        {
            var root = new Node3D { Name = table.Name };
            tables.AddChild(root);
            CreateCylinder(
                root,
                "Top",
                Vector3.Zero,
                table.Radius,
                table.Height,
                glasses ? new Color("087a83") : table.RealityColor,
                glasses);
            root.Position = table.Position;
            CreateCylinder(root, "Pedestal", new Vector3(0f, -0.34f, 0f), 0.075f, 0.64f,
                glasses ? new Color("1c8090") : new Color("4d5154"), glasses);
            CreateCylinder(root, "Base", new Vector3(0f, -0.66f, 0f), 0.24f, 0.04f,
                glasses ? new Color("1c8090") : new Color("4d5154"), glasses);
        }
        foreach (var chair in _layout.LoungeChairs)
            BuildChair(chairs, chair, glasses);
        foreach (var stool in _layout.FrontStools)
        {
            var root = new Node3D { Name = stool.Name, Position = stool.Position };
            stools.AddChild(root);
            CreateCylinder(
                root,
                "Seat",
                Vector3.Zero,
                stool.Radius,
                stool.Height,
                glasses ? new Color("087a83") : stool.RealityColor,
                glasses);
            CreateCylinder(root, "Stem", new Vector3(0f, -0.36f, 0f), 0.035f, 0.66f,
                glasses ? new Color("1c8090") : new Color("4d5154"), glasses);
            CreateCylinder(root, "FootRing", new Vector3(0f, -0.49f, 0f), 0.15f, 0.025f,
                glasses ? new Color("1c8090") : new Color("4d5154"), glasses);
            CreateCylinder(root, "Base", new Vector3(0f, -0.75f, 0f), 0.18f, 0.035f,
                glasses ? new Color("1c8090") : new Color("4d5154"), glasses);
        }
    }

    private static void BuildChair(Node3D parent, BarChairLayout chair, bool glasses)
    {
        var root = new Node3D { Name = chair.Id, Position = chair.Position };
        parent.AddChild(root);
        var wood = glasses ? new Color("096174") : new Color("65412e");
        var metal = glasses ? new Color("1c8090") : new Color("4d5154");
        CreateBox(root, new BarBoxLayout("Seat", new Vector3(0f, 0.03f, 0f), new Vector3(0.46f, 0.06f, 0.50f)), wood, glasses);
        CreateBox(root, new BarBoxLayout("Back", new Vector3(0f, 0.27f, -0.22f), new Vector3(0.46f, 0.34f, 0.06f)), wood, glasses);
        foreach (var x in new[] { -0.18f, 0.18f })
        foreach (var z in new[] { -0.18f, 0.18f })
            CreateBox(root, new BarBoxLayout($"Leg{x}_{z}", new Vector3(x, -0.20f, z), new Vector3(0.035f, 0.40f, 0.035f)), metal, glasses);
    }

    private void BuildLightRig(Node3D parent, bool glasses)
    {
        var rig = new Node3D { Name = "BarLightRig" };
        parent.AddChild(rig);
        BuildFixtureGroup(rig, "Pendants", _layout.PendantFixtures, glasses, true);
        BuildFixtureGroup(rig, "RearLinears", _layout.RearLinearFixtures, glasses, true);
        BuildFixtureGroup(rig, "CustomerSconces", _layout.CustomerSconces, glasses, true);
        BuildFixtureGroup(rig, "CustomerFills", _layout.CustomerFillLights, glasses, false);
    }

    private static void BuildFixtureGroup(Node3D rig, string name,
        IReadOnlyList<BarLightFixtureLayout> fixtures, bool glasses, bool visibleGeometry)
    {
        var group = new Node3D { Name = name };
        rig.AddChild(group);
        foreach (var fixture in fixtures)
        {
            var root = new Node3D { Name = fixture.Id, Position = fixture.Position };
            group.AddChild(root);
            if (visibleGeometry && fixture.HasVisibleGeometry)
            {
                var fixtureColor = glasses ? new Color("4ba9b8") : new Color("8b654a");
                if (fixture.Group == "rear_linear")
                    CreateBox(root,
                        new BarBoxLayout("Fixture", Vector3.Zero, new Vector3(2.40f, 0.035f, 0.06f)),
                        fixtureColor, glasses);
                else if (fixture.Group == "customer_sconce")
                    CreateBox(root,
                        new BarBoxLayout("Fixture", Vector3.Zero, new Vector3(0.18f, 0.12f, 0.08f)),
                        fixtureColor, glasses);
                else
                {
                    CreateCylinder(root, "Fixture", Vector3.Zero, 0.11f, 0.12f,
                        fixtureColor, glasses);
                    CreateCylinder(root, "Cord", new Vector3(0f, 0.30f, 0f), 0.012f, 0.50f,
                        glasses ? new Color("357b86") : new Color("3d302b"), glasses);
                }
            }
            var energy = fixture.Group switch
            {
                "front_pendant" => 3.4f,
                "rear_linear" => 2.2f,
                "customer_fill" => 1.35f,
                _ => 1.6f
            };
            var range = fixture.Group == "customer_fill" ? 5.2f : 3.6f;
            root.AddChild(new OmniLight3D
            {
                Name = "Light",
                LightColor = glasses ? new Color("8cb8c4") : new Color("ffd0a0"),
                LightEnergy = energy,
                OmniRange = range,
                ShadowEnabled = fixture.Group != "customer_fill"
            });
        }
    }

    private static void CreateRotatedBox(Node3D parent, BarBoxLayout layout, Vector3 rotationDegrees,
        Color color, bool emissive)
    {
        parent.AddChild(new MeshInstance3D
        {
            Name = layout.Name,
            Position = layout.Position,
            RotationDegrees = rotationDegrees,
            Mesh = new BoxMesh { Size = layout.Size },
            MaterialOverride = MakeMaterial(color, emissive)
        });
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

    internal static void AddStaticRotatedBox(
        Node3D parent,
        string name,
        Vector3 position,
        Vector3 size,
        Vector3 rotationDegrees,
        uint collisionLayer = 1)
    {
        var body = new StaticBody3D
        {
            Name = name,
            Position = position,
            RotationDegrees = rotationDegrees,
            CollisionLayer = collisionLayer
        };
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
