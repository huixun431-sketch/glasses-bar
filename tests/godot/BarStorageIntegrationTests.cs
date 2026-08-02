using System;
using System.Linq;
using Godot;

namespace GlassesBar.Tests;

public partial class BarStorageIntegrationTests : Node
{
    public override void _Ready()
    {
        try
        {
            var main = GetNode<Node3D>("Main");
            main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Start")
                .EmitSignal(Button.SignalName.Pressed);
            GameSession.Instance.AcceptOrder();

            var player = main.GetNode<PlayerController>("Player");
            var workstation = main.GetNode<DrinkWorkstation>("NeutralGameplay/DrinkWorkstation");
            var context = new InteractionContext { Player = player, Workstation = workstation };
            var neutral = main.GetNode<Node3D>("NeutralGameplay");
            var layout = BarLayoutDefinition.Prototype;

            var storageFronts = main.GetTree().GetNodesInGroup("cabinet_storage")
                .OfType<CabinetInteractable>()
                .ToArray();
            Require(storageFronts.Length == layout.Storages.Count,
                "every authoritative storage has one runtime front");
            Require(storageFronts.All(front => !front.IsOpen),
                "every storage begins closed");

            var filter = main.GetTree().GetNodesInGroup("movable_tool")
                .OfType<ToolInteractable>()
                .Single(tool => tool.ToolId == "traditional_filter");
            var toolDrawer = neutral.GetNode<CabinetInteractable>("front_drawer_1_upper");
            Require(filter.StorageId == "front_drawer_1_upper" &&
                    filter.GetParent().Name == "front_drawer_1_upper_host",
                "filter is placed by local coordinates in its assigned drawer host");
            Require(!filter.CanInteract(context),
                "tool is inaccessible while its storage is closed");
            toolDrawer.SetOpen(true, false);
            Require(filter.CanInteract(context),
                "tool becomes accessible after its storage opens");

            var coffee = main.GetTree().GetNodesInGroup("interactable")
                .OfType<StationInteractable>()
                .Single(station => station.EntityId == "coffee_beans");
            var coffeeDoor = neutral.GetNode<CabinetInteractable>("rear_lower_cabinet_1");
            Require(coffee.StorageId == "rear_lower_cabinet_1" &&
                    coffee.GetParent().Name == "rear_lower_cabinet_1_host",
                "coffee is placed by local coordinates in its assigned rear cabinet host");
            Require(!coffee.IsStorageAccessible,
                "coffee is inaccessible while its cabinet is closed");
            var coffeeDoorClosedPosition = coffeeDoor.Position;
            var movingLeaf = coffeeDoor.GetNodeOrNull<Node3D>("MovingLeaf");
            Require(movingLeaf is not null,
                "rear lower cabinet exposes a dedicated moving sliding-door leaf");
            var movingLeafClosedPosition = movingLeaf!.Position;
            coffeeDoor.SetOpen(true, false);
            Require(!toolDrawer.IsOpen && coffeeDoor.IsOpen && coffee.IsStorageAccessible,
                "opening another storage front closes the first and exposes only its own contents");
            Require(coffeeDoor.Position.IsEqualApprox(coffeeDoorClosedPosition) &&
                    !Mathf.IsEqualApprox(movingLeaf.Position.X, movingLeafClosedPosition.X) &&
                    Mathf.IsEqualApprox(movingLeaf.Position.Y, movingLeafClosedPosition.Y) &&
                    Mathf.IsEqualApprox(movingLeaf.Position.Z, movingLeafClosedPosition.Z),
                "sliding door keeps the storage root fixed and moves one leaf laterally");

            var iceDrawer = neutral.GetNode<CabinetInteractable>("front_drawer_2_upper");
            var ice = main.GetTree().GetNodesInGroup("interactable")
                .OfType<StationInteractable>()
                .Single(station => station.EntityId == "ice_bucket");
            Require(ice.StorageId == "front_drawer_2_upper" && ice.GetParent().Name == "front_drawer_2_upper_host",
                "stable ice bucket remains assigned to the stable upper drawer");
            iceDrawer.SetOpen(true, false);
            Require(!coffeeDoor.IsOpen && iceDrawer.IsOpen && ice.IsStorageAccessible,
                "ice bucket follows the same storage access contract");

            main.GetNode<PauseMenuController>("PauseMenu")
                .EmitSignal(PauseMenuController.SignalName.RestartDayRequested);
            Require(storageFronts.All(front => !front.IsOpen),
                "daily restart closes every storage front");
            Require(movingLeaf.Position.IsEqualApprox(movingLeafClosedPosition),
                "daily restart returns the sliding leaf to its closed position");
            Require(layout.ItemStorageAssignments.All(assignment =>
                    main.GetTree().GetNodesInGroup("interactable")
                        .OfType<Node>()
                        .Any(node => node.Name == assignment.ItemId)),
                "daily restart preserves every assigned gameplay instance");

            GD.Print("BAR_STORAGE_INTEGRATION_PASS");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
