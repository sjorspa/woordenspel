namespace Woordenspel.Domain.Tiles;

/// <summary>
/// Een speelsteen. Een blanco steen heeft op het plankje het symbool <see cref="BlankSymbol"/>
/// en krijgt pas een letter (met 0 punten) wanneer hij wordt gelegd.
/// </summary>
public sealed record Tile
{
    public const char BlankSymbol = '?';

    private Tile(char letter, int points, bool isBlank)
    {
        Letter = letter;
        Points = points;
        IsBlank = isBlank;
    }

    public char Letter { get; }

    public int Points { get; }

    public bool IsBlank { get; }

    public bool IsUnassignedBlank => IsBlank && Letter == BlankSymbol;

    /// <summary>Hoe de steen op het plankje wordt aangeduid: de letter, of '?' voor een blanco.</summary>
    public char RackSymbol => IsBlank ? BlankSymbol : Letter;

    public static Tile ForLetter(char letter, int points)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(points);
        return new Tile(Alphabet.Normalize(letter), points, isBlank: false);
    }

    public static Tile Blank() => new(BlankSymbol, 0, isBlank: true);

    public Tile WithBlankLetter(char letter)
    {
        if (!IsBlank)
        {
            throw new InvalidOperationException("Alleen een blanco steen kan een letter krijgen.");
        }

        return new Tile(Alphabet.Normalize(letter), 0, isBlank: true);
    }
}
