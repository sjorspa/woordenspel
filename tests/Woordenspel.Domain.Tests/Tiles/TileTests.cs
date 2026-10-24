using Woordenspel.Domain.Tiles;

namespace Woordenspel.Domain.Tests.Tiles;

public class TileTests
{
    [Fact]
    public void ForLetter_normalizes_to_upper_case()
    {
        var tile = Tile.ForLetter('k', 3);

        Assert.Equal('K', tile.Letter);
        Assert.Equal(3, tile.Points);
        Assert.False(tile.IsBlank);
    }

    [Theory]
    [InlineData('1')]
    [InlineData('é')]
    [InlineData('?')]
    public void ForLetter_rejects_non_letters(char symbol)
    {
        var exception = Assert.Throws<DomainException>(() => Tile.ForLetter(symbol, 1));
        Assert.Equal(DomainErrors.InvalidLetter, exception.Code);
    }

    [Fact]
    public void Blank_gets_a_letter_but_stays_worth_zero_points()
    {
        var tile = Tile.Blank().WithBlankLetter('q');

        Assert.Equal('Q', tile.Letter);
        Assert.Equal(0, tile.Points);
        Assert.True(tile.IsBlank);
        Assert.False(tile.IsUnassignedBlank);
        Assert.Equal(Tile.BlankSymbol, tile.RackSymbol);
    }

    [Fact]
    public void Only_blanks_can_get_a_letter()
    {
        Assert.Throws<InvalidOperationException>(() => Tile.ForLetter('A', 1).WithBlankLetter('B'));
    }
}
