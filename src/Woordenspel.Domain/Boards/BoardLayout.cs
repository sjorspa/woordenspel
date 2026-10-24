namespace Woordenspel.Domain.Boards;

public enum SquareBonus
{
    None,
    DoubleLetter,
    TripleLetter,
    DoubleWord,
    TripleWord,
}

public static class SquareBonusExtensions
{
    public static int LetterMultiplier(this SquareBonus bonus) => bonus switch
    {
        SquareBonus.DoubleLetter => 2,
        SquareBonus.TripleLetter => 3,
        _ => 1,
    };

    public static int WordMultiplier(this SquareBonus bonus) => bonus switch
    {
        SquareBonus.DoubleWord => 2,
        SquareBonus.TripleWord => 3,
        _ => 1,
    };
}

/// <summary>De indeling van het bord: grootte, startvakje en premievakjes.</summary>
public sealed class BoardLayout
{
    private readonly SquareBonus[,] _bonuses;

    private BoardLayout(SquareBonus[,] bonuses, Position center)
    {
        _bonuses = bonuses;
        Center = center;
    }

    /// <summary>Het standaard Wordfeud-bord.</summary>
    public static BoardLayout Standard { get; } = Parse(
    [
        "t...T..d..T...t",
        ".d...t...t...d.",
        "..D...d.d...D..",
        "...t...D...t...",
        "T...D.d.d.D...T",
        ".t...t...t...t.",
        "..d.d.....d.d..",
        "d..D...*...D..d",
        "..d.d.....d.d..",
        ".t...t...t...t.",
        "T...D.d.d.D...T",
        "...t...D...t...",
        "..D...d.d...D..",
        ".d...t...t...d.",
        "t...T..d..T...t",
    ]);

    public int Size => _bonuses.GetLength(0);

    public Position Center { get; }

    public bool IsInside(Position position) =>
        position.Row >= 0 && position.Row < Size && position.Column >= 0 && position.Column < Size;

    public SquareBonus GetBonus(Position position)
    {
        if (!IsInside(position))
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "Positie ligt buiten het bord.");
        }

        return _bonuses[position.Row, position.Column];
    }

    /// <summary>De indeling in het formaat van <see cref="Parse"/>.</summary>
    public IReadOnlyList<string> ToRows() =>
        Enumerable.Range(0, Size)
            .Select(row => new string(Enumerable.Range(0, Size).Select(column => ToSymbol(new Position(row, column))).ToArray()))
            .ToList();

    private char ToSymbol(Position position) =>
        position == Center
            ? '*'
            : GetBonus(position) switch
            {
                SquareBonus.DoubleLetter => 'd',
                SquareBonus.TripleLetter => 't',
                SquareBonus.DoubleWord => 'D',
                SquareBonus.TripleWord => 'T',
                _ => '.',
            };

    /// <summary>
    /// Leest een vierkante indeling. Tekens: '.' geen bonus, 'd' dubbele letter, 't' driedubbele letter,
    /// 'D' dubbel woord, 'T' driedubbel woord, '*' startvakje (precies één keer, zonder bonus).
    /// </summary>
    public static BoardLayout Parse(IReadOnlyList<string> rows)
    {
        var size = rows.Count;
        if (size == 0 || rows.Any(r => r.Length != size))
        {
            throw new FormatException("Een bordindeling moet vierkant zijn.");
        }

        var bonuses = new SquareBonus[size, size];
        Position? center = null;

        for (var row = 0; row < size; row++)
        {
            for (var column = 0; column < size; column++)
            {
                var symbol = rows[row][column];
                if (symbol == '*')
                {
                    if (center is not null)
                    {
                        throw new FormatException("Een bordindeling mag maar één startvakje hebben.");
                    }

                    center = new Position(row, column);
                }

                bonuses[row, column] = symbol switch
                {
                    '.' or '*' => SquareBonus.None,
                    'd' => SquareBonus.DoubleLetter,
                    't' => SquareBonus.TripleLetter,
                    'D' => SquareBonus.DoubleWord,
                    'T' => SquareBonus.TripleWord,
                    _ => throw new FormatException($"Onbekend teken '{symbol}' in bordindeling."),
                };
            }
        }

        return new BoardLayout(bonuses, center ?? throw new FormatException("Een bordindeling moet een startvakje ('*') hebben."));
    }
}
