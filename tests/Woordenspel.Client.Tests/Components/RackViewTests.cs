using Woordenspel.Client.Components;
using Woordenspel.Client.Game;
using Woordenspel.Contracts;

namespace Woordenspel.Client.Tests.Components;

public class RackViewTests : BunitContext
{
    private readonly PlacementDraft _draft = new();

    public RackViewTests() =>
        _draft.Load([new RackTileDto('K', false, 3), new RackTileDto('?', true, 0), new RackTileDto('A', false, 1)], 15, (_, _) => false);

    [Fact]
    public void Shows_letters_and_points_and_an_empty_blank()
    {
        var rack = Render<RackView>(p => p.Add(r => r.Draft, _draft));

        var tiles = rack.FindAll(".rack-slot .tile");
        Assert.Equal(["K3", "", "A1"], tiles.Select(t => t.TextContent.Replace(" ", "").Replace("\n", "").Trim()));
    }

    [Fact]
    public void Tiles_on_the_board_leave_an_empty_slot()
    {
        _draft.Place(0, 7, 7);

        var rack = Render<RackView>(p => p.Add(r => r.Draft, _draft));

        Assert.Single(rack.FindAll(".rack-slot.empty"));
        Assert.Equal(2, rack.FindAll(".rack-slot .tile").Count);
    }

    [Fact]
    public void Selected_and_swap_tiles_are_marked()
    {
        _draft.Select(0);
        _draft.ToggleSwap(2);

        var rack = Render<RackView>(p => p.Add(r => r.Draft, _draft));

        Assert.Contains("selected", rack.Find("[data-slot='0']").ClassList);
        Assert.Contains("to-swap", rack.Find("[data-slot='2']").ClassList);
    }

    [Fact]
    public void Clicking_a_tile_reports_its_slot()
    {
        int? clicked = null;
        var rack = Render<RackView>(p => p.Add(r => r.Draft, _draft).Add(r => r.OnSlotClick, (int id) => clicked = id));

        rack.Find("[data-slot='2']").Click();

        Assert.Equal(2, clicked);
    }
}
