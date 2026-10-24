using Woordenspel.Domain.Tiles;

namespace Woordenspel.Domain.Games;

public sealed class Player
{
    public const int MaxNameLength = 30;

    private readonly Rack _rack = new();

    internal Player(Guid id, string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > MaxNameLength)
        {
            throw new DomainException(
                DomainErrors.InvalidPlayerName,
                $"Een spelersnaam moet tussen 1 en {MaxNameLength} tekens lang zijn.");
        }

        Id = id;
        Name = trimmed;
    }

    internal static Player Restore(Guid id, string name, int score, bool hasResigned, TimeSpan? timeRemaining, IEnumerable<Tile> rack)
    {
        var player = new Player(id, name) { Score = score, HasResigned = hasResigned, TimeRemaining = timeRemaining };
        player.AddTiles(rack);
        return player;
    }

    public Guid Id { get; }

    public string Name { get; }

    public int Score { get; private set; }

    /// <summary>Opgegeven of de bedenktijd overschreden; zo'n speler kan niet winnen.</summary>
    public bool HasResigned { get; private set; }

    /// <summary>
    /// Resterende bedenktijd aan het begin van de huidige beurt; zie <see cref="Game.GetTimeRemaining"/> voor
    /// de tijd op een bepaald moment. Null als er zonder klok wordt gespeeld.
    /// </summary>
    public TimeSpan? TimeRemaining { get; internal set; }

    public IReadOnlyList<Tile> Rack => _rack.Tiles;

    public int RackPoints => _rack.TotalPoints;

    internal void AddTiles(IEnumerable<Tile> tiles) => _rack.Add(tiles);

    internal IReadOnlyList<Tile> FindTiles(IEnumerable<char> symbols) => _rack.Find(symbols);

    internal IReadOnlyList<Tile> TakeTiles(IEnumerable<char> symbols) => _rack.Take(symbols);

    internal void AddScore(int points) => Score += points;

    internal void Resign() => HasResigned = true;
}
