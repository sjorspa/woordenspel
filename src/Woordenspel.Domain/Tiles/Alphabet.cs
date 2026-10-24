namespace Woordenspel.Domain.Tiles;

public static class Alphabet
{
    public static bool IsLetter(char c) => c is >= 'A' and <= 'Z';

    /// <summary>Zet een letter om naar hoofdletter en controleert dat het A t/m Z is.</summary>
    public static char Normalize(char c)
    {
        var upper = char.ToUpperInvariant(c);
        if (!IsLetter(upper))
        {
            throw new DomainException(DomainErrors.InvalidLetter, $"'{c}' is geen geldige letter.");
        }

        return upper;
    }
}
