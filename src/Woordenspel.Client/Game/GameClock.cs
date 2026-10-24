using Woordenspel.Contracts;

namespace Woordenspel.Client.Game;

/// <summary>
/// Telt de bedenktijd lokaal af. De server stuurt per speler de resterende tijd op het moment van versturen;
/// vanaf het moment van ontvangst loopt alleen de klok van de speler die aan de beurt is verder. Zo maakt een
/// verkeerd ingestelde klok op de computer van de speler niet uit.
/// </summary>
public sealed class GameClock
{
    private readonly Dictionary<Guid, TimeSpan?> _remaining;
    private readonly Guid? _runningPlayerId;
    private readonly DateTimeOffset _receivedAt;

    public GameClock(IReadOnlyList<PlayerDto> players, Guid? runningPlayerId, DateTimeOffset receivedAt)
    {
        _remaining = players.ToDictionary(
            p => p.Id,
            p => p.TimeRemainingMs is { } ms ? TimeSpan.FromMilliseconds(ms) : (TimeSpan?)null);
        _runningPlayerId = runningPlayerId;
        _receivedAt = receivedAt;
    }

    public static GameClock For(GameDto game, DateTimeOffset receivedAt) => new(game.Players, game.CurrentPlayerId, receivedAt);

    public static GameClock For(GameSummaryDto game, DateTimeOffset receivedAt) => new(game.Players, game.CurrentPlayerId, receivedAt);

    public bool IsRunning(Guid playerId) => playerId == _runningPlayerId;

    /// <summary>De resterende tijd op <paramref name="now"/>, nooit minder dan 0; null zonder klok.</summary>
    public TimeSpan? Remaining(Guid playerId, DateTimeOffset now)
    {
        if (!_remaining.TryGetValue(playerId, out var remaining) || remaining is not { } value)
        {
            return null;
        }

        if (!IsRunning(playerId))
        {
            return value;
        }

        var left = value - (now - _receivedAt);
        return left > TimeSpan.Zero ? left : TimeSpan.Zero;
    }

    /// <summary>"4:59", en in de laatste 10 seconden met tienden: "0:09.4".</summary>
    public static string Format(TimeSpan time)
    {
        if (time < TimeSpan.Zero)
        {
            time = TimeSpan.Zero;
        }

        if (time < TimeSpan.FromSeconds(10))
        {
            var tenths = (int)(time.Ticks / (TimeSpan.TicksPerSecond / 10));
            return $"0:{tenths / 10:00}.{tenths % 10}";
        }

        var seconds = (int)Math.Ceiling(time.TotalSeconds);
        return $"{seconds / 60}:{seconds % 60:00}";
    }
}
