using Woordenspel.Contracts;

namespace Woordenspel.Application.Abstractions;

/// <summary>Brengt spelers realtime op de hoogte van wijzigingen in hun spel.</summary>
public interface IGameNotifier
{
    /// <param name="game">Het spel zoals <paramref name="playerId"/> het ziet, inclusief zijn eigen plankje.</param>
    Task GameUpdatedAsync(Guid playerId, GameDto game, CancellationToken cancellationToken);

    /// <summary>Een nieuw spel waarin deze speler meedoet is begonnen; de client opent het.</summary>
    Task GameStartedAsync(Guid playerId, GameDto game, CancellationToken cancellationToken);

    /// <summary>De lijst met uitnodigingen is veranderd; iedereen die meekijkt haalt hem opnieuw op.</summary>
    Task SeeksChangedAsync(CancellationToken cancellationToken);
}
