using Woordenspel.Domain.Tiles;
using Woordenspel.TestSupport;

namespace Woordenspel.Domain.Tests.Tiles;

public class RackTests
{
    [Fact]
    public void Take_removes_the_requested_tiles()
    {
        var rack = RackWith("KATTEN?");

        var taken = rack.Take(['t', 'T', '?']);

        Assert.Equal(3, taken.Count);
        Assert.Equal("KAEN", new string(rack.Tiles.Select(t => t.RackSymbol).ToArray()));
    }

    [Fact]
    public void Take_is_all_or_nothing()
    {
        var rack = RackWith("KAT");

        var exception = Assert.Throws<DomainException>(() => rack.Take(['K', 'A', 'A']));

        Assert.Equal(DomainErrors.TilesNotOnRack, exception.Code);
        Assert.Equal(3, rack.Count);
    }

    [Fact]
    public void Blank_must_be_requested_explicitly()
    {
        var rack = RackWith("?");

        Assert.Throws<DomainException>(() => rack.Find(['A']));
        Assert.Single(rack.Find(['?']));
    }

    [Fact]
    public void TotalPoints_sums_tile_values()
    {
        Assert.Equal(3 + 1 + 2 + 0, RackWith("KAT?").TotalPoints);
    }

    private static Rack RackWith(string symbols)
    {
        var rack = new Rack();
        rack.Add(TestGames.Tiles(symbols));
        return rack;
    }
}
