using System;
using System.Collections.Generic;
using Godot;

namespace GlassesBar;

public partial class GrayboxLevelBuilder : Node3D
{
    private static readonly (string Id, StationKind Kind, Vector3 Position, Vector3 Size, string Label)[] Stations =
    {
        ("customer", StationKind.Customer, new Vector3(0f, 1f, 1.2f), new Vector3(0.65f, 1.7f, 0.65f), "客人"),
        ("highball_glass", StationKind.PickupGlass, new Vector3(-4.2f, 0.9f, -3.2f), new Vector3(0.35f, 0.55f, 0.35f), "高球杯"),
        ("ice_bucket", StationKind.IceBucket, new Vector3(-2.8f, 0.9f, -3.2f), new Vector3(0.8f, 0.65f, 0.8f), "冰桶"),
        ("water_dispenser", StationKind.WaterDispenser, new Vector3(-1.35f, 1.1f, -3.35f), new Vector3(0.8f, 1.4f, 0.7f), "水台"),
        ("grinder", StationKind.Grinder, new Vector3(0.15f, 1.1f, -3.35f), new Vector3(0.75f, 1.35f, 0.7f), "磨豆机"),
        ("espresso_machine", StationKind.EspressoMachine, new Vector3(1.75f, 1.15f, -3.35f), new Vector3(1.1f, 1.45f, 0.85f), "咖啡机"),
        ("serve_counter", StationKind.ServeCounter, new Vector3(3.35f, 0.9f, -2.8f), new Vector3(1.15f, 0.35f, 1.05f), "出杯区"),
        ("sink", StationKind.Sink, new Vector3(4.65f, 0.9f, -3.2f), new Vector3(1.0f, 0.6f, 0.85f), "水槽/丢弃")
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
        BuildStations();

        var workstation = new DrinkWorkstation { Name = "DrinkWorkstation" };
        workstation.AddToGroup("workstation");
        _neutral.AddChild(workstation);

        var player = GetNode<PlayerController>("Player");
        var hud = GetNode<HudController>("HUD");
        player.BindWorkstation(workstation);
        hud.Bind(player, workstation);
    }

    private void BuildArchitecture()
    {
        AddStaticBox(_neutral, "FloorCollider", new Vector3(0f, -0.15f, -1f), new Vector3(14f, 0.3f, 14f));

        CreateBox(_reality, "Floor", new Vector3(0f, -0.15f, -1f), new Vector3(14f, 0.3f, 14f),
            new Color("2d2424"));
        CreateBox(_glasses, "Floor", new Vector3(0f, -0.15f, -1f), new Vector3(14f, 0.3f, 14f),
            new Color("071d29"), true);

        var walls = new[]
        {
            ("BackWall", new Vector3(0f, 2.4f, -7.8f), new Vector3(14f, 5f, 0.25f)),
            ("LeftWall", new Vector3(-7f, 2.4f, -1f), new Vector3(0.25f, 5f, 14f)),
            ("RightWall", new Vector3(7f, 2.4f, -1f), new Vector3(0.25f, 5f, 14f))
        };
        foreach (var wall in walls)
        {
            AddStaticBox(_neutral, wall.Item1 + "Collider", wall.Item2, wall.Item3);
            CreateBox(_reality, wall.Item1, wall.Item2, wall.Item3, new Color("201d24"));
            CreateBox(_glasses, wall.Item1, wall.Item2, wall.Item3, new Color("052b3a"), true);
        }

        CreateBox(_reality, "BackCounter", new Vector3(0f, 0.45f, -4f), new Vector3(11.5f, 0.85f, 1.4f), new Color("4b3027"));
        CreateBox(_glasses, "BackCounter", new Vector3(0f, 0.45f, -4f), new Vector3(11.5f, 0.85f, 1.4f), new Color("0c6a78"), true);
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
            var shape = new CollisionShape3D
            {
                Name = "CollisionShape3D",
                Shape = new BoxShape3D { Size = station.Size }
            };
            gameplay.AddChild(shape);
            _neutral.AddChild(gameplay);

            CreateStationVisual(_reality, station.Id, station.Position, station.Size, station.Kind, station.Label, false);
            if (station.Kind != StationKind.Customer)
                CreateStationVisual(_glasses, station.Id, station.Position, station.Size, station.Kind, station.Label, true);
        }
    }

    private static void CreateStationVisual(Node3D parent, string id, Vector3 position, Vector3 size,
        StationKind kind, string label, bool glasses)
    {
        var holder = new Node3D { Name = id, Position = position };
        parent.AddChild(holder);

        var mesh = new MeshInstance3D
        {
            Name = "Visual",
            Mesh = kind == StationKind.Customer
                ? new CapsuleMesh { Radius = 0.32f, Height = 1.65f }
                : new BoxMesh { Size = size },
            MaterialOverride = MakeMaterial(glasses ? new Color("2dd4bf") : RealityColor(kind), glasses)
        };
        holder.AddChild(mesh);

        if (!glasses)
            return;
        var tag = new Label3D
        {
            Name = "InformationLabel",
            Text = label,
            Position = new Vector3(0f, size.Y * 0.65f + 0.25f, 0f),
            FontSize = 48,
            OutlineSize = 12,
            Modulate = new Color("b8fff4"),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true
        };
        holder.AddChild(tag);
    }

    private static Color RealityColor(StationKind kind) => kind switch
    {
        StationKind.Customer => new Color("705667"),
        StationKind.PickupGlass => new Color(0.65f, 0.8f, 0.9f, 0.5f),
        StationKind.IceBucket => new Color("8da4b8"),
        StationKind.WaterDispenser => new Color("6385a5"),
        StationKind.Grinder => new Color("58483f"),
        StationKind.EspressoMachine => new Color("8d9399"),
        StationKind.ServeCounter => new Color("a17252"),
        StationKind.Sink => new Color("566b75"),
        _ => Colors.Gray
    };

    private static void AddStaticBox(Node3D parent, string name, Vector3 position, Vector3 size)
    {
        var body = new StaticBody3D { Name = name, Position = position };
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        parent.AddChild(body);
    }

    private static void CreateBox(Node3D parent, string name, Vector3 position, Vector3 size, Color color, bool emissive = false)
    {
        var mesh = new MeshInstance3D
        {
            Name = name,
            Position = position,
            Mesh = new BoxMesh { Size = size },
            MaterialOverride = MakeMaterial(color, emissive)
        };
        parent.AddChild(mesh);
    }

    private static StandardMaterial3D MakeMaterial(Color color, bool emissive)
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

