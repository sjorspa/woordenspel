using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Woordenspel.Domain.Users;
using Woordenspel.Infrastructure.Persistence;
using Woordenspel.TestSupport;

namespace Woordenspel.Infrastructure.Tests;

/// <summary>Een SQLite-database in het geheugen met het echte schema; leeft zolang dit object leeft.</summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public SqliteTestDatabase()
    {
        _connection.Open();
        using var db = CreateContext();
        db.Database.Migrate();
    }

    /// <summary>Elke context is een aparte "request", met eigen change tracking.</summary>
    public WoordenspelDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<WoordenspelDbContext>().UseSqlite(_connection).Options);

    public EfGameRepository CreateGameRepository(WoordenspelDbContext db) =>
        new(db, new GameSnapshotSerializer(new PredictableRandomizer()), new FakeTimeProvider());

    public User AddUser(string username)
    {
        using var db = CreateContext();
        var user = User.Register(Guid.NewGuid(), username, "hash", TestGames.Now);
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public void Dispose() => _connection.Dispose();
}
