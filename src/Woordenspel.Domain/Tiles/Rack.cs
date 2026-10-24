namespace Woordenspel.Domain.Tiles;

/// <summary>Het plankje van een speler.</summary>
public sealed class Rack
{
    private readonly List<Tile> _tiles = [];

    public IReadOnlyList<Tile> Tiles => _tiles;

    public int Count => _tiles.Count;

    public int TotalPoints => _tiles.Sum(t => t.Points);

    public void Add(IEnumerable<Tile> tiles) => _tiles.AddRange(tiles);

    /// <summary>
    /// Zoekt voor elk symbool (letter of '?') een steen op het plankje, zonder iets weg te halen.
    /// De volgorde van het resultaat komt overeen met de volgorde van de symbolen.
    /// </summary>
    public IReadOnlyList<Tile> Find(IEnumerable<char> symbols)
    {
        var available = new List<Tile>(_tiles);
        var found = new List<Tile>();

        foreach (var symbol in symbols)
        {
            var normalized = symbol == Tile.BlankSymbol ? symbol : Alphabet.Normalize(symbol);
            var index = available.FindIndex(t => t.RackSymbol == normalized);
            if (index < 0)
            {
                var description = normalized == Tile.BlankSymbol ? "blanco steen" : $"'{normalized}'";
                throw new DomainException(DomainErrors.TilesNotOnRack, $"Je hebt geen {description} (meer) op je plankje.");
            }

            found.Add(available[index]);
            available.RemoveAt(index);
        }

        return found;
    }

    /// <summary>Haalt de stenen weg; alles of niets.</summary>
    public IReadOnlyList<Tile> Take(IEnumerable<char> symbols)
    {
        var tiles = Find(symbols);
        foreach (var tile in tiles)
        {
            _tiles.Remove(tile);
        }

        return tiles;
    }
}
