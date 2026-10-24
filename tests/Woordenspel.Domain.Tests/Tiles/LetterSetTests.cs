using Woordenspel.Domain.Tiles;

namespace Woordenspel.Domain.Tests.Tiles;

public class LetterSetTests
{
    [Fact]
    public void Dutch_set_contains_every_letter_of_the_alphabet()
    {
        var letters = LetterSet.Dutch.Letters.Select(l => l.Letter).Order();

        Assert.Equal("ABCDEFGHIJKLMNOPQRSTUVWXYZ", new string(letters.ToArray()));
    }

    [Theory]
    [InlineData('E', 1)]
    [InlineData('D', 2)]
    [InlineData('K', 3)]
    [InlineData('J', 4)]
    [InlineData('Z', 5)]
    [InlineData('X', 8)]
    [InlineData('Q', 10)]
    public void Dutch_set_has_wordfeud_letter_values(char letter, int points)
    {
        Assert.Equal(points, LetterSet.Dutch.PointsFor(letter));
    }

    [Fact]
    public void CreateTiles_creates_all_letters_and_blanks()
    {
        var tiles = LetterSet.Dutch.CreateTiles().ToList();

        Assert.Equal(LetterSet.Dutch.TotalTiles, tiles.Count);
        Assert.Equal(18, tiles.Count(t => t.Letter == 'E'));
        Assert.Equal(2, tiles.Count(t => t.IsUnassignedBlank));
    }

    [Fact]
    public void CreateTile_supports_blank_symbol()
    {
        Assert.True(LetterSet.Dutch.CreateTile('?').IsUnassignedBlank);
        Assert.Equal(Tile.ForLetter('W', 5), LetterSet.Dutch.CreateTile('w'));
    }

    [Fact]
    public void Duplicate_letters_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => new LetterSet([new('A', 1, 1), new('a', 2, 1)], blankCount: 0));
    }
}
