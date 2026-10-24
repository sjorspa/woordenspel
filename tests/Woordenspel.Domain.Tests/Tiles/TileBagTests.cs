using Woordenspel.Domain.Tiles;
using Woordenspel.TestSupport;

namespace Woordenspel.Domain.Tests.Tiles;

public class TileBagTests
{
    [Fact]
    public void Draw_takes_tiles_from_the_front()
    {
        var bag = TestGames.Bag("KATTEN");

        var drawn = bag.Draw(3);

        Assert.Equal("KAT", Symbols(drawn));
        Assert.Equal(3, bag.Count);
    }

    [Fact]
    public void Draw_returns_fewer_tiles_when_bag_runs_out()
    {
        var bag = TestGames.Bag("AB");

        var drawn = bag.Draw(7);

        Assert.Equal(2, drawn.Count);
        Assert.Equal(0, bag.Count);
        Assert.Empty(bag.Draw(1));
    }

    [Fact]
    public void Return_puts_tiles_back()
    {
        var bag = TestGames.Bag("AB");

        bag.Return(TestGames.Tiles("XY"));

        Assert.Equal(4, bag.Count);
    }

    [Fact]
    public void Constructor_shuffles_with_the_randomizer()
    {
        // Kiest steeds de laatste steen: ABC -> CBA -> CAB.
        var bag = new TileBag(TestGames.Tiles("ABC"), new LastChoiceRandomizer());

        Assert.Equal("CAB", Symbols(bag.Draw(3)));
    }

    [Fact]
    public void Randomizer_returning_zero_keeps_the_order()
    {
        var bag = new TileBag(TestGames.Tiles("ABC"), new PredictableRandomizer());

        Assert.Equal("ABC", Symbols(bag.Draw(3)));
    }

    private static string Symbols(IEnumerable<Tile> tiles) => new(tiles.Select(t => t.RackSymbol).ToArray());

    private sealed class LastChoiceRandomizer : IRandomizer
    {
        public int Next(int maxExclusive) => maxExclusive - 1;
    }
}
