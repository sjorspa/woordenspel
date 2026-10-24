using Woordenspel.Domain.Seeks;

namespace Woordenspel.Application.Abstractions;

/// <summary>Uitnodigingen die wachten op een tegenstander.</summary>
public interface ISeekRepository
{
    Task<IReadOnlyList<GameSeek>> ListAsync(CancellationToken cancellationToken);

    Task<GameSeek?> FindAsync(Guid seekId, CancellationToken cancellationToken);

    Task AddAsync(GameSeek seek, CancellationToken cancellationToken);

    /// <summary>
    /// Haalt de uitnodiging weg en geeft hem terug, of null als hij al weg was. Atomair: als twee spelers
    /// tegelijk meedoen, krijgt er maar één de uitnodiging.
    /// </summary>
    Task<GameSeek?> TakeAsync(Guid seekId, CancellationToken cancellationToken);

    Task RemoveByCreatorAsync(Guid creatorId, CancellationToken cancellationToken);
}
