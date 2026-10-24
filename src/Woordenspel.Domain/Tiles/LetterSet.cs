namespace Woordenspel.Domain.Tiles;

public readonly record struct LetterInfo(char Letter, int Count, int Points);

/// <summary>De verdeling van letters (aantal en puntwaarde) waarmee de letterzak wordt gevuld.</summary>
public sealed class LetterSet
{
    private readonly Dictionary<char, LetterInfo> _letters;

    public LetterSet(IEnumerable<LetterInfo> letters, int blankCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(blankCount);

        _letters = [];
        foreach (var info in letters)
        {
            var letter = Alphabet.Normalize(info.Letter);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(info.Count);
            ArgumentOutOfRangeException.ThrowIfNegative(info.Points);
            if (!_letters.TryAdd(letter, info with { Letter = letter }))
            {
                throw new ArgumentException($"Letter '{letter}' komt meerdere keren voor.", nameof(letters));
            }
        }

        BlankCount = blankCount;
    }

    /// <summary>De Nederlandse Wordfeud-verdeling.</summary>
    public static LetterSet Dutch { get; } = new(
        [
            new('A', 7, 1), new('B', 2, 4), new('C', 2, 5), new('D', 5, 2), new('E', 18, 1),
            new('F', 2, 4), new('G', 3, 3), new('H', 2, 4), new('I', 4, 2), new('J', 2, 4),
            new('K', 3, 3), new('L', 3, 3), new('M', 3, 3), new('N', 10, 1), new('O', 6, 1),
            new('P', 2, 4), new('Q', 1, 10), new('R', 5, 2), new('S', 5, 2), new('T', 5, 2),
            new('U', 3, 2), new('V', 2, 4), new('W', 2, 5), new('X', 1, 8), new('Y', 1, 8),
            new('Z', 2, 5),
        ],
        blankCount: 2);

    public IReadOnlyCollection<LetterInfo> Letters => _letters.Values;

    public int BlankCount { get; }

    public int TotalTiles => _letters.Values.Sum(l => l.Count) + BlankCount;

    public int PointsFor(char letter)
    {
        var normalized = Alphabet.Normalize(letter);
        return _letters.TryGetValue(normalized, out var info)
            ? info.Points
            : throw new DomainException(DomainErrors.InvalidLetter, $"De letter '{normalized}' zit niet in deze letterset.");
    }

    public Tile CreateTile(char symbol) =>
        symbol == Tile.BlankSymbol ? Tile.Blank() : Tile.ForLetter(symbol, PointsFor(symbol));

    public IEnumerable<Tile> CreateTiles()
    {
        foreach (var info in _letters.Values.OrderBy(l => l.Letter))
        {
            for (var i = 0; i < info.Count; i++)
            {
                yield return Tile.ForLetter(info.Letter, info.Points);
            }
        }

        for (var i = 0; i < BlankCount; i++)
        {
            yield return Tile.Blank();
        }
    }
}
