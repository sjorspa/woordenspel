using Woordenspel.Client.Components;
using Woordenspel.Client.Game;
using Woordenspel.Contracts;
using static Woordenspel.Client.Tests.TestData;

namespace Woordenspel.Client.Tests.Components;

public class BoardViewTests : BunitContext
{
    [Fact]
    public void Renders_every_square_with_bonus_labels_and_the_start_square()
    {
        var board = Render<BoardView>(p => p.Add(b => b.Setup, Setup).Add(b => b.Board, Board([])));

        Assert.Equal(225, board.FindAll(".square").Count);
        Assert.Equal("TW", Square(board, 0, 0).TextContent.Trim());
        Assert.Contains("tw", Square(board, 0, 0).ClassList);
        Assert.Equal("DL", Square(board, 1, 1).TextContent.Trim());
        Assert.Equal("★", Square(board, 7, 7).TextContent.Trim());
    }

    [Fact]
    public void Shows_tiles_pending_tiles_and_the_last_move()
    {
        var draft = new PlacementDraft();
        draft.Load([new RackTileDto('A', false, 1)], 15, (_, _) => false);
        draft.Place(0, 8, 8);

        var board = Render<BoardView>(p => p
            .Add(b => b.Setup, Setup)
            .Add(b => b.Board, Board([new BoardTileDto(7, 7, 'K', false, 3), new BoardTileDto(7, 8, 'A', true, 0)]))
            .Add(b => b.Draft, draft)
            .Add(b => b.Highlight, new HashSet<BoardPosition> { new(7, 7) }));

        Assert.Equal("K3", Square(board, 7, 7).TextContent.Replace(" ", "").Trim());
        Assert.NotNull(Square(board, 7, 7).QuerySelector(".tile.last-move"));
        Assert.NotNull(Square(board, 7, 8).QuerySelector(".tile.blank"));
        Assert.NotNull(Square(board, 8, 8).QuerySelector(".tile.pending"));
    }

    [Fact]
    public void Clicking_a_square_reports_its_position()
    {
        BoardPosition? clicked = null;
        var board = Render<BoardView>(p => p
            .Add(b => b.Setup, Setup)
            .Add(b => b.Board, Board([]))
            .Add(b => b.OnSquareClick, (BoardPosition position) => clicked = position));

        Square(board, 3, 4).Click();

        Assert.Equal(new BoardPosition(3, 4), clicked);
    }

    private static AngleSharp.Dom.IElement Square(IRenderedComponent<BoardView> board, int row, int column) =>
        board.Find($"[data-row='{row}'][data-column='{column}']");
}
