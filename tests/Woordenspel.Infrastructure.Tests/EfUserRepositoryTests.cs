using Woordenspel.Application;
using Woordenspel.Domain.Users;
using Woordenspel.Infrastructure.Persistence;
using Woordenspel.TestSupport;

namespace Woordenspel.Infrastructure.Tests;

public sealed class EfUserRepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    [Fact]
    public async Task Added_user_can_be_found_by_username_in_any_case_and_by_id()
    {
        var user = User.Register(Guid.NewGuid(), "Anna", "hash", TestGames.Now);
        await using (var db = _database.CreateContext())
        {
            await new EfUserRepository(db).AddAsync(user, default);
        }

        await using var readDb = _database.CreateContext();
        var repository = new EfUserRepository(readDb);
        var byName = await repository.FindByUsernameAsync("ANNA", default);
        var byId = await repository.FindByIdAsync(user.Id, default);

        Assert.Equal(user.Id, byName?.Id);
        Assert.Equal("Anna", byId?.Username);
        Assert.Equal("hash", byId?.PasswordHash);
    }

    [Fact]
    public async Task Guest_without_password_is_stored_and_loaded()
    {
        var guest = User.RegisterGuest(Guid.NewGuid(), "SnelleVos42", TestGames.Now);
        await using (var db = _database.CreateContext())
        {
            await new EfUserRepository(db).AddAsync(guest, default);
        }

        await using var readDb = _database.CreateContext();
        var loaded = await new EfUserRepository(readDb).FindByUsernameAsync("snellevos42", default);

        Assert.NotNull(loaded);
        Assert.True(loaded.IsGuest);
    }

    [Fact]
    public async Task Unknown_user_returns_null()
    {
        await using var db = _database.CreateContext();

        Assert.Null(await new EfUserRepository(db).FindByUsernameAsync("niemand", default));
    }

    [Fact]
    public async Task Duplicate_username_is_rejected_by_the_database()
    {
        _database.AddUser("Anna");
        await using var db = _database.CreateContext();

        await Assert.ThrowsAsync<UsernameTakenException>(
            () => new EfUserRepository(db).AddAsync(User.Register(Guid.NewGuid(), "anna", "hash", TestGames.Now), default));
    }

    public void Dispose() => _database.Dispose();
}
