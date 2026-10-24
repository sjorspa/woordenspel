namespace Woordenspel.Domain.Tiles;

public sealed class TileBag
{
    private readonly List<Tile> _tiles;
    private readonly IRandomizer _randomizer;

    /// <summary>Maakt een geschudde zak.</summary>
    public TileBag(IEnumerable<Tile> tiles, IRandomizer randomizer)
        : this(tiles.ToList(), randomizer)
    {
        Shuffle();
    }

    private TileBag(List<Tile> tiles, IRandomizer randomizer)
    {
        if (tiles.Any(t => t.IsBlank && !t.IsUnassignedBlank))
        {
            throw new ArgumentException("Blanco stenen in de zak mogen geen letter hebben.", nameof(tiles));
        }

        _tiles = tiles;
        _randomizer = randomizer;
    }

    /// <summary>Herstelt een zak in precies deze volgorde, zonder te schudden.</summary>
    public static TileBag Restore(IEnumerable<Tile> tiles, IRandomizer randomizer) => new(tiles.ToList(), randomizer);

    public int Count => _tiles.Count;

    internal IReadOnlyList<Tile> Tiles => _tiles;

    /// <summary>Pakt maximaal <paramref name="count"/> stenen; minder als de zak bijna leeg is.</summary>
    public IReadOnlyList<Tile> Draw(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var drawn = _tiles.Take(count).ToList();
        _tiles.RemoveRange(0, drawn.Count);
        return drawn;
    }

    public void Return(IEnumerable<Tile> tiles)
    {
        _tiles.AddRange(tiles);
        Shuffle();
    }

    private void Shuffle()
    {
        // Fisher-Yates; een randomizer die altijd 0 geeft laat de volgorde ongemoeid.
        for (var i = 0; i < _tiles.Count - 1; i++)
        {
            var j = i + _randomizer.Next(_tiles.Count - i);
            (_tiles[i], _tiles[j]) = (_tiles[j], _tiles[i]);
        }
    }
}
