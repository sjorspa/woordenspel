using Woordenspel.Application.Abstractions;
using Woordenspel.Contracts;
using Woordenspel.Domain.Games;
using Woordenspel.Domain.Seeks;
using Woordenspel.Domain.Users;
using Woordenspel.TestSupport;

namespace Woordenspel.Application.Tests;

internal sealed class FakeGameRepository : IGameRepository
{
    private readonly Dictionary<Guid, Game> _games = [];

    public int SaveCount { get; set; }

    public Task<Game?> FindAsync(Guid gameId, CancellationToken cancellationToken) =>
        Task.FromResult(_games.GetValueOrDefault(gameId));

    public Task<IReadOnlyList<Game>> ListForPlayerAsync(Guid playerId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Game>>(_games.Values.Where(g => g.Players.Any(p => p.Id == playerId)).ToList());

    public Task<IReadOnlyList<Game>> ListOverdueAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Game>>(_games.Values.Where(g => g.TurnDeadline <= now).ToList());

    public Task SaveAsync(Game game, CancellationToken cancellationToken)
    {
        _games[game.Id] = game;
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public IReadOnlyList<User> Users => _users;

    public User Add(string username)
    {
        var user = User.Register(Guid.NewGuid(), username, "hash", TestGames.Now);
        _users.Add(user);
        return user;
    }

    public Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken) =>
        Task.FromResult(_users.FirstOrDefault(u => u.NormalizedUsername == User.Normalize(username)));

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }
}

internal sealed class FakeGameNotifier : IGameNotifier
{
    public List<(Guid PlayerId, GameDto Game)> Sent { get; } = [];

    public List<(Guid PlayerId, GameDto Game)> Started { get; } = [];

    public int SeeksChangedCount { get; set; }

    public Task GameUpdatedAsync(Guid playerId, GameDto game, CancellationToken cancellationToken)
    {
        Sent.Add((playerId, game));
        return Task.CompletedTask;
    }

    public Task GameStartedAsync(Guid playerId, GameDto game, CancellationToken cancellationToken)
    {
        Started.Add((playerId, game));
        return Task.CompletedTask;
    }

    public Task SeeksChangedAsync(CancellationToken cancellationToken)
    {
        SeeksChangedCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeSeekRepository : ISeekRepository
{
    private readonly Dictionary<Guid, GameSeek> _seeks = [];

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
        Task.FromResult(_seeks.Remove(seekId, out var seek) ? seek : null);

    public Task RemoveByCreatorAsync(Guid creatorId, CancellationToken cancellationToken)
    {
        foreach (var id in _seeks.Values.Where(s => s.Creator.Id == creatorId).Select(s => s.Id).ToList())
        {
            _seeks.Remove(id);
        }

        return Task.CompletedTask;
    }
}

internal sealed class FakePresence : IPresence
{
    public HashSet<Guid> Online { get; } = [];

    public bool IsOnline(Guid userId) => Online.Contains(userId);
}

/// <summary>"Hasht" door een voorvoegsel toe te voegen, zodat tests kunnen zien dat er gehasht is.</summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";

    public bool Verify(string passwordHash, string password) => passwordHash == Hash(password);
}
