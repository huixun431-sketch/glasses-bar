using System;
using Godot;

namespace GlassesBar;

/// <summary>
/// Creates gameplay adapters and binds the existing scene controllers. Presentation
/// construction is delegated to GrayboxArchitectureBuilder.
/// </summary>
public sealed class GameplaySceneComposer
{
    private readonly Node3D _root;
    private readonly BarLayoutDefinition _layout;
    private readonly Node3D _neutral;
    private readonly GrayboxArchitectureBuilder _architecture;

    public GameplaySceneComposer(
        Node3D root,
        BarLayoutDefinition layout,
        Node3D neutral,
        GrayboxArchitectureBuilder architecture)
    {
        _root = root;
        _layout = layout;
        _neutral = neutral;
        _architecture = architecture;
    }

    public DrinkWorkstation CreateWorkstation()
    {
        var catalog =
            ResourceLoader.Load<GameplayCatalogDefinition>(
                "res://data/gameplay/prototype_gameplay_catalog.tres")
            ?? throw new InvalidOperationException(
                "Prototype gameplay catalog could not be loaded.");
        var workstation = new DrinkWorkstation { Name = "DrinkWorkstation" };
        workstation.ConfigureCatalog(catalog);
        workstation.AddToGroup("workstation");
        _neutral.AddChild(workstation);
        return workstation;
    }

    public void BuildCounterSurfaces(DrinkWorkstation workstation)
    {
        var front = new CounterSurfaceInteractable();
        front.Configure(
            workstation,
            _layout.FrontCounterSurface.Id,
            _layout.FrontCounterSurface.Position,
            _layout.FrontCounterSurface.Size);
        _neutral.AddChild(front);

        var rear = new CounterSurfaceInteractable();
        rear.Configure(
            workstation,
            _layout.RearShelfSurface.Id,
            _layout.RearShelfSurface.Position,
            _layout.RearShelfSurface.Size);
        _neutral.AddChild(rear);
    }

    public void BuildStations()
    {
        foreach (var station in _layout.Stations)
        {
            CreateGameplayStation(
                _neutral,
                station.Id,
                station.Kind,
                station.Position,
                station.Size);
            _architecture.CreateStationVisual(station, false);
            if (station.Kind is StationKind.HandWashSink or StationKind.Kettle or StationKind.WasteBin)
                _architecture.CreateStationVisual(station, true);
        }
    }

    public void BuildWorkboard(DrinkWorkstation workstation)
    {
        var board = new WorkboardInteractable();
        board.Configure(
            workstation,
            _layout.Workboard.Position,
            _layout.Workboard.Size,
            System.Linq.Enumerable.ToArray(_layout.Workboard.Slots));
        _neutral.AddChild(board);
    }

    public void BuildTools(DrinkWorkstation workstation)
    {
        foreach (var layout in _layout.Tools)
        {
            var spec = workstation.GetToolSpec(layout.ToolId);
            var node = new ToolInteractable { Position = layout.Position };
            node.Configure(workstation, spec, ToolMesh(layout.ToolId), layout.Color);
            _neutral.AddChild(node);
            workstation.RegisterTool(node, layout.ToolId, layout.Position);
        }
    }

    public void BindRuntime(DrinkWorkstation workstation, Action resetCabinetry)
    {
        var player = _root.GetNode<PlayerController>("Player");
        var hud = _root.GetNode<HudController>("HUD");
        var menu = _root.GetNode<OpeningMenuController>("OpeningMenu");
        var pauseMenu = _root.GetNode<PauseMenuController>("PauseMenu");
        player.BindWorkstation(workstation);
        hud.Bind(player, workstation);
        menu.StartRequested += () =>
        {
            workstation.ResetForNewDay();
            player.ResetForNewDay();
            resetCabinetry();
            GameSession.Instance.StartNewGame();
        };
        menu.QuitRequested += () => _root.GetTree().Quit();
        pauseMenu.RestartDayRequested += () =>
        {
            workstation.ResetForNewDay();
            player.ResetForNewDay();
            resetCabinetry();
            GameSession.Instance.RestartDay();
        };
        pauseMenu.ReturnToMainMenuRequested += () =>
        {
            workstation.ResetForNewDay();
            player.ResetForNewDay();
            resetCabinetry();
            GameSession.Instance.ReturnToMainMenu();
        };
        GameSession.Instance.DayChanged += _ => resetCabinetry();
        GameSession.Instance.GameStartedChanged += started =>
        {
            if (!started)
                resetCabinetry();
        };
    }

    internal static StationInteractable CreateGameplayStation(
        Node3D parent,
        string id,
        StationKind kind,
        Vector3 position,
        Vector3 size)
    {
        var definition = StationDefinitionCatalog.GetPrototype(id, kind);
        var gameplay = new StationInteractable
        {
            Name = id,
            EntityId = id,
            Kind = kind,
            Definition = definition,
            Position = position
        };
        gameplay.AddToGroup("interactable");
        gameplay.AddChild(new CollisionShape3D
        {
            Name = "CollisionShape3D",
            Shape = new BoxShape3D { Size = size }
        });
        parent.AddChild(gameplay);
        return gameplay;
    }

    private static Mesh ToolMesh(string toolId) => toolId switch
    {
        "highball_glass" => new CylinderMesh
        {
            TopRadius = 0.075f,
            BottomRadius = 0.06f,
            Height = 0.25f
        },
        "mortar" => new CylinderMesh
        {
            TopRadius = 0.2f,
            BottomRadius = 0.24f,
            Height = 0.24f
        },
        "traditional_filter" => new CylinderMesh
        {
            TopRadius = 0.18f,
            BottomRadius = 0.11f,
            Height = 0.32f
        },
        "pestle" => new CylinderMesh
        {
            TopRadius = 0.055f,
            BottomRadius = 0.075f,
            Height = 0.42f
        },
        "jigger_small" => new CylinderMesh
        {
            TopRadius = 0.055f,
            BottomRadius = 0.055f,
            Height = 0.15f
        },
        "jigger_medium" => new CylinderMesh
        {
            TopRadius = 0.065f,
            BottomRadius = 0.065f,
            Height = 0.18f
        },
        "jigger_large" => new CylinderMesh
        {
            TopRadius = 0.075f,
            BottomRadius = 0.075f,
            Height = 0.21f
        },
        "ice_tongs" => new BoxMesh
        {
            Size = new Vector3(0.1f, 0.08f, 0.46f)
        },
        _ => new BoxMesh
        {
            Size = new Vector3(0.18f, 0.1f, 0.34f)
        }
    };
}
