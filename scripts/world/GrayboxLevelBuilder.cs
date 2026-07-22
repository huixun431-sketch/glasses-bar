using System;
using Godot;

namespace GlassesBar;

public partial class GrayboxLevelBuilder : Node3D
{
    public const float FrontBarTopHeight = 1.18f;
    public const float BackBarTopHeight = 1.11f;
    public const float OperationAisleClearWidth = 1.24f;

    private const float FrontCounterZ = 0.2f;
    private const float BackCounterZ = -1.9f;

    private static readonly (string Id, StationKind Kind, Vector3 Position, Vector3 Size, string Label)[] Stations =
    {
        ("customer", StationKind.Customer, new Vector3(0f, 1f, 2.6f), new Vector3(0.65f, 1.7f, 0.65f), "客人"),
        ("coffee_beans", StationKind.CoffeeBeans, new Vector3(-3.75f, 1.34f, -1.84f), new Vector3(0.58f, 0.42f, 0.52f), "咖啡豆"),
        ("hand_wash_sink", StationKind.HandWashSink, new Vector3(3.55f, 1.16f, -1.86f), new Vector3(1.05f, 0.16f, 0.54f), "每日洗手水槽"),
        ("kettle", StationKind.Kettle, new Vector3(-4.75f, 1.41f, 0.2f), new Vector3(0.44f, 0.48f, 0.4f), "水壶｜量酒器水源"),
        ("waste_bin", StationKind.WasteBin, new Vector3(5.05f, 0.58f, -0.86f), new Vector3(0.72f, 1.1f, 0.72f), "弃物桶")
    };

    private static readonly (string ToolId, Vector3 Position, Color Color)[] ToolLayouts =
    {
        ("highball_glass", new Vector3(4.55f, 1.36f, 0.2f), new Color(0.62f, 0.82f, 0.94f, 0.62f)),
        ("mortar", new Vector3(2.05f, 1.36f, 0.2f), new Color("786859")),
        ("pestle", new Vector3(2.75f, 1.38f, 0.2f), new Color("6c5546")),
        ("traditional_filter", new Vector3(-1.25f, 1.4f, 0.2f), new Color("aaa08b")),
        ("bean_scoop", new Vector3(-2.05f, 1.34f, 0.2f), new Color("9a8b72")),
        ("ice_tongs", new Vector3(-2.7f, 1.34f, 0.2f), new Color("8797a1")),
        ("jigger_small", new Vector3(-3.3f, 1.34f, 0.2f), new Color("aab3b7")),
        ("jigger_medium", new Vector3(-3.75f, 1.35f, 0.2f), new Color("909da3")),
        ("jigger_large", new Vector3(-4.2f, 1.36f, 0.2f), new Color("76878f"))
    };

    private Node3D _neutral = null!;
    private Node3D _reality = null!;
    private Node3D _glasses = null!;

    public override void _Ready()
    {
        _neutral = GetNode<Node3D>("NeutralGameplay");
        _reality = GetNode<Node3D>("RealityWorld");
        _glasses = GetNode<Node3D>("GlassesWorld");
        BuildArchitecture();

        var catalog = ResourceLoader.Load<GameplayCatalogDefinition>("res://data/gameplay/prototype_gameplay_catalog.tres")
            ?? throw new InvalidOperationException("Prototype gameplay catalog could not be loaded.");
        var workstation = new DrinkWorkstation { Name = "DrinkWorkstation" };
        workstation.ConfigureCatalog(catalog);
        workstation.AddToGroup("workstation");
        _neutral.AddChild(workstation);

        BuildCounterSurfaces(workstation);
        BuildCabinetry();
        BuildStations();
        BuildWorkboard(workstation);
        BuildTools(workstation);

        var player = GetNode<PlayerController>("Player");
        var hud = GetNode<HudController>("HUD");
        var menu = GetNode<OpeningMenuController>("OpeningMenu");
        var pauseMenu = GetNode<PauseMenuController>("PauseMenu");
        player.BindWorkstation(workstation);
        hud.Bind(player, workstation);
        menu.StartRequested += () =>
        {
            workstation.ResetForNewDay();
            player.ResetForNewDay();
            ResetCabinetry();
            GameSession.Instance.StartNewGame();
        };
        menu.QuitRequested += () => GetTree().Quit();
        pauseMenu.RestartDayRequested += () =>
        {
            workstation.ResetForNewDay();
            player.ResetForNewDay();
            ResetCabinetry();
            GameSession.Instance.RestartDay();
        };
        pauseMenu.ReturnToMainMenuRequested += () =>
        {
            workstation.ResetForNewDay();
            player.ResetForNewDay();
            ResetCabinetry();
            GameSession.Instance.ReturnToMainMenu();
        };
        GameSession.Instance.DayChanged += _ => ResetCabinetry();
        GameSession.Instance.GameStartedChanged += started =>
        {
            if (!started)
                ResetCabinetry();
        };
    }

    private void BuildArchitecture()
    {
        var floorPosition = new Vector3(0f, -0.15f, 3.35f);
        var floorSize = new Vector3(14f, 0.3f, 13.3f);
        AddStaticBox(_neutral, "FloorCollider", floorPosition, floorSize);
        CreateBox(_reality, "ExpandedFloor", floorPosition, floorSize, new Color("2d2424"));
        CreateBox(_glasses, "ExpandedFloor", floorPosition, floorSize, new Color("071d29"), true);

        var walls = new[]
        {
            ("BackWall", new Vector3(0f, 2.4f, -3.3f), new Vector3(14f, 5f, 0.25f)),
            ("LeftWall", new Vector3(-7f, 2.4f, 3.35f), new Vector3(0.25f, 5f, 13.3f)),
            ("RightWall", new Vector3(7f, 2.4f, 3.35f), new Vector3(0.25f, 5f, 13.3f)),
            ("FrontWall", new Vector3(0f, 2.4f, 10f), new Vector3(14f, 5f, 0.25f))
        };
        foreach (var wall in walls)
        {
            AddStaticBox(_neutral, wall.Item1 + "Collider", wall.Item2, wall.Item3);
            CreateBox(_reality, wall.Item1, wall.Item2, wall.Item3, new Color("201d24"));
            CreateBox(_glasses, wall.Item1, wall.Item2, wall.Item3, new Color("052b3a"), true);
        }

        var frontBodySize = new Vector3(10.8f, 1.08f, 1f);
        var frontBodyPosition = new Vector3(0f, frontBodySize.Y * 0.5f, FrontCounterZ);
        AddStaticBox(_neutral, "FrontBarCollider", frontBodyPosition, frontBodySize, 2);
        CreateBox(_reality, "RaisedFrontBar", frontBodyPosition, frontBodySize, new Color("5b3524"));
        CreateBox(_glasses, "RaisedFrontBar", frontBodyPosition, frontBodySize, new Color("075366"), true);
        CreateBox(_reality, "RaisedFrontBarTop", new Vector3(0f, FrontBarTopHeight - 0.07f, FrontCounterZ),
            new Vector3(11.1f, 0.14f, 1.08f), new Color("8b5634"));
        CreateBox(_glasses, "RaisedFrontBarTop", new Vector3(0f, FrontBarTopHeight - 0.07f, FrontCounterZ),
            new Vector3(11.1f, 0.14f, 1.08f), new Color("0f98a4"), true);

        var backBodySize = new Vector3(10.8f, 0.98f, 0.72f);
        var backBodyPosition = new Vector3(0f, backBodySize.Y * 0.5f, BackCounterZ);
        AddStaticBox(_neutral, "MergedBackBarCollider", backBodyPosition, backBodySize, 2);
        CreateBox(_reality, "MergedBackBar", backBodyPosition, backBodySize, new Color("3f2924"));
        CreateBox(_glasses, "MergedBackBar", backBodyPosition, backBodySize, new Color("073f50"), true);
        CreateBox(_reality, "MergedBackBarTop", new Vector3(0f, BackBarTopHeight - 0.07f, BackCounterZ),
            new Vector3(11f, 0.14f, 0.8f), new Color("76503a"));
        CreateBox(_glasses, "MergedBackBarTop", new Vector3(0f, BackBarTopHeight - 0.07f, BackCounterZ),
            new Vector3(11f, 0.14f, 0.8f), new Color("0b8d9a"), true);

        foreach (var side in new[] { -1f, 1f })
        {
            var returnPosition = new Vector3(side * 5.25f, 0.54f, -0.68f);
            var returnSize = new Vector3(0.5f, 1.08f, 1.96f);
            AddStaticBox(_neutral, side < 0 ? "RightReturnCollider" : "LeftReturnCollider", returnPosition, returnSize, 2);
            CreateBox(_reality, side < 0 ? "RightCounterReturn" : "LeftCounterReturn", returnPosition, returnSize, new Color("4b3027"));
            CreateBox(_glasses, side < 0 ? "RightCounterReturn" : "LeftCounterReturn", returnPosition, returnSize, new Color("075064"), true);
        }

        BuildMergedBackRack(_reality, false);
        BuildMergedBackRack(_glasses, true);
        BuildFrontWorktop(_reality, false);
        BuildFrontWorktop(_glasses, true);
        BuildExpandedLounge(_reality, false);
        BuildExpandedLounge(_glasses, true);
    }

    private void BuildCounterSurfaces(DrinkWorkstation workstation)
    {
        var front = new CounterSurfaceInteractable();
        front.Configure(workstation, "front_counter_surface", new Vector3(0f, FrontBarTopHeight + 0.03f, FrontCounterZ),
            new Vector3(10.3f, 0.08f, 0.84f));
        _neutral.AddChild(front);
        var back = new CounterSurfaceInteractable();
        back.Configure(workstation, "back_counter_surface", new Vector3(0f, BackBarTopHeight + 0.03f, BackCounterZ),
            new Vector3(10.2f, 0.08f, 0.58f));
        _neutral.AddChild(back);
    }

    private void BuildTools(DrinkWorkstation workstation)
    {
        foreach (var layout in ToolLayouts)
        {
            var spec = workstation.GetToolSpec(layout.ToolId);
            var node = new ToolInteractable { Position = layout.Position };
            node.Configure(workstation, spec, ToolMesh(layout.ToolId), layout.Color);
            _neutral.AddChild(node);
            workstation.RegisterTool(node, layout.ToolId, layout.Position);
        }
    }

    private void BuildWorkboard(DrinkWorkstation workstation)
    {
        var board = new WorkboardInteractable();
        board.Configure(workstation, new Vector3(0.35f, 1.26f, FrontCounterZ), new Vector3(2.05f, 0.14f, 0.72f), new[]
        {
            new Vector3(-0.35f, 1.43f, FrontCounterZ),
            new Vector3(0.35f, 1.43f, FrontCounterZ),
            new Vector3(1.05f, 1.43f, FrontCounterZ)
        });
        _neutral.AddChild(board);
    }

    private void BuildStations()
    {
        foreach (var station in Stations)
        {
            CreateGameplayStation(_neutral, station.Id, station.Kind, station.Position, station.Size);
            CreateStationVisual(_reality, station.Id, station.Position, station.Size, station.Kind, station.Label, false);
            if (station.Kind is StationKind.HandWashSink or StationKind.Kettle or StationKind.WasteBin)
                CreateStationVisual(_glasses, station.Id, station.Position, station.Size, station.Kind, station.Label, true);
        }
    }

    private void BuildCabinetry()
    {
        BuildFrontDoubleDrawers();
        BuildBackCabinetDoors();
        BuildSinkLeftNarrowDrawers();
    }

    private void BuildFrontDoubleDrawers()
    {
        var moduleCenters = new[] { -4f, -2f, 0f, 2f, 4f };
        for (var moduleIndex = 0; moduleIndex < moduleCenters.Length; moduleIndex++)
        for (var layerIndex = 0; layerIndex < 2; layerIndex++)
        {
            var upper = layerIndex == 0;
            var id = $"front_drawer_{moduleIndex + 1}_{(upper ? "upper" : "lower")}";
            var center = new Vector3(moduleCenters[moduleIndex], upper ? 0.83f : 0.48f, -0.34f);
            CreateBox(_neutral, id + "_cavity", center + new Vector3(0f, 0f, 0.055f),
                new Vector3(1.72f, 0.28f, 0.05f), new Color("171112"));
            var drawer = new CabinetInteractable();
            drawer.Configure(id, CabinetPartKind.Drawer, center, new Vector3(1.72f, 0.28f, 0.1f), false,
                Vector3.Forward, 0.76f);
            _neutral.AddChild(drawer);

            // Player faces +Z, so screen-right is world -X. This is the upper drawer directly
            // below and right of the centered cutting board.
            if (moduleIndex == 1 && upper)
                AddIceBucketToDrawer(drawer);
        }
    }

    private void AddIceBucketToDrawer(CabinetInteractable drawer)
    {
        drawer.SetContentsDescription("内置冰桶");
        var station = CreateGameplayStation(drawer, "ice_bucket", StationKind.IceBucket,
            new Vector3(0f, 0.1f, 0.04f), new Vector3(0.62f, 0.25f, 0.48f));
        var visual = new MeshInstance3D
        {
            Name = "Visual",
            Mesh = new CylinderMesh { TopRadius = 0.26f, BottomRadius = 0.23f, Height = 0.24f },
            MaterialOverride = MakeMaterial(new Color("8da4b8"))
        };
        station.AddChild(visual);
        GameSession.Instance.WorldModeChanged += mode => visual.Visible = (WorldMode)mode == WorldMode.Reality;
    }

    private void BuildBackCabinetDoors()
    {
        var centers = new[] { -3f, -1.8f, -0.6f, 0.6f, 1.8f, 2.75f };
        for (var moduleIndex = 0; moduleIndex < centers.Length; moduleIndex++)
        for (var leafIndex = 0; leafIndex < 2; leafIndex++)
        {
            var leftLeaf = leafIndex == 0;
            var leafCenter = centers[moduleIndex] + (leftLeaf ? -0.2f : 0.2f);
            var door = new CabinetInteractable();
            door.Configure($"back_cabinet_{moduleIndex + 1}_{(leftLeaf ? "left" : "right")}", CabinetPartKind.Door,
                new Vector3(leafCenter, 0.52f, -1.5f), new Vector3(0.38f, 0.78f, 0.08f), leftLeaf,
                Vector3.Back);
            _neutral.AddChild(door);
        }
    }

    private void BuildSinkLeftNarrowDrawers()
    {
        for (var layerIndex = 0; layerIndex < 2; layerIndex++)
        {
            var upper = layerIndex == 0;
            var drawer = new CabinetInteractable();
            drawer.Configure($"sink_left_drawer_{(upper ? "upper" : "lower")}", CabinetPartKind.Drawer,
                new Vector3(4.65f, upper ? 0.8f : 0.47f, -1.5f), new Vector3(0.68f, 0.26f, 0.08f), false,
                Vector3.Back, 0.58f);
            _neutral.AddChild(drawer);
        }
    }

    private void ResetCabinetry()
    {
        foreach (var node in GetTree().GetNodesInGroup("cabinet_storage"))
            if (node is CabinetInteractable cabinet)
                cabinet.ResetClosed();
    }

    private static StationInteractable CreateGameplayStation(Node3D parent, string id, StationKind kind,
        Vector3 position, Vector3 size)
    {
        var gameplay = new StationInteractable { Name = id, EntityId = id, Kind = kind, Position = position };
        gameplay.AddToGroup("interactable");
        gameplay.AddChild(new CollisionShape3D { Name = "CollisionShape3D", Shape = new BoxShape3D { Size = size } });
        parent.AddChild(gameplay);
        return gameplay;
    }

    private static Mesh ToolMesh(string toolId) => toolId switch
    {
        "highball_glass" => new CylinderMesh { TopRadius = 0.075f, BottomRadius = 0.06f, Height = 0.25f },
        "mortar" => new CylinderMesh { TopRadius = 0.2f, BottomRadius = 0.24f, Height = 0.24f },
        "traditional_filter" => new CylinderMesh { TopRadius = 0.18f, BottomRadius = 0.11f, Height = 0.32f },
        "pestle" => new CylinderMesh { TopRadius = 0.055f, BottomRadius = 0.075f, Height = 0.42f },
        "jigger_small" => new CylinderMesh { TopRadius = 0.055f, BottomRadius = 0.055f, Height = 0.15f },
        "jigger_medium" => new CylinderMesh { TopRadius = 0.065f, BottomRadius = 0.065f, Height = 0.18f },
        "jigger_large" => new CylinderMesh { TopRadius = 0.075f, BottomRadius = 0.075f, Height = 0.21f },
        "ice_tongs" => new BoxMesh { Size = new Vector3(0.1f, 0.08f, 0.46f) },
        _ => new BoxMesh { Size = new Vector3(0.18f, 0.1f, 0.34f) }
    };

    private static void BuildMergedBackRack(Node3D parent, bool glasses)
    {
        var wood = glasses ? new Color("074a5e") : new Color("402821");
        var shelf = glasses ? new Color("0ba0a8") : new Color("795038");
        CreateBox(parent, "MergedBottleRackBack", new Vector3(0f, 2.15f, -2.57f), new Vector3(10.5f, 2.1f, 0.12f), wood, glasses);
        for (var row = 0; row < 3; row++)
            CreateBox(parent, $"MergedShelf{row}", new Vector3(0f, 1.32f + row * 0.55f, -2.4f),
                new Vector3(10.2f, 0.09f, 0.42f), shelf, glasses);
        if (glasses)
            return;
        for (var index = 0; index < 14; index++)
        {
            var x = -4.55f + index % 7 * 1.52f;
            var y = 1.55f + index / 7 * 0.56f;
            CreateCylinder(parent, $"BackLiquor{index}", new Vector3(x, y, -2.31f), 0.11f, 0.38f, BottleColor(index));
        }
    }

    private static void BuildFrontWorktop(Node3D parent, bool glasses)
    {
        var accent = glasses ? new Color("28d5c6") : new Color("845531");
        CreateBox(parent, "CuttingBoard", new Vector3(0.35f, 1.22f, FrontCounterZ), new Vector3(2.25f, 0.08f, 0.82f), accent, glasses);
        if (glasses)
        {
            parent.AddChild(new Label3D
            {
                Name = "CuttingBoardLabel",
                Text = "砧板｜能力由已放置工具组合决定",
                Position = new Vector3(0.35f, 1.72f, FrontCounterZ),
                FontSize = 25,
                OutlineSize = 8,
                Modulate = new Color("c6fff5"),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                NoDepthTest = true
            });
            CreateBox(parent, "OperationManual", new Vector3(-4.55f, 1.24f, FrontCounterZ), new Vector3(0.75f, 0.08f, 0.68f), new Color("45f1d4"), true);
            parent.AddChild(new Label3D
            {
                Name = "OperationManualLabel",
                Text = "操作手册",
                Position = new Vector3(-4.55f, 1.58f, FrontCounterZ),
                FontSize = 28,
                OutlineSize = 8,
                Modulate = new Color("c6fff5"),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                NoDepthTest = true
            });
        }
    }

    private static void BuildExpandedLounge(Node3D parent, bool glasses)
    {
        var booth = glasses ? new Color("093f58") : new Color("4f2630");
        var table = glasses ? new Color("087a83") : new Color("5c3929");
        for (var side = -1; side <= 1; side += 2)
        {
            CreateBox(parent, side < 0 ? "RightBooth" : "LeftBooth", new Vector3(side * 4.4f, 0.62f, 5.1f), new Vector3(2.4f, 1.15f, 0.9f), booth, glasses);
            CreateCylinder(parent, side < 0 ? "RightTable" : "LeftTable", new Vector3(side * 2.7f, 0.72f, 4.8f), 0.65f, 0.12f, table, glasses);
        }
        CreateBox(parent, "RearBooth", new Vector3(0f, 0.62f, 8.25f), new Vector3(3.2f, 1.15f, 0.9f), booth, glasses);
        CreateCylinder(parent, "RearTable", new Vector3(0f, 0.72f, 7f), 0.72f, 0.12f, table, glasses);
        for (var stool = 0; stool < 4; stool++)
        {
            var x = -3.2f + stool * 2.1f;
            CreateCylinder(parent, $"FrontStool{stool}", new Vector3(x, 0.7f, 1.35f), 0.27f, 0.12f, table, glasses);
        }
        for (var window = 0; window < 3; window++)
        {
            var x = -3.7f + window * 3.7f;
            CreateBox(parent, $"NightWindow{window}", new Vector3(x, 3f, 9.82f), new Vector3(2.9f, 2.5f, 0.05f),
                glasses ? new Color("063a52") : new Color("07172c"), true);
        }
    }

    private static void CreateStationVisual(Node3D parent, string id, Vector3 position, Vector3 size, StationKind kind,
        string label, bool glasses)
    {
        var holder = new Node3D { Name = id, Position = position };
        parent.AddChild(holder);
        holder.AddChild(new MeshInstance3D
        {
            Name = "Visual",
            Mesh = kind == StationKind.Customer
                ? new CapsuleMesh { Radius = 0.32f, Height = 1.65f }
                : kind is StationKind.CoffeeBeans or StationKind.Kettle or StationKind.WasteBin
                    ? new CylinderMesh { TopRadius = size.X * 0.36f, BottomRadius = size.X * 0.46f, Height = size.Y }
                    : new BoxMesh { Size = size },
            MaterialOverride = MakeMaterial(glasses ? new Color("2dd4bf") : RealityColor(kind), glasses)
        });
        if (glasses)
            holder.AddChild(new Label3D
            {
                Name = "InformationLabel",
                Text = label,
                Position = new Vector3(0f, size.Y * 0.65f + 0.25f, 0f),
                FontSize = 30,
                OutlineSize = 8,
                Modulate = new Color("b8fff4"),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                NoDepthTest = true
            });
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

    private static Color BottleColor(int index) => (index % 5) switch
    {
        0 => new Color("506b37"), 1 => new Color("8d5a32"), 2 => new Color("305a55"),
        3 => new Color("6d3b32"), _ => new Color("aaa087")
    };

    private static void AddStaticBox(Node3D parent, string name, Vector3 position, Vector3 size, uint collisionLayer = 1)
    {
        var body = new StaticBody3D { Name = name, Position = position, CollisionLayer = collisionLayer };
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        parent.AddChild(body);
    }

    private static void CreateBox(Node3D parent, string name, Vector3 position, Vector3 size, Color color, bool emissive = false) =>
        parent.AddChild(new MeshInstance3D
        {
            Name = name,
            Position = position,
            Mesh = new BoxMesh { Size = size },
            MaterialOverride = MakeMaterial(color, emissive)
        });

    private static void CreateCylinder(Node3D parent, string name, Vector3 position, float radius, float height, Color color,
        bool emissive = false) => parent.AddChild(new MeshInstance3D
    {
        Name = name,
        Position = position,
        Mesh = new CylinderMesh { TopRadius = radius * 0.82f, BottomRadius = radius, Height = height },
        MaterialOverride = MakeMaterial(color, emissive)
    });

    private static StandardMaterial3D MakeMaterial(Color color, bool emissive = false)
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
}
