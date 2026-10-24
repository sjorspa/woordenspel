using Woordenspel.Domain.Boards;
using Woordenspel.Domain.Tiles;

namespace Woordenspel.Domain.Tests.Boards;

public class BoardTests
{
    private readonly Board _board = new(BoardLayout.Standard);

    [Fact]
    public void New_board_is_empty()
    {
        Assert.True(_board.IsEmpty);
        Assert.Empty(_board.GetTiles());
    }

    [Fact]
    public void Place_puts_tile_on_square()
    {
        var position = new Position(3, 4);
        var tile = Tile.ForLetter('A', 1);

        _board.Place(position, tile);

        Assert.False(_board.IsEmpty);
        Assert.Equal(tile, _board.GetTile(position));
        Assert.Equal(new BoardTile(position, tile), Assert.Single(_board.GetTiles()));
    }

    [Fact]
    public void Place_rejects_occupied_square()
    {
        var position = new Position(3, 4);
        _board.Place(position, Tile.ForLetter('A', 1));

        var exception = Assert.Throws<DomainException>(() => _board.Place(position, Tile.ForLetter('B', 4)));
        Assert.Equal(DomainErrors.SquareOccupied, exception.Code);
    }

    [Fact]
    public void Place_rejects_unassigned_blank()
    {
        var exception = Assert.Throws<DomainException>(() => _board.Place(new Position(0, 0), Tile.Blank()));
        Assert.Equal(DomainErrors.BlankLetterRequired, exception.Code);
    }

    [Fact]
    public void GetTile_outside_board_returns_null()
    {
        Assert.Null(_board.GetTile(new Position(-1, 0)));
        Assert.Null(_board.GetTile(new Position(0, 15)));
    }
}
