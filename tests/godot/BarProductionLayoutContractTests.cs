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

        Require(layout.RoomClearSize.IsEqualApprox(new Vector3(12f, 3.5f, 9f)),
            "room is 12 by 9 by 3.5 metres");
        Require(Mathf.IsEqualApprox(BarLayoutDefinition.FrontBarTopHeight, 1.20f) &&
                Mathf.IsEqualApprox(BarLayoutDefinition.PlayerWorktopHeight, 0.96f) &&
                Mathf.IsEqualApprox(BarLayoutDefinition.OperationAisleClearWidth, 1.40f) &&
                Mathf.IsEqualApprox(BarLayoutDefinition.DrawerOpenTravel, 0.38f),
            "gameplay scale uses the approved compromise values");
        Require(layout.SouthWindows.Count == 1 &&
                layout.SouthWindows[0].Size.IsEqualApprox(
                    new Vector3(3.20f, 1.55f, BarLayoutDefinition.WallThickness)) &&
                Mathf.IsEqualApprox(layout.SouthWindows[0].SillHeight, 0.75f),
            "south elevation has one east landscape window");
        Require(layout.Cabinets.Count(cabinet => cabinet.Kind == CabinetPartKind.Drawer) == 8 &&
                layout.Cabinets.Single(cabinet => cabinet.ContainsIceBucket).Id == "front_drawer_2_upper",
            "front bar keeps eight drawers and the stable ice drawer ID");
        Require(layout.FrontStools.Count == 6 && layout.LoungeTables.Count == 3 &&
                layout.LoungeChairs.Count == 12 && layout.Booths.Count == 0,
            "customer furniture counts are locked");
        Require(layout.PendantFixtures.Count == 3 && layout.RearLinearFixtures.Count == 2 &&
                layout.CustomerSconces.Count == 4 && layout.CustomerFillLights.Count == 2,
            "all approved lighting groups are represented");
        Require(layout.FrontFootrails.Count == 0,
            "the front bar has no attached footrail or fixed footboard");
        Require(layout.PlayerFacingDirection.Z > 0.99f &&
                BarLayoutDefinition.RearBarFrontZ < BarLayoutDefinition.FrontBarInnerEdgeZ,
            "rear bar is north, front bar is south, and player faces customers");
        Require(Mathf.IsEqualApprox(layout.FrontBarTop.Size.X, 5.60f) &&
                Mathf.IsEqualApprox(layout.FrontBarBody.Size.Z, 0.80f) &&
                Mathf.IsEqualApprox(
                    BarLayoutDefinition.PlayerSurfaceDepth + BarLayoutDefinition.GuestSurfaceDepth,
                    BarLayoutDefinition.FrontSectionDepth),
            "front bar outline and 0.62 plus 0.18 metre depth split are locked");
        Require(Mathf.IsEqualApprox(layout.CounterReturns[0].Size.X, 0.65f) &&
                Mathf.IsEqualApprox(layout.CounterReturns[1].Size.X, 0.80f) &&
                Mathf.IsEqualApprox(
                    layout.CounterReturns[1].Position.X - layout.CounterReturns[1].Size.X * 0.5f -
                    (layout.CounterReturns[0].Position.X + layout.CounterReturns[0].Size.X * 0.5f),
                    4.15f),
            "asymmetric dry and wet returns preserve the internal clear span");
        Require(BarLayoutDefinition.FrontFacadeBayCount == 3 &&
                Mathf.IsEqualApprox(BarLayoutDefinition.HandoffStripWidth, 0.90f),
            "front facade keeps three bays and the central handoff strip");
        Require(Mathf.IsEqualApprox(layout.UpperBackCabinet.Size.Z, 0.38f) &&
                Mathf.IsEqualApprox(
                    layout.UpperBackCabinet.Position.Y - layout.UpperBackCabinet.Size.Y * 0.5f,
                    2.10f),
            "upper cabinet depth and lower edge are locked");
        Require(layout.BottleRackShelves.Count == 2 &&
                Mathf.IsEqualApprox(layout.BottleRackShelves[0].Position.Y, 1.34f) &&
                Mathf.IsEqualApprox(layout.BottleRackShelves[1].Position.Y, 1.68f),
            "bottle-rack levels are locked");
        Require(Mathf.IsEqualApprox(layout.SouthMainEntry.Size.X, 1.40f) &&
                layout.SouthMainEntry.LeafCount == 2 &&
                layout.SouthMainEntry.OpenDirection.Z < -0.99f &&
                Mathf.IsEqualApprox(layout.NorthEastServiceDoor.Size.X, 0.90f) &&
                layout.NorthEastServiceDoor.OpenDirection.Z < -0.99f &&
                layout.NorthEastServiceDoor.Position.X > 0f &&
                layout.NorthEastServiceDoor.Position.Z < 0f,
            "south double entry and north-east service door are locked");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
