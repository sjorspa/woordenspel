using Woordenspel.Domain.Games;

namespace Woordenspel.Infrastructure.Persistence;

/// <summary>
/// Een spel wordt als JSON-snapshot opgeslagen. <see cref="Version"/> voorkomt dat twee gelijktijdige
/// verzoeken elkaars wijzigingen overschrijven.
/// </summary>
public sealed class GameRecord
{
    public Guid Id { get; set; }

    public GameStatus Status { get; set; }

    public string State { get; set; } = string.Empty;

    public int Version { get; set; }

    /// <summary>Kopie van <see cref="Game.TurnDeadline"/>, zodat verlopen beurten snel te vinden zijn.</summary>
    public DateTimeOffset? TurnDeadline { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<GamePlayerRecord> Players { get; set; } = [];
}

/// <summary>Koppelt spellen aan gebruikers, zodat je snel "mijn spellen" kunt opvragen.</summary>
public sealed class GamePlayerRecord
{
    public Guid GameId { get; set; }

    public Guid UserId { get; set; }
}
