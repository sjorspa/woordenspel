using Woordenspel.Domain.Boards;
using Woordenspel.Domain.Games;
using Woordenspel.Domain.Moves;
using Woordenspel.Domain.Tiles;

namespace Woordenspel.TestSupport;

public static class TestGames
{
    /// <summary>Maakt stenen met Nederlandse puntwaarden; '?' is een blanco steen.</summary>
    public static IEnumerable<Tile> Tiles(string symbols) => symbols.Select(LetterSet.Dutch.CreateTile);

    public static TileBag Bag(string symbols) => new(Tiles(symbols), new PredictableRandomizer());

    /// <summary>
    /// Start een spel voor twee spelers met bekende plankjes. De zak bevat daarna precies <paramref name="restOfBag"/>.
    /// </summary>
    public static Game Start(string rack1, string rack2, string restOfBag = "", GameRules? rules = null)
    {
        rules ??= GameRules.Default;
        if (rack1.Length != rules.RackSize || rack2.Length != rules.RackSize)
        {
            throw new ArgumentException($"Een plankje moet {rules.RackSize} stenen hebben.");
        }

        return Game.Start(
            Guid.NewGuid(), Players("Anna", "Bram"), rules, BoardLayout.Standard, Bag(rack1 + rack2 + restOfBag),
            new PredictableRandomizer(), Now);
    }

    public static IReadOnlyList<PlayerIdentity> Players(params string[] names) =>
        names.Select(name => new PlayerIdentity(Guid.NewGuid(), name)).ToList();

    /// <summary>Het tijdstip waarop <see cref="Start"/> het spel begint.</summary>
    public static DateTimeOffset Now => FakeTimeProvider.Start;

    /// <summary>Plaatsingen voor een woord vanaf (row, column); een kleine letter is een blanco steen.</summary>
    public static IReadOnlyList<TilePlacement> Word(string word, int row, int column, Direction direction = Direction.Horizontal)
    {
        var position = new Position(row, column);
        var placements = new List<TilePlacement>();
        foreach (var letter in word)
        {
            placements.Add(new TilePlacement(position, letter, IsBlank: char.IsLower(letter)));
            position = position.Next(direction);
        }

        return placements;
    }

    /// <summary>Legt een woord direct op een bord, buiten de spelregels om.</summary>
    public static void PlaceWord(Board board, string word, int row, int column, Direction direction = Direction.Horizontal)
    {
        foreach (var placement in Word(word, row, column, direction))
        {
            var tile = placement.IsBlank
                ? Tile.Blank().WithBlankLetter(placement.Letter)
                : LetterSet.Dutch.CreateTile(placement.Letter);
            board.Place(placement.Position, tile);
        }
    }
}
