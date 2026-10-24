using Woordenspel.Client.Game;
using Woordenspel.Contracts;

namespace Woordenspel.Client.Tests.Game;

public class PlacementDraftTests
{
    private readonly PlacementDraft _draft = new();
    private readonly HashSet<(int, int)> _occupied = [(7, 7)];

    public PlacementDraftTests() => Load("KAT?");

    [Fact]
    public void Load_creates_one_slot_per_tile()
    {
        Assert.Equal("KAT?", Letters(_draft.Rack));
        Assert.False(_draft.HasPending);
    }

    [Fact]
    public void Selected_tile_can_be_placed_on_an_empty_square()
    {
        _draft.Select(0);

        Assert.True(_draft.PlaceSelected(7, 8));

        Assert.Equal(new PendingTile(0, 7, 8, 'K', false, 3), _draft.PendingAt(7, 8));
        Assert.True(_draft.IsOnBoard(0));
        Assert.Null(_draft.SelectedSlotId);
    }

    [Fact]
    public void Tiles_cannot_go_on_occupied_squares_or_off_the_board()
    {
        Assert.False(_draft.Place(0, 7, 7));
        Assert.False(_draft.Place(0, 15, 0));
        Assert.True(_draft.Place(0, 7, 8));
        Assert.False(_draft.Place(1, 7, 8));
    }

    [Fact]
    public void A_tile_can_only_be_placed_once()
    {
        _draft.Place(0, 7, 8);

        Assert.False(_draft.Place(0, 7, 9));
    }

    [Fact]
    public void Blank_needs_a_letter()
    {
        Assert.True(_draft.IsBlank(3));
        Assert.False(_draft.Place(3, 7, 8));

        Assert.True(_draft.Place(3, 7, 8, 'e'));

        Assert.Equal(new PendingTile(3, 7, 8, 'E', true, 0), _draft.PendingAt(7, 8));
    }

    [Fact]
    public void Clicking_the_selected_tile_again_deselects_it()
    {
        _draft.Select(1);
        _draft.Select(1);

        Assert.Null(_draft.SelectedSlotId);
    }

    [Fact]
    public void Recall_puts_tiles_back_on_the_rack()
    {
        _draft.Place(0, 7, 8);
        _draft.Place(1, 7, 9);

        _draft.Recall(7, 8);
        Assert.False(_draft.IsOnBoard(0));
        Assert.True(_draft.IsOnBoard(1));

        _draft.RecallAll();
        Assert.False(_draft.HasPending);
    }

    [Fact]
    public void ToPlacements_lists_the_pending_tiles_in_board_order()
    {
        _draft.Place(2, 7, 10);
        _draft.Place(0, 7, 8);
        _draft.Place(3, 7, 9, 'a');

        Assert.Equal(
            [new TilePlacementDto(7, 8, 'K'), new TilePlacementDto(7, 9, 'A', true), new TilePlacementDto(7, 10, 'T')],
            _draft.ToPlacements());
    }

    [Fact]
    public void Shuffle_changes_the_order_but_keeps_the_tiles()
    {
        _draft.Shuffle(new Random(1));

        Assert.Equal("?AKT", new string(Letters(_draft.Rack).Order().ToArray()));
        Assert.Equal(4, _draft.Rack.Select(s => s.Id).Distinct().Count());
    }

    [Fact]
    public void Swap_selection_toggles_and_gives_the_letters_to_swap()
    {
        _draft.ToggleSwap(0);
        _draft.ToggleSwap(3);
        _draft.ToggleSwap(0);

        Assert.Equal(['?'], _draft.SwapLetters());
    }

    [Fact]
    public void Placed_tiles_cannot_be_selected_for_swapping()
    {
        _draft.Place(0, 7, 8);

        _draft.ToggleSwap(0);

        Assert.Empty(_draft.SwapSelection);
    }

    [Fact]
    public void Same_rack_after_an_update_keeps_order_and_pending_tiles()
    {
        _draft.Shuffle(new Random(1));
        var order = Letters(_draft.Rack);
        _draft.Place(0, 7, 8);
        _draft.Place(1, 3, 3);

        _occupied.Add((3, 3)); // de tegenstander legde daar een steen
        Load("TAK?");

        Assert.Equal(order, Letters(_draft.Rack));
        Assert.NotNull(_draft.PendingAt(7, 8));
        Assert.Null(_draft.PendingAt(3, 3));
    }

    [Fact]
    public void New_rack_after_a_move_starts_fresh()
    {
        _draft.Place(0, 7, 8);
        _draft.Select(1);

        Load("EENZAAM");

        Assert.Equal("EENZAAM", Letters(_draft.Rack));
        Assert.False(_draft.HasPending);
        Assert.Null(_draft.SelectedSlotId);
    }

    private void Load(string rack) =>
        _draft.Load(
            rack.Select(c => new RackTileDto(c, c == '?', c switch { '?' => 0, 'K' => 3, 'T' => 2, _ => 1 })).ToList(),
            15,
            (row, column) => _occupied.Contains((row, column)));

    private static string Letters(IEnumerable<RackSlot> slots) => new(slots.Select(s => s.Tile.Letter).ToArray());
}
