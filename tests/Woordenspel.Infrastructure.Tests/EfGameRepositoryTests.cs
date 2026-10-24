using System.Text.Json;
using Woordenspel.Application;
using Woordenspel.Domain.Boards;
using Woordenspel.Domain.Games;
using Woordenspel.Domain.Users;
using Woordenspel.TestSupport;

namespace Woordenspel.Infrastructure.Tests;

public sealed class EfGameRepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly User _anna;
    private readonly User _bram;

    public EfGameRepositoryTests()
    {
        _anna = _database.AddUser("Anna");
        _bram = _database.AddUser("Bram");
    }

    [Fact]
    public async Task Saved_game_is_restored_with_its_full_state()
    {
        var game = StartGame();
        game.PlaceTiles(_anna.Id, TestGames.Word("KAt", 7, 7), new FakeWordDictionary("KAT"), TestGames.Now);
        await SaveInNewContextAsync(game);

        await using var db = _database.CreateContext();
        var loaded = await _database.CreateGameRepository(db).FindAsync(game.Id, default);

        Assert.NotNull(loaded);
        Assert.Equal(Json(game), Json(loaded));
    }

    [Fact]
    public async Task Unknown_game_returns_null()
    {
        await using var db = _database.CreateContext();

        Assert.Null(await _database.CreateGameRepository(db).FindAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Changes_to_a_loaded_game_are_saved()
    {
        var game = StartGame();
        await SaveInNewContextAsync(game);

        await using (var db = _database.CreateContext())
        {
            var repository = _database.CreateGameRepository(db);
            var loaded = (await repository.FindAsync(game.Id, default))!;
            loaded.Pass(_anna.Id, TestGames.Now);
            await repository.SaveAsync(loaded, default);
        }

        await using var readDb = _database.CreateContext();
        var reloaded = (await _database.CreateGameRepository(readDb).FindAsync(game.Id, default))!;
        Assert.Equal(MoveType.Pass, Assert.Single(reloaded.Moves).Type);
        Assert.Equal(_bram.Id, reloaded.CurrentPlayer.Id);
    }

    [Fact]
    public async Task ListForPlayer_returns_only_that_players_games()
    {
        var charlie = _database.AddUser("Charlie");
        var annaVsBram = StartGame();
        var bramVsCharlie = StartGame(_bram, charlie);
        await SaveInNewContextAsync(annaVsBram);
        await SaveInNewContextAsync(bramVsCharlie);

        await using var db = _database.CreateContext();
        var repository = _database.CreateGameRepository(db);

        Assert.Equal([annaVsBram.Id], (await repository.ListForPlayerAsync(_anna.Id, default)).Select(g => g.Id));
        Assert.Equal(2, (await repository.ListForPlayerAsync(_bram.Id, default)).Count);
    }

    [Fact]
    public async Task ListOverdue_returns_running_games_whose_deadline_has_passed()
    {
        var overdue = StartGame();
        var finished = StartGame();
        finished.Resign(_bram.Id, TestGames.Now);
        await SaveInNewContextAsync(overdue);
        await SaveInNewContextAsync(finished);

        await using var db = _database.CreateContext();
        var repository = _database.CreateGameRepository(db);
        var deadline = overdue.TurnDeadline!.Value;

        Assert.Empty(await repository.ListOverdueAsync(deadline.AddMilliseconds(-1), default));
        Assert.Equal([overdue.Id], (await repository.ListOverdueAsync(deadline, default)).Select(g => g.Id));
    }

    [Fact]
    public async Task Repository_can_save_again_after_a_concurrency_conflict()
    {
        var game = StartGame();
        await SaveInNewContextAsync(game);

        await using var db = _database.CreateContext();
        var repository = _database.CreateGameRepository(db);
        var copy = (await repository.FindAsync(game.Id, default))!;
        await using (var otherDb = _database.CreateContext())
        {
            var other = _database.CreateGameRepository(otherDb);
            var otherCopy = (await other.FindAsync(game.Id, default))!;
            otherCopy.Pass(_anna.Id, TestGames.Now);
            await other.SaveAsync(otherCopy, default);
        }

        copy.Pass(_anna.Id, TestGames.Now);
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => repository.SaveAsync(copy, default));

        var another = StartGame();
        await repository.SaveAsync(another, default);
        Assert.NotNull(await repository.FindAsync(another.Id, default));
    }

    [Fact]
    public async Task Concurrent_changes_to_the_same_game_are_detected()
    {
        var game = StartGame();
        await SaveInNewContextAsync(game);

        await using var firstDb = _database.CreateContext();
        await using var secondDb = _database.CreateContext();
        var first = _database.CreateGameRepository(firstDb);
        var second = _database.CreateGameRepository(secondDb);
        var firstCopy = (await first.FindAsync(game.Id, default))!;
        var secondCopy = (await second.FindAsync(game.Id, default))!;

        firstCopy.Pass(_anna.Id, TestGames.Now);
        await first.SaveAsync(firstCopy, default);
        secondCopy.Resign(_anna.Id, TestGames.Now);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => second.SaveAsync(secondCopy, default));
    }

    private Game StartGame(User? first = null, User? second = null) =>
        Game.Start(
            Guid.NewGuid(),
            [new PlayerIdentity((first ?? _anna).Id, (first ?? _anna).Username), new PlayerIdentity((second ?? _bram).Id, (second ?? _bram).Username)],
            GameRules.Default,
            BoardLayout.Standard,
            TestGames.Bag("KA?BEDSWOORDENAEIOUNRSTL"),
            new PredictableRandomizer(),
            TestGames.Now);

    private async Task SaveInNewContextAsync(Game game)
    {
        await using var db = _database.CreateContext();
        await _database.CreateGameRepository(db).SaveAsync(game, default);
    }

    private static string Json(Game game) => JsonSerializer.Serialize(game.ToSnapshot());

    public void Dispose() => _database.Dispose();
}
