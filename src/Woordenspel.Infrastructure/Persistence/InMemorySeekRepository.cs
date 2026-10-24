using System.Collections.Concurrent;
using Woordenspel.Application.Abstractions;
using Woordenspel.Domain.Seeks;

namespace Woordenspel.Infrastructure.Persistence;

/// <summary>
/// Uitnodigingen leven in het geheugen: ze gelden alleen zolang de maker online is, en na een herstart van de
/// server zijn alle verbindingen toch weg.
/// </summary>
public sealed class InMemorySeekRepository : ISeekRepository
{
    private readonly ConcurrentDictionary<Guid, GameSeek> _seeks = new();

    public Task<IReadOnlyList<GameSeek>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<GameSeek>>(_seeks.Values.ToList());

    public Task<GameSeek?> FindAsync(Guid seekId, CancellationToken cancellationToken) =>
        Task.FromResult(_seeks.GetValueOrDefault(seekId));

    public Task AddAsync(GameSeek seek, CancellationToken cancellationToken)
    {
        _seeks[seek.Id] = seek;
        return Task.CompletedTask;
    }

    public Task<GameSeek?> TakeAsync(Guid seekId, CancellationToken cancellationToken) =>
        Task.FromResult(_seeks.TryRemove(seekId, out var seek) ? seek : null);

    public Task RemoveByCreatorAsync(Guid creatorId, CancellationToken cancellationToken)
    {
        foreach (var seek in _seeks.Values.Where(s => s.Creator.Id == creatorId))
        {
            _seeks.TryRemove(seek.Id, out _);
        }

        return Task.CompletedTask;
    }
}
