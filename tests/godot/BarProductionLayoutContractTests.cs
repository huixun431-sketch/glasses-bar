using System;
using System.Linq;
using Godot;

namespace GlassesBar.Tests;

public partial class BarProductionLayoutContractTests : Node
{
    public override void _Ready()
    {
        try
        {
            Run();
            GD.Print("BAR_PRODUCTION_LAYOUT_CONTRACT_PASS");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private static void Run()
    {
        var layout = BarLayoutDefinition.Prototype;
        layout.Validate();

        Require(layout.RoomClearSize.IsEqualApprox(new Vector3(16f, 4.5f, 10f)),
            "room is 16 by 10 by 4.5 metres");
        Require(Mathf.IsEqualApprox(BarLayoutDefinition.FrontOutlineWidth, 9.10f) &&
                Mathf.IsEqualApprox(BarLayoutDefinition.PlayerWorktopHeight, 1.12f) &&
                Mathf.IsEqualApprox(BarLayoutDefinition.FrontBarTopHeight, 1.38f) &&
                Mathf.IsEqualApprox(BarLayoutDefinition.OperationAisleClearWidth, 1.55f) &&
                Mathf.IsEqualApprox(BarLayoutDefinition.PlayerEyeHeight, 1.83f),
            "approved Z3 and H3 scale is locked");
        Require(Mathf.IsEqualApprox(BarLayoutDefinition.UpperCabinetBottomHeight, 2.65f) &&
                Mathf.IsEqualApprox(BarLayoutDefinition.UpperCabinetTopHeight, 3.95f),
            "approved upper cabinet range is locked");
        Require(Mathf.IsEqualApprox(BarLayoutDefinition.BottleRackLowerShelfHeight, 1.50f) &&
                Mathf.IsEqualApprox(BarLayoutDefinition.BottleRackUpperShelfHeight, 2.10f) &&
                Mathf.IsEqualApprox(BarLayoutDefinition.BottleRackBackTopHeight, 2.55f),
            "approved empty rack heights are locked");

        Require(layout.SouthWindows.Count == 1 &&
                layout.SouthWindows[0].Size.IsEqualApprox(
                    new Vector3(3.20f, 1.55f, BarLayoutDefinition.WallThickness)) &&
                Mathf.IsEqualApprox(layout.SouthWindows[0].SillHeight, 0.75f),
            "south elevation keeps one east landscape window");
        Require(layout.Cabinets.Count(cabinet => cabinet.Kind == CabinetPartKind.Drawer) == 8 &&
                layout.Cabinets.Single(cabinet => cabinet.ContainsIceBucket).Id == "front_drawer_2_upper",
            "front bar keeps eight drawers and the stable ice drawer ID");
        Require(layout.Cabinets.Count(cabinet => cabinet.Id.StartsWith("rear_lower_cabinet_", StringComparison.Ordinal)) == 5,
            "rear bar has five lower storage fronts");
        Require(layout.Cabinets.Where(cabinet =>
                    cabinet.Id.StartsWith("rear_lower_cabinet_", StringComparison.Ordinal))
                .All(cabinet => cabinet.Kind.ToString() == "SlidingDoor") &&
                layout.Cabinets.Where(cabinet =>
                    cabinet.Id.StartsWith("back_cabinet_", StringComparison.Ordinal))
                .All(cabinet => cabinet.Kind == CabinetPartKind.Door),
            "rear lower cabinets use sliding doors while upper cabinets remain hinged");
        Require(layout.Cabinets.Count(cabinet => cabinet.Id.StartsWith("back_cabinet_", StringComparison.Ordinal)) == 10,
            "rear bar has five paired upper cabinet groups");
        Require(layout.FrontStools.Count == 6 && layout.LoungeTables.Count == 3 &&
                layout.LoungeChairs.Count == 12 && layout.Booths.Count == 0,
            "customer furniture counts are locked");
        Require(layout.PendantFixtures.Count == 3 && layout.RearLinearFixtures.Count == 2 &&
                layout.CustomerSconces.Count == 4 && layout.CustomerFillLights.Count == 2,
            "approved lighting groups are represented");
        Require(layout.FrontFootrails.Count == 0,
            "the front bar has no attached footrail or fixed footboard");
        Require(layout.PlayerFacingDirection.Z > 0.99f &&
                BarLayoutDefinition.RearBarFrontZ < BarLayoutDefinition.FrontBarInnerEdgeZ &&
                Mathf.IsEqualApprox(
                    BarLayoutDefinition.FrontBarInnerEdgeZ - BarLayoutDefinition.RearBarFrontZ,
                    BarLayoutDefinition.OperationAisleClearWidth),
            "rear bar is north, front bar is south, and the aisle is 1.55 metres");

        Require(layout.FrontBarInnerChamfers.Count == 0 &&
                layout.PlayerWorktopChamfers.Count == 0 &&
                layout.LiquorBottles.Count == 0,
            "obsolete overlay chamfers and placeholder bottles are absent");
        Require(layout.FrontBodyFootprint.Footprint.Count >= 6 &&
                layout.FrontPlayerTopFootprint.Footprint.Count >= 6 &&
                layout.FrontGuestRiserFootprint.Footprint.Count >= 4 &&
                layout.FrontGuestTopFootprint.Footprint.Count >= 4,
            "front body and both worktops use authoritative polygon outlines");
        Require(Mathf.IsEqualApprox(layout.FrontGuestRiserFootprint.Footprint.Max(point => point.Y), -0.85f) &&
                Mathf.IsEqualApprox(layout.FrontGuestTopFootprint.Footprint.Max(point => point.Y), -0.55f),
            "guest countertop extends 0.30 metres toward customers without moving its riser");
        Require(layout.FrontBodyFootprint.TopY <= 0.09f,
            "front body polygon is only a thin base instead of a solid drawer-blocking prism");
        Require(layout.FrontCarcassParts.Count > 0 &&
                layout.Cabinets.Where(cabinet => cabinet.Kind == CabinetPartKind.Drawer)
                    .All(drawer => layout.FrontCarcassParts.All(part =>
                        !BoxesOverlap(drawer.Center, drawer.Size, part.Position, part.Size))),
            "authoritative hollow front carcass leaves every closed drawer panel clear");
        Require(layout.SinkPlumbingParts.Count >= 4 &&
                layout.SinkPlumbingParts.All(part => Contains(layout.SinkUnderClearVolume, part)),
            "exposed sink drain and supply pieces stay inside the open sink underbay");
        Require(layout.BottleRackBays.Count == 5 &&
                layout.BottleRackBays.All(bay => bay.Shelves.Count == 2),
            "five aligned two-level empty bottle rack bays are locked");

        var requiredStorageIds = new[]
        {
            "front_drawer_1_upper", "front_drawer_1_lower",
            "front_drawer_2_upper", "front_drawer_2_lower",
            "front_drawer_3_upper", "front_drawer_3_lower",
            "front_drawer_4_upper", "front_drawer_4_lower",
            "rear_lower_cabinet_1", "rear_lower_cabinet_2",
            "rear_lower_cabinet_3", "rear_lower_cabinet_4", "rear_lower_cabinet_5"
        };
        Require(requiredStorageIds.All(id => layout.Storages.Any(storage => storage.Id == id)) &&
                layout.ItemStorageAssignments.All(item =>
                    layout.Storages.Any(storage => storage.Id == item.StorageId)),
            "all required storage and item assignments resolve");
        var expectedStoredItems = new[]
        {
            "traditional_filter", "bean_scoop", "ice_tongs", "mortar", "pestle",
            "ice_bucket", "jigger_small", "jigger_medium", "jigger_large",
            "highball_glass", "coffee_beans", "kettle"
        };
        Require(layout.ItemStorageAssignments.Select(item => item.ItemId)
                .OrderBy(id => id, StringComparer.Ordinal)
                .SequenceEqual(expectedStoredItems.OrderBy(id => id, StringComparer.Ordinal)),
            "every approved tool and raw-material station is assigned exactly once");

        Require(Mathf.IsEqualApprox(layout.SouthMainEntry.Size.X, 1.40f) &&
                layout.SouthMainEntry.LeafCount == 2 &&
                layout.SouthMainEntry.OpenDirection.Z < -0.99f &&
                Mathf.IsEqualApprox(layout.NorthEastServiceDoor.Size.X, 0.90f) &&
                layout.NorthEastServiceDoor.OpenDirection.Z < -0.99f,
            "south double entry and north-east service door remain stable");
        Require(layout.Stations.Single(station => station.Kind == StationKind.HandWashSink).Position.X >
                BarLayoutDefinition.BarCenterX,
            "hand-wash sink is in the east front-bar region");
        Require(layout.Stations.Single(station => station.Kind == StationKind.WasteBin).Position.X >
                BarLayoutDefinition.BarCenterX,
            "waste zone is on the east side");

        var sink = layout.Stations.Single(station => station.Kind == StationKind.HandWashSink);
        Require(layout.Cabinets.All(cabinet =>
                !BoxesOverlap(cabinet.Center, cabinet.Size,
                    layout.SinkUnderClearVolume.Position, layout.SinkUnderClearVolume.Size)),
            "sink underbay is free of every cabinet and drawer");
        Require(layout.OperationManual.Position.X < BarLayoutDefinition.BarCenterX,
            "west side contains the operation manual");

        var pulledWestEdge = layout.LoungeChairs.Min(chair =>
            chair.PulledOutPosition.X - chair.Size.X * 0.5f);
        var barEastEdge = BarLayoutDefinition.BarCenterX + BarLayoutDefinition.FrontOutlineWidth * 0.5f;
        Require(pulledWestEdge >= barEastEdge + BarLayoutDefinition.MainCustomerRouteClearWidth,
            "pulled customer chairs preserve the 1.40 metre main route");
        Require(layout.LoungeChairs.All(chair =>
                !BoxesOverlap(chair.PulledOutPosition, chair.Size,
                    layout.SouthEntrySwingEnvelope.Position, layout.SouthEntrySwingEnvelope.Size)) &&
                layout.LoungeChairs.All(chair =>
                    !BoxesOverlap(chair.PulledOutPosition, chair.Size,
                        layout.SouthWindowAccessEnvelope.Position, layout.SouthWindowAccessEnvelope.Size)),
            "pulled customer chairs preserve entry and window access");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static bool BoxesOverlap(Vector3 firstCenter, Vector3 firstSize,
        Vector3 secondCenter, Vector3 secondSize) =>
        Math.Abs(firstCenter.X - secondCenter.X) * 2f < firstSize.X + secondSize.X &&
        Math.Abs(firstCenter.Y - secondCenter.Y) * 2f < firstSize.Y + secondSize.Y &&
        Math.Abs(firstCenter.Z - secondCenter.Z) * 2f < firstSize.Z + secondSize.Z;

    private static bool Contains(BarBoxLayout outer, BarBoxLayout inner)
    {
        var outerMin = outer.Position - outer.Size * 0.5f;
        var outerMax = outer.Position + outer.Size * 0.5f;
        var innerMin = inner.Position - inner.Size * 0.5f;
        var innerMax = inner.Position + inner.Size * 0.5f;
        return innerMin.X >= outerMin.X && innerMin.Y >= outerMin.Y && innerMin.Z >= outerMin.Z &&
               innerMax.X <= outerMax.X && innerMax.Y <= outerMax.Y && innerMax.Z <= outerMax.Z;
    }
}
