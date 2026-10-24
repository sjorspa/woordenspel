using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Woordenspel.Application;
using Woordenspel.Application.Abstractions;
using Woordenspel.Domain.Users;

namespace Woordenspel.Infrastructure.Persistence;

public sealed class EfUserRepository(WoordenspelDbContext db) : IUserRepository
{
    private const int SqliteConstraintViolation = 19;

    public Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var normalized = User.Normalize(username);
        return db.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == normalized, cancellationToken);
    }

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteErrorCode: SqliteConstraintViolation })
        {
            // Iemand anders registreerde dezelfde naam tussen onze controle en het opslaan.
            db.Entry(user).State = EntityState.Detached;
            throw new UsernameTakenException(user.Username);
        }
    }
}
