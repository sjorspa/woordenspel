using Woordenspel.Domain.Boards;
using Woordenspel.Domain.Tiles;

namespace Woordenspel.Domain.Moves;

/// <summary>
/// Wat een speler wil leggen: een letter op een positie. Bij een blanco steen is
/// <see cref="Letter"/> de letter die de blanco steen moet voorstellen.
/// </summary>
public sealed record TilePlacement(Position Position, char Letter, bool IsBlank = false)
{
    public char Letter { get; } = Alphabet.Normalize(Letter);

    public char RackSymbol => IsBlank ? Tile.BlankSymbol : Letter;
}

/// <summary>Een steen (met eventueel ingevulde blanco letter) op een positie.</summary>
public readonly record struct PlacedTile(Position Position, Tile Tile);
