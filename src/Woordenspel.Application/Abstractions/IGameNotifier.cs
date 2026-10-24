using Woordenspel.Contracts;

namespace Woordenspel.Application.Abstractions;

/// <summary>Brengt spelers realtime op de hoogte van wijzigingen in hun spel.</summary>
public interface IGameNotifier
{
    /// <param name="game">Het spel zoals <paramref name="playerId"/> het ziet, inclusief zijn eigen plankje.</param>
    Task GameUpdatedAsync(Guid playerId, GameDto game, CancellationToken cancellationToken);
}
