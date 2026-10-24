using Woordenspel.Domain.Moves;

namespace Woordenspel.Domain.Games;

public enum MoveType
{
    Placement,
    Swap,
    Pass,
    Resign,

    /// <summary>De speler heeft niet binnen de bedenktijd gespeeld en verliest.</summary>
    Timeout,
}

/// <summary>Een gespeelde beurt in de geschiedenis van het spel.</summary>
/// <param name="TileCount">Aantal gelegde of geruilde stenen.</param>
public sealed record MoveRecord(Guid PlayerId, MoveType Type, int Score, IReadOnlyList<string> Words, int TileCount)
{
    public static MoveRecord Placement(Guid playerId, PlacementResult result, int tileCount) =>
        new(playerId, MoveType.Placement, result.Score, result.Words.Select(w => w.Text).ToList(), tileCount);

    public static MoveRecord Swap(Guid playerId, int tileCount) => new(playerId, MoveType.Swap, 0, [], tileCount);

    public static MoveRecord Pass(Guid playerId) => new(playerId, MoveType.Pass, 0, [], 0);

    public static MoveRecord Resign(Guid playerId) => new(playerId, MoveType.Resign, 0, [], 0);

    public static MoveRecord Timeout(Guid playerId) => new(playerId, MoveType.Timeout, 0, [], 0);
}
