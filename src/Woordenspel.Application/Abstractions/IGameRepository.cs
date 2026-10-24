using Woordenspel.Domain.Games;

namespace Woordenspel.Application.Abstractions;

public interface IGameRepository
{
    Task<Game?> FindAsync(Guid gameId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Game>> ListForPlayerAsync(Guid playerId, CancellationToken cancellationToken);

    /// <summary>Lopende spellen waarvan de deadline van de huidige beurt op of vóór <paramref name="now"/> ligt.</summary>
    Task<IReadOnlyList<Game>> ListOverdueAsync(DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Voegt een nieuw spel toe of slaat de gewijzigde toestand op.
    /// </summary>
    /// <exception cref="ConcurrencyConflictException">Als het spel sinds het laden door iemand anders is gewijzigd.</exception>
    Task SaveAsync(Game game, CancellationToken cancellationToken);
}
