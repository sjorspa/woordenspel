using Woordenspel.Domain.Boards;
using Woordenspel.Domain.Games;
using Woordenspel.Domain.Moves;
using Woordenspel.Domain.Tiles;
using Woordenspel.Domain.Words;
using Woordenspel.TestSupport;
using static Woordenspel.Domain.Boards.Direction;

namespace Woordenspel.Domain.Tests.Moves;

public class PlacementEvaluatorTests
{
    private readonly Board _board = new(BoardLayout.Standard);

    // --- Eerste zet ---

    [Fact]
    public void First_word_over_center_scores_letter_values()
    {
        var result = Evaluate(TestGames.Word("KAT", 7, 7));

        var word = Assert.Single(result.Words);
        Assert.Equal("KAT", word.Text);
        Assert.Equal(3 + 1 + 2, result.Score);
        Assert.False(result.IsBingo);
    }

    [Fact]
    public void First_word_must_cover_center()
    {
        AssertRejected(DomainErrors.MustCoverCenter, TestGames.Word("KAT", 0, 0));
    }

    [Fact]
    public void First_move_needs_at_least_two_letters()
    {
        AssertRejected(DomainErrors.WordTooShort, TestGames.Word("K", 7, 7));
    }

    // --- Vorm van de zet ---

    [Fact]
    public void Tiles_must_be_in_one_line()
    {
        AssertRejected(DomainErrors.NotInLine, [Placement('K', 7, 7), Placement('A', 8, 8)]);
    }

    [Fact]
    public void Tiles_may_not_have_gaps()
    {
        AssertRejected(DomainErrors.NotContiguous, [Placement('K', 7, 7), Placement('T', 7, 9)]);
    }

    [Fact]
    public void Gaps_may_be_filled_by_tiles_already_on_the_board()
    {
        TestGames.PlaceWord(_board, "KAT", 7, 7);

        var result = Evaluate([Placement('S', 7, 6), Placement('S', 7, 10)]);

        Assert.Equal("SKATS", Assert.Single(result.Words).Text);
    }

    [Fact]
    public void Tiles_must_connect_to_existing_tiles()
    {
        TestGames.PlaceWord(_board, "KAT", 7, 7);

        AssertRejected(DomainErrors.NotConnected, TestGames.Word("EN", 0, 0));
    }

    [Fact]
    public void Tiles_must_be_placed_on_the_board()
    {
        AssertRejected(DomainErrors.OutsideBoard, TestGames.Word("KAT", 7, 13));
    }

    [Fact]
    public void Tiles_may_not_be_placed_on_occupied_squares()
    {
        TestGames.PlaceWord(_board, "KAT", 7, 7);

        AssertRejected(DomainErrors.SquareOccupied, TestGames.Word("EN", 7, 9, Vertical));
    }

    [Fact]
    public void Two_tiles_may_not_share_a_square()
    {
        AssertRejected(DomainErrors.DuplicatePosition, [Placement('K', 7, 7), Placement('A', 7, 7)]);
    }

    [Fact]
    public void At_least_one_tile_must_be_placed()
    {
        AssertRejected(DomainErrors.NoTiles, Array.Empty<PlacedTile>());
    }

    [Fact]
    public void Blank_needs_a_letter()
    {
        AssertRejected(DomainErrors.BlankLetterRequired, [new PlacedTile(new Position(7, 7), Tile.Blank())]);
    }

    // --- Woorden ---

    [Fact]
    public void All_formed_words_must_be_in_the_dictionary()
    {
        TestGames.PlaceWord(_board, "KAT", 7, 7);
        var evaluator = new PlacementEvaluator(new FakeWordDictionary("AT"), GameRules.Default);

        var exception = Assert.Throws<InvalidWordsException>(
            () => evaluator.Evaluate(_board, ToPlacedTiles(TestGames.Word("AT", 8, 8))));

        Assert.Equal(["AA", "TT"], exception.InvalidWords);
    }

    [Fact]
    public void Single_tile_can_extend_a_word_vertically()
    {
        TestGames.PlaceWord(_board, "KAT", 7, 7);

        var result = Evaluate([Placement('A', 8, 8)]);

        var word = Assert.Single(result.Words);
        Assert.Equal("AA", word.Text);
        Assert.Equal(Vertical, word.Direction);
        Assert.Equal(new Position(7, 8), word.Start);
    }

    [Fact]
    public void Cross_words_are_found_and_scored()
    {
        TestGames.PlaceWord(_board, "KAT", 7, 7);

        var result = Evaluate(TestGames.Word("AT", 8, 8));

        Assert.Equal(
            [("AT", 3), ("AA", 2), ("TT", 4)],
            result.Words.Select(w => (w.Text, w.Score)).OrderBy(w => w.Text == "AT" ? 0 : 1).ThenBy(w => w.Text));
        Assert.Equal(9, result.Score);
    }

    // --- Score ---

    [Fact]
    public void Double_word_square_doubles_the_word()
    {
        TestGames.PlaceWord(_board, "KAT", 7, 7);

        // E komt op (7,11): dubbel woord.
        var result = Evaluate(TestGames.Word("TEN", 7, 10));

        Assert.Equal("KATTEN", Assert.Single(result.Words).Text);
        Assert.Equal((3 + 1 + 2 + 2 + 1 + 1) * 2, result.Score);
    }

    [Fact]
    public void Triple_letter_square_triples_the_letter()
    {
        TestGames.PlaceWord(_board, "KAT", 7, 7);

        // B komt op (5,9): drievoudige letter.
        var result = Evaluate(TestGames.Word("BA", 5, 9, Vertical));

        Assert.Equal("BAT", Assert.Single(result.Words).Text);
        Assert.Equal(4 * 3 + 1 + 2, result.Score);
    }

    [Fact]
    public void Bonus_squares_only_count_for_newly_placed_tiles()
    {
        TestGames.PlaceWord(_board, "KATTEN", 7, 7); // E ligt al op dubbel woord (7,11)

        var result = Evaluate([Placement('B', 6, 11), Placement('D', 8, 11)]);

        Assert.Equal("BED", Assert.Single(result.Words).Text);
        Assert.Equal(4 + 1 + 2, result.Score);
    }

    [Fact]
    public void Blank_tiles_are_worth_zero_points()
    {
        var result = Evaluate(TestGames.Word("KaT", 7, 7));

        Assert.Equal("KAT", Assert.Single(result.Words).Text);
        Assert.Equal(3 + 0 + 2, result.Score);
    }

    [Fact]
    public void Using_all_seven_tiles_earns_the_bingo_bonus()
    {
        var result = Evaluate(TestGames.Word("WOORDEN", 7, 4));

        Assert.True(result.IsBingo);
        Assert.Equal(5 + 1 + 1 + 2 + 2 + 1 + 1 + GameRules.Default.BingoBonus, result.Score);
    }

    private PlacementResult Evaluate(IReadOnlyList<TilePlacement> placements) =>
        Evaluate(ToPlacedTiles(placements));

    private PlacementResult Evaluate(IReadOnlyList<PlacedTile> placements) =>
        new PlacementEvaluator(new AnyWordDictionary(), GameRules.Default).Evaluate(_board, placements);

    private void AssertRejected(string expectedCode, IReadOnlyList<TilePlacement> placements) =>
        AssertRejected(expectedCode, ToPlacedTiles(placements));

    private void AssertRejected(string expectedCode, IReadOnlyList<PlacedTile> placements)
    {
        var exception = Assert.Throws<DomainException>(() => Evaluate(placements));
        Assert.Equal(expectedCode, exception.Code);
    }

    private static TilePlacement Placement(char letter, int row, int column) => new(new Position(row, column), letter);

    private static List<PlacedTile> ToPlacedTiles(IEnumerable<TilePlacement> placements) =>
        placements
            .Select(p => new PlacedTile(
                p.Position,
                p.IsBlank ? Tile.Blank().WithBlankLetter(p.Letter) : LetterSet.Dutch.CreateTile(p.Letter)))
            .ToList();

    private sealed class AnyWordDictionary : IWordDictionary
    {
        public bool Contains(string word) => true;
    }
}
