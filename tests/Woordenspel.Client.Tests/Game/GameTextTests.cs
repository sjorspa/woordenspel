using Woordenspel.Client.Game;
using Woordenspel.Contracts;
using Woordenspel.Domain.Games;
using static Woordenspel.Client.Tests.TestData;

namespace Woordenspel.Client.Tests.Game;

public class GameTextTests
{
    [Theory]
    [InlineData(MoveType.Placement, "anna: KAT, AT (9)")]
    [InlineData(MoveType.Swap, "anna ruilde 3 stenen")]
    [InlineData(MoveType.Pass, "anna paste")]
    [InlineData(MoveType.Resign, "anna gaf op")]
    [InlineData(MoveType.Timeout, "anna was door de tijd heen")]
    public void Describe_moves(MoveType type, string expected)
    {
        var move = new MoveDto(Anna, type, 9, type == MoveType.Placement ? ["KAT", "AT"] : [], 3, []);

        Assert.Equal(expected, GameText.Describe(move, "anna"));
    }

    [Fact]
    public void Result_from_the_point_of_view_of_the_viewer()
    {
        var players = GameState().Players;

        Assert.Equal("Je hebt gewonnen!", GameText.Result(players, [Anna], Anna));
        Assert.Equal("bram heeft gewonnen.", GameText.Result(players, [Bram], Anna));
        Assert.Equal("Gelijkspel!", GameText.Result(players, [Anna, Bram], Anna));
    }

    [Fact]
    public void Preview_lists_words_and_total()
    {
        Assert.Equal("KAT (6) + AT (3) = 9 punten", GameText.Preview(new PreviewDto(9, false, [new("KAT", 6), new("AT", 3)])));
        Assert.Equal("KATTEN (10) + bingo = 50 punten", GameText.Preview(new PreviewDto(50, true, [new("KATTEN", 10)])));
    }
}
