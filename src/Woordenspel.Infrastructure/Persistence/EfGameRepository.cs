using Microsoft.EntityFrameworkCore;
using Woordenspel.Application;
using Woordenspel.Application.Abstractions;
using Woordenspel.Domain.Games;

namespace Woordenspel.Infrastructure.Persistence;

public sealed class EfGameRepository(WoordenspelDbContext db, GameSnapshotSerializer serializer, TimeProvider timeProvider)
    : IGameRepository
{
    public async Task<Game?> FindAsync(Guid gameId, CancellationToken cancellationToken)
    {
        var record = await db.Games.FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);
        return record is null ? null : serializer.Deserialize(record.State);
    }

    public async Task<IReadOnlyList<Game>> ListForPlayerAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var records = await db.Games
            .Where(g => g.Players.Any(p => p.UserId == playerId))
            .ToListAsync(cancellationToken);
        return records.Select(r => serializer.Deserialize(r.State)).ToList();
    }

    public async Task<IReadOnlyList<Game>> ListOverdueAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var records = await db.Games
            .Where(g => g.Status == GameStatus.InProgress && g.TurnDeadline != null && g.TurnDeadline <= now)
            .ToListAsync(cancellationToken);
        return records.Select(r => serializer.Deserialize(r.State)).ToList();
    }

    /// <remarks>
    /// Een bestaand spel moet in dezelfde scope eerst zijn geladen; de versie van dat moment wordt
    /// bij het opslaan gecontroleerd.
    /// </remarks>
    public async Task SaveAsync(Game game, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var record = db.Games.Local.FirstOrDefault(r => r.Id == game.Id);

        if (record is null)
        {
            record = new GameRecord
            {
                Id = game.Id,
                CreatedAt = now,
                Players = game.Players.Select(p => new GamePlayerRecord { GameId = game.Id, UserId = p.Id }).ToList(),
            };
            db.Games.Add(record);
        }
        else
        {
            record.Version++;
        }

        record.State = serializer.Serialize(game);
        record.Status = game.Status;
        record.TurnDeadline = game.TurnDeadline;
        record.UpdatedAt = now;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            // Niet meer volgen, anders probeert een volgende SaveChanges in deze scope het opnieuw.
            db.Entry(record).State = EntityState.Detached;
            throw new ConcurrencyConflictException(exception);
        }
    }
}
