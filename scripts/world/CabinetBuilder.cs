using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GlassesBar;

/// <summary>
/// Assembles cabinet interaction nodes from layout data. Cabinet state remains on
/// CabinetInteractable; this builder owns no gameplay or presentation state.
/// </summary>
public sealed class CabinetBuilder
{
    private readonly BarLayoutDefinition _layout;
    private readonly Node3D _neutral;
    private readonly Dictionary<string, CabinetInteractable> _fronts =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, Node3D> _storageHosts =
        new(StringComparer.Ordinal);

    public CabinetBuilder(BarLayoutDefinition layout, Node3D neutral)
    {
        _layout = layout;
        _neutral = neutral;
    }

    public void Build()
    {
        foreach (var storage in _layout.Storages)
        {
            var layout = storage.Front;
            if (layout.Cavity is { } cavity)
                GrayboxArchitectureBuilder.CreateBox(
                    _neutral,
                    cavity,
                    new Color("171112"));

            var cabinet = new CabinetInteractable();
            cabinet.Configure(
                layout.Id,
                layout.Kind,
                layout.Center,
                layout.Size,
                layout.HingeOnLeft,
                layout.OutwardDirection,
                layout.StorageDepth,
                layout.OpenTravelDistance);
            _neutral.AddChild(cabinet);
            _fronts.Add(storage.Id, cabinet);

            var host = new Node3D { Name = storage.Id + "_host" };
            if (storage.MovesWithFront)
            {
                cabinet.AddChild(host);
                host.Position = storage.HostPosition - cabinet.Position;
            }
            else
            {
                _neutral.AddChild(host);
                host.Position = storage.HostPosition;
            }
            _storageHosts.Add(storage.Id, host);

            if (layout.ContainsIceBucket)
                AddIceBucket(host, cabinet);

            var contents = _layout.ItemStorageAssignments
                .Where(item => item.StorageId == storage.Id)
                .Select(item => item.ItemId)
                .ToArray();
            if (contents.Length > 0)
                cabinet.SetContentsDescription(string.Join("、", contents));
        }
    }

    public IReadOnlyDictionary<string, Node3D> StorageHosts => _storageHosts;

    public Node3D RequireHost(string storageId) =>
        _storageHosts.TryGetValue(storageId, out var host)
            ? host
            : throw new InvalidOperationException($"Unknown storage host '{storageId}'.");

    public CabinetInteractable RequireFront(string storageId) =>
        _fronts.TryGetValue(storageId, out var front)
            ? front
            : throw new InvalidOperationException($"Unknown storage front '{storageId}'.");

    public void ResetAll()
    {
        foreach (var node in _neutral.GetTree().GetNodesInGroup("cabinet_storage"))
            if (node is CabinetInteractable cabinet)
                cabinet.ResetClosed();

        foreach (var assignment in _layout.ItemStorageAssignments)
        {
            var assignedNode = _neutral.GetTree().GetNodesInGroup("interactable")
                .OfType<Node3D>()
                .FirstOrDefault(node => node.Name == assignment.ItemId);
            if (assignedNode is null || assignedNode is StationInteractable)
                continue;
            var host = RequireHost(assignment.StorageId);
            assignedNode.Reparent(host, false);
            assignedNode.Position = assignment.LocalPlacement;
            if (assignedNode is ToolInteractable tool)
                tool.ResetToStorage();
        }
    }

    private void AddIceBucket(Node3D host, CabinetInteractable drawer)
    {
        drawer.SetContentsDescription("内置冰桶");
        var station = GameplaySceneComposer.CreateGameplayStation(
            host,
            _layout.IceBucket.Id,
            _layout.IceBucket.Kind,
            _layout.IceBucket.Position,
            _layout.IceBucket.Size);
        station.BindStorage(drawer);
        var visual = new MeshInstance3D
        {
            Name = "Visual",
            Mesh = new CylinderMesh
            {
                TopRadius = 0.26f,
                BottomRadius = 0.23f,
                Height = 0.24f
            },
            MaterialOverride = GrayboxArchitectureBuilder.MakeMaterial(new Color("8da4b8"))
        };
        station.AddChild(visual);
        GameSession.Instance.WorldModeChanged +=
            mode => visual.Visible = (WorldMode)mode == WorldMode.Reality;
    }
}
