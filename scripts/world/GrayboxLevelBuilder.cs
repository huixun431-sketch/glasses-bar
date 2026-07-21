using System;
using Godot;

namespace GlassesBar;

public partial class GrayboxLevelBuilder : Node3D
{
    private static readonly (string Id, StationKind Kind, Vector3 Position, Vector3 Size, string Label)[] Stations =
    {
        ("customer", StationKind.Customer, new Vector3(0f, 1f, 2.05f), new Vector3(0.65f, 1.7f, 0.65f), "客人"),
        ("ice_bucket", StationKind.IceBucket, new Vector3(-2.75f, 1.06f, -5.45f), new Vector3(0.72f, 0.55f, 0.72f), "冰桶"),
        ("coffee_beans", StationKind.CoffeeBeans, new Vector3(-1.3f, 1.3f, -5.4f), new Vector3(0.62f, 0.52f, 0.62f), "咖啡豆"),
        ("water_dispenser", StationKind.WaterDispenser, new Vector3(3.75f, 1.08f, 0.15f), new Vector3(1.15f, 0.38f, 0.92f), "水槽"),
        ("waste_bin", StationKind.WasteBin, new Vector3(4.82f, 0.58f, -3.85f), new Vector3(0.78f, 1.15f, 0.78f), "弃物桶")
    };

    private static readonly (string ToolId, Vector3 Position, Color Color)[] ToolLayouts =
    {
        ("highball_glass", new Vector3(-4.35f, 1.24f, -5.42f), new Color(0.62f, 0.82f, 0.94f, 0.62f)),
        ("mortar", new Vector3(1.62f, 1.24f, 0.12f), new Color("786859")),
        ("traditional_filter", new Vector3(-1.62f, 1.24f, 0.12f), new Color("aaa08b")),
        ("pestle", new Vector3(2.22f, 1.24f, 0.12f), new Color("6c5546")),
        ("bean_scoop", new Vector3(-2.05f, 1.13f, -5.42f), new Color("9a8b72")),
        ("ice_tongs", new Vector3(-3.55f, 1.13f, -5.42f), new Color("8797a1")),
        ("water_carafe", new Vector3(1.25f, 1.13f, -5.42f), new Color(0.42f, 0.68f, 0.82f, 0.72f))
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
        BuildStations();
        BuildCabinetry();
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
        AddStaticBox(_neutral, "FloorCollider", new Vector3(0f, -0.15f, -1f), new Vector3(14f, 0.3f, 14f));
        CreateBox(_reality, "Floor", new Vector3(0f, -0.15f, -1f), new Vector3(14f, 0.3f, 14f), new Color("2d2424"));
        CreateBox(_glasses, "Floor", new Vector3(0f, -0.15f, -1f), new Vector3(14f, 0.3f, 14f), new Color("071d29"), true);

        var walls = new[]
        {
            ("BackWall", new Vector3(0f, 2.4f, -7.8f), new Vector3(14f, 5f, 0.25f)),
            ("LeftWall", new Vector3(-7f, 2.4f, -1f), new Vector3(0.25f, 5f, 14f)),
            ("RightWall", new Vector3(7f, 2.4f, -1f), new Vector3(0.25f, 5f, 14f)),
            ("FrontWall", new Vector3(0f, 2.4f, 6f), new Vector3(14f, 5f, 0.25f))
        };
        foreach (var wall in walls)
        {
            AddStaticBox(_neutral, wall.Item1 + "Collider", wall.Item2, wall.Item3);
            CreateBox(_reality, wall.Item1, wall.Item2, wall.Item3, new Color("201d24"));
            CreateBox(_glasses, wall.Item1, wall.Item2, wall.Item3, new Color("052b3a"), true);
        }

        AddStaticBox(_neutral, "WorkingCounterCollider", new Vector3(0f, 0.45f, 0.15f), new Vector3(11.5f, 0.9f, 1.25f), 2);
        CreateBox(_reality, "WorkingCounter", new Vector3(0f, 0.45f, 0.15f), new Vector3(11.5f, 0.9f, 1.25f), new Color("5b3524"));
        CreateBox(_reality, "WorkingCounterTop", new Vector3(0f, 0.94f, 0.15f), new Vector3(11.8f, 0.12f, 1.42f), new Color("8b5634"));
        CreateBox(_glasses, "WorkingCounter", new Vector3(0f, 0.45f, 0.15f), new Vector3(11.5f, 0.9f, 1.25f), new Color("075366"), true);
        CreateBox(_glasses, "WorkingCounterTop", new Vector3(0f, 0.94f, 0.15f), new Vector3(11.8f, 0.12f, 1.42f), new Color("0f98a4"), true);

        AddStaticBox(_neutral, "BackCounterCollider", new Vector3(0f, 0.45f, -5.7f), new Vector3(11.5f, 0.85f, 1.4f), 2);
        CreateBox(_reality, "BackCounter", new Vector3(0f, 0.45f, -5.7f), new Vector3(11.5f, 0.85f, 1.4f), new Color("3f2924"));
        CreateBox(_glasses, "BackCounter", new Vector3(0f, 0.45f, -5.7f), new Vector3(11.5f, 0.85f, 1.4f), new Color("073f50"), true);
        foreach (var side in new[] { -1f, 1f })
        {
            var position = new Vector3(side * 5.5f, 0.48f, -2.73f);
            var size = new Vector3(0.58f, 0.96f, 4.55f);
            AddStaticBox(_neutral, side < 0 ? "LeftReturnCollider" : "RightReturnCollider", position, size, 2);
            CreateBox(_reality, side < 0 ? "LeftCounterReturn" : "RightCounterReturn", position, size, new Color("4b3027"));
            CreateBox(_glasses, side < 0 ? "LeftCounterReturn" : "RightCounterReturn", position, size, new Color("075064"), true);
            CreateBox(_reality, side < 0 ? "LeftReturnTop" : "RightReturnTop", position + new Vector3(0f, 0.53f, 0f), new Vector3(0.72f, 0.1f, 4.7f), new Color("805036"));
            CreateBox(_glasses, side < 0 ? "LeftReturnTop" : "RightReturnTop", position + new Vector3(0f, 0.53f, 0f), new Vector3(0.72f, 0.1f, 4.7f), new Color("0b8d9a"), true);
        }
        BuildBackBar(_reality, false);
        BuildBackBar(_glasses, true);
        BuildFrontWorktop(_reality, false);
        BuildFrontWorktop(_glasses, true);
        BuildLounge(_reality, false);
        BuildLounge(_glasses, true);
    }

    private void BuildCounterSurfaces(DrinkWorkstation workstation)
    {
        var front = new CounterSurfaceInteractable();
        front.Configure(workstation, "front_counter_surface", new Vector3(0f, 1.03f, 0.15f), new Vector3(10.85f, 0.08f, 1.12f));
        _neutral.AddChild(front);
        var back = new CounterSurfaceInteractable();
        back.Configure(workstation, "back_counter_surface", new Vector3(0f, 0.92f, -5.7f), new Vector3(10.85f, 0.08f, 1.12f));
        _neutral.AddChild(back);
    }

    private void BuildTools(DrinkWorkstation workstation)
    {
        foreach (var layout in ToolLayouts)
        {
            var spec = workstation.GetToolSpec(layout.ToolId);
            var position = layout.Position;
            var node = new ToolInteractable { Position = position };
            node.Configure(workstation, spec, ToolMesh(layout.ToolId), layout.Color);
            _neutral.AddChild(node);
            workstation.RegisterTool(node, layout.ToolId, position);
        }
    }

    private void BuildWorkboard(DrinkWorkstation workstation)
    {
        var board = new WorkboardInteractable();
        board.Configure(workstation, new Vector3(0f, 1.12f, 0.12f), new Vector3(0.62f, 0.18f, 0.76f), new[]
        {
            new Vector3(-0.72f, 1.28f, 0.12f),
            new Vector3(0f, 1.28f, 0.12f),
            new Vector3(0.72f, 1.28f, 0.12f)
        });
        _neutral.AddChild(board);
    }

    private void BuildStations()
    {
        foreach (var station in Stations)
        {
            var gameplay = new StationInteractable
            {
                Name = station.Id,
                EntityId = station.Id,
                Kind = station.Kind,
                Position = station.Position
            };
            gameplay.AddToGroup("interactable");
            gameplay.AddChild(new CollisionShape3D
            {
                Name = "CollisionShape3D",
                Shape = new BoxShape3D { Size = station.Size }
            });
            _neutral.AddChild(gameplay);
            CreateStationVisual(_reality, station.Id, station.Position, station.Size, station.Kind, station.Label, false);
            if (station.Kind is StationKind.WaterDispenser or StationKind.WasteBin)
                CreateStationVisual(_glasses, station.Id, station.Position, station.Size, station.Kind, station.Label, true);
        }
    }

    private void BuildCabinetry()
    {
        var modules = new[] { -4.65f, -2.85f, -1.05f, 0.75f, 2.35f, 5.1f };
        for (var index = 0; index < modules.Length; index++)
        {
            var centerX = modules[index];
            CreateBox(_neutral, $"front_cabinet_cavity_{index + 1}", new Vector3(centerX, 0.49f, -0.445f),
                new Vector3(1.58f, 0.79f, 0.07f), new Color("171112"));
            var drawer = new CabinetInteractable();
            drawer.Configure($"front_drawer_{index + 1}", CabinetPartKind.Drawer,
                new Vector3(centerX, 0.77f, -0.515f), new Vector3(1.52f, 0.2f, 0.12f), false);
            _neutral.AddChild(drawer);

            for (var half = 0; half < 2; half++)
            {
                var left = half == 0;
                var door = new CabinetInteractable();
                door.Configure($"front_cabinet_{index + 1}_{(left ? "left" : "right")}", CabinetPartKind.Door,
                    new Vector3(centerX + (left ? -0.39f : 0.39f), 0.39f, -0.52f),
                    new Vector3(0.72f, 0.5f, 0.12f), left);
                _neutral.AddChild(door);
            }
        }
    }

    private void ResetCabinetry()
    {
        foreach (var node in GetTree().GetNodesInGroup("cabinet_storage"))
            if (node is CabinetInteractable cabinet)
                cabinet.ResetClosed();
    }

    private static Mesh ToolMesh(string toolId) => toolId switch
    {
        "highball_glass" => new CylinderMesh { TopRadius = 0.075f, BottomRadius = 0.06f, Height = 0.25f },
        "mortar" => new CylinderMesh { TopRadius = 0.2f, BottomRadius = 0.24f, Height = 0.24f },
        "traditional_filter" => new CylinderMesh { TopRadius = 0.18f, BottomRadius = 0.11f, Height = 0.32f },
        "water_carafe" => new CylinderMesh { TopRadius = 0.1f, BottomRadius = 0.16f, Height = 0.38f },
        "pestle" => new CylinderMesh { TopRadius = 0.055f, BottomRadius = 0.075f, Height = 0.42f },
        "ice_tongs" => new BoxMesh { Size = new Vector3(0.1f, 0.08f, 0.46f) },
        _ => new BoxMesh { Size = new Vector3(0.18f, 0.1f, 0.34f) }
    };

    private static void BuildBackBar(Node3D parent, bool glasses)
    {
        var wood = glasses ? new Color("074a5e") : new Color("402821");
        var shelf = glasses ? new Color("0ba0a8") : new Color("795038");
        CreateBox(parent, "BackBarPanel", new Vector3(0f, 2.45f, -7.58f), new Vector3(10.8f, 3.6f, 0.18f), wood, glasses);
        for (var row = 0; row < 3; row++)
            CreateBox(parent, $"Shelf{row}", new Vector3(0f, 1.25f + row * 0.9f, -7.32f), new Vector3(10.3f, 0.12f, 0.55f), shelf, glasses);
        if (glasses)
            return;
        for (var index = 0; index < 10; index++)
        {
            var x = -4.7f + (index % 5) * 2.35f;
            var y = 1.6f + (index / 5) * 0.9f;
            CreateCylinder(parent, $"BackLiquor{index}", new Vector3(x, y, -7.08f), 0.13f, 0.52f, BottleColor(index));
        }
        CreateBox(parent, "LemonBasket", new Vector3(0f, 1.22f, -5.38f), new Vector3(0.85f, 0.35f, 0.62f), new Color("c5a33c"));
    }

    private static void BuildFrontWorktop(Node3D parent, bool glasses)
    {
        var accent = glasses ? new Color("28d5c6") : new Color("845531");
        CreateBox(parent, "CuttingBoard", new Vector3(0f, 1.06f, 0.12f), new Vector3(2.25f, 0.12f, 0.92f), accent, glasses);
        if (glasses)
        {
            parent.AddChild(new Label3D
            {
                Name = "CuttingBoardLabel",
                Text = "砧板｜能力由已放置工具组合决定",
                Position = new Vector3(0f, 1.68f, 0.08f),
                FontSize = 25,
                OutlineSize = 8,
                Modulate = new Color("c6fff5"),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                NoDepthTest = true
            });
            CreateBox(parent, "OperationManual", new Vector3(-3.25f, 1.11f, 0.08f), new Vector3(1.05f, 0.12f, 0.78f), new Color("45f1d4"), true);
            parent.AddChild(new Label3D
            {
                Name = "OperationManualLabel",
                Text = "操作手册",
                Position = new Vector3(-3.25f, 1.42f, 0.08f),
                FontSize = 28,
                OutlineSize = 8,
                Modulate = new Color("c6fff5"),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                NoDepthTest = true
            });
            return;
        }
        for (var side = -1; side <= 1; side += 2)
            CreateCylinder(parent, side < 0 ? "LeftDisplayLiquor" : "RightDisplayLiquor", new Vector3(side * 5.05f, 1.42f, 0.1f), 0.22f, 0.82f, side < 0 ? new Color("49663d") : new Color("805039"));
    }

    private static void BuildLounge(Node3D parent, bool glasses)
    {
        var booth = glasses ? new Color("093f58") : new Color("4f2630");
        var table = glasses ? new Color("087a83") : new Color("5c3929");
        for (var side = -1; side <= 1; side += 2)
        {
            CreateBox(parent, side < 0 ? "LeftBooth" : "RightBooth", new Vector3(side * 4.2f, 0.62f, 3.3f), new Vector3(2.5f, 1.15f, 0.9f), booth, glasses);
            CreateCylinder(parent, side < 0 ? "LeftTable" : "RightTable", new Vector3(side * 2.65f, 0.72f, 3.25f), 0.65f, 0.12f, table, glasses);
        }
        for (var window = 0; window < 3; window++)
        {
            var x = -3.7f + window * 3.7f;
            CreateBox(parent, $"NightWindow{window}", new Vector3(x, 3f, 5.84f), new Vector3(2.9f, 2.5f, 0.05f), glasses ? new Color("063a52") : new Color("07172c"), true);
            for (var light = 0; light < 6; light++)
                CreateBox(parent, $"WindowLight{window}_{light}", new Vector3(x - 1f + light % 3, 2.2f + light / 3 * 0.75f, 5.78f), new Vector3(0.12f, 0.18f, 0.04f), glasses ? new Color("21d4c3") : new Color("e3ad55"), true);
        }
    }

    private static void CreateStationVisual(Node3D parent, string id, Vector3 position, Vector3 size, StationKind kind, string label, bool glasses)
    {
        var holder = new Node3D { Name = id, Position = position };
        parent.AddChild(holder);
        holder.AddChild(new MeshInstance3D
        {
            Name = "Visual",
            Mesh = kind == StationKind.Customer
                ? new CapsuleMesh { Radius = 0.32f, Height = 1.65f }
                : kind is StationKind.IceBucket or StationKind.CoffeeBeans or StationKind.WasteBin
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
        StationKind.WaterDispenser => new Color("6385a5"),
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

    private static void CreateBox(Node3D parent, string name, Vector3 position, Vector3 size, Color color, bool emissive = false)
    {
        parent.AddChild(new MeshInstance3D
        {
            Name = name,
            Position = position,
            Mesh = new BoxMesh { Size = size },
            MaterialOverride = MakeMaterial(color, emissive)
        });
    }

    private static void CreateCylinder(Node3D parent, string name, Vector3 position, float radius, float height, Color color, bool emissive = false)
    {
        parent.AddChild(new MeshInstance3D
        {
            Name = name,
            Position = position,
            Mesh = new CylinderMesh { TopRadius = radius * 0.82f, BottomRadius = radius, Height = height },
            MaterialOverride = MakeMaterial(color, emissive)
        });
    }

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
