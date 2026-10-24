using Woordenspel.Contracts;
using Woordenspel.Domain;
using Woordenspel.Domain.Games;
using Woordenspel.Domain.Users;
using Woordenspel.TestSupport;

namespace Woordenspel.Application.Tests;

public class LobbyServiceTests
{
    private readonly FakeSeekRepository _seeks = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakePresence _presence = new();
    private readonly FakeGameNotifier _notifier = new();
    private readonly FakeGameRepository _games = new();
    private readonly LobbyService _lobby;
    private readonly User _anna;
    private readonly User _bram;
    private readonly User _charlie;

    public LobbyServiceTests()
    {
        var clock = new FakeTimeProvider();
        var games = new GameService(_games, new FakeWordDictionary(), new PredictableRandomizer(), _notifier, clock, GameSetup.Wordfeud);
        _lobby = new LobbyService(_seeks, _users, _presence, _notifier, games, GameSetup.Wordfeud, clock);
        _anna = _users.Add("Anna");
        _bram = _users.Add("Bram");
        _charlie = _users.Add("Charlie");
        _presence.Online.UnionWith([_anna.Id, _bram.Id, _charlie.Id]);
    }

    [Fact]
    public async Task Open_game_is_visible_to_everyone_who_is_online()
    {
        var seek = await _lobby.CreateSeekAsync(_anna.Id, opponentUsername: null);

        Assert.Equal(new SeekDto(seek.Id, _anna.Id, "Anna", null, null, true, 300_000, FakeTimeProvider.Start), seek);
        Assert.Single(await _lobby.ListSeeksAsync(_bram.Id));
        Assert.False((await _lobby.ListSeeksAsync(_bram.Id))[0].IsMine);
        Assert.Single(await _lobby.ListSeeksAsync(_charlie.Id));
        Assert.Equal(1, _notifier.SeeksChangedCount);
    }

    [Fact]
    public async Task Challenge_is_visible_only_to_both_players()
    {
        var seek = await _lobby.CreateSeekAsync(_anna.Id, "bram");

        Assert.Equal((_bram.Id, "Bram"), (seek.OpponentId, seek.OpponentName));
        Assert.Single(await _lobby.ListSeeksAsync(_anna.Id));
        Assert.Single(await _lobby.ListSeeksAsync(_bram.Id));
        Assert.Empty(await _lobby.ListSeeksAsync(_charlie.Id));
    }

    [Fact]
    public async Task Challenges_to_you_come_first()
    {
        await _lobby.CreateSeekAsync(_charlie.Id, opponentUsername: null);
        var challenge = await _lobby.CreateSeekAsync(_anna.Id, "Bram");

        var list = await _lobby.ListSeeksAsync(_bram.Id);

        Assert.Equal(challenge.Id, list[0].Id);
    }

    [Fact]
    public async Task Seeks_of_players_who_are_offline_are_hidden_except_from_themselves()
    {
        await _lobby.CreateSeekAsync(_anna.Id, opponentUsername: null);
        _presence.Online.Remove(_anna.Id);

        Assert.Empty(await _lobby.ListSeeksAsync(_bram.Id));
        Assert.Single(await _lobby.ListSeeksAsync(_anna.Id));
    }

    [Fact]
    public async Task A_new_seek_replaces_your_previous_one()
    {
        await _lobby.CreateSeekAsync(_anna.Id, opponentUsername: null);
        await _lobby.CreateSeekAsync(_anna.Id, "Bram");

        var seek = Assert.Single(await _lobby.ListSeeksAsync(_anna.Id));
        Assert.Equal("Bram", seek.OpponentName);
    }

    [Fact]
    public async Task Challenging_an_unknown_player_fails()
    {
        await Assert.ThrowsAsync<UserNotFoundException>(() => _lobby.CreateSeekAsync(_anna.Id, "Niemand"));
    }

    [Fact]
    public async Task Challenging_yourself_fails()
    {
        var exception = await Assert.ThrowsAsync<DomainException>(() => _lobby.CreateSeekAsync(_anna.Id, "anna"));

        Assert.Equal(DomainErrors.DuplicatePlayer, exception.Code);
    }

    [Fact]
    public async Task Joining_starts_the_game_with_both_players_and_opens_it_for_both()
    {
        var seek = await _lobby.CreateSeekAsync(_anna.Id, opponentUsername: null);
        _notifier.SeeksChangedCount = 0;

        var game = await _lobby.JoinAsync(seek.Id, _bram.Id);

        Assert.Equal([_anna.Id, _bram.Id], game.Players.Select(p => p.Id));
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal("BBCCDDD", new string(game.Rack.Select(t => t.Letter).ToArray()));
        Assert.Equal([_anna.Id, _bram.Id], _notifier.Started.Select(s => s.PlayerId));
        Assert.Empty(await _lobby.ListSeeksAsync(_charlie.Id));
        Assert.Equal(1, _notifier.SeeksChangedCount);
    }

    [Fact]
    public async Task Clock_starts_when_the_opponent_joins_not_when_the_seek_is_made()
    {
        var clock = new FakeTimeProvider();
        var games = new GameService(_games, new FakeWordDictionary(), new PredictableRandomizer(), _notifier, clock, GameSetup.Wordfeud);
        var lobby = new LobbyService(_seeks, _users, _presence, _notifier, games, GameSetup.Wordfeud, clock);
        var seek = await lobby.CreateSeekAsync(_anna.Id, opponentUsername: null);
        clock.Advance(TimeSpan.FromMinutes(10));

        var game = await lobby.JoinAsync(seek.Id, _bram.Id);

        Assert.Equal(clock.Now.AddMinutes(5), game.TurnDeadline);
        Assert.All(game.Players, p => Assert.Equal(300_000, p.TimeRemainingMs));
    }

    [Fact]
    public async Task Only_the_challenged_player_can_accept()
    {
        var seek = await _lobby.CreateSeekAsync(_anna.Id, "Bram");

        var exception = await Assert.ThrowsAsync<DomainException>(() => _lobby.JoinAsync(seek.Id, _charlie.Id));

        Assert.Equal(DomainErrors.SeekNotForYou, exception.Code);
        Assert.Single(await _lobby.ListSeeksAsync(_anna.Id));
    }

    [Fact]
    public async Task You_cannot_join_your_own_seek()
    {
        var seek = await _lobby.CreateSeekAsync(_anna.Id, opponentUsername: null);

        var exception = await Assert.ThrowsAsync<DomainException>(() => _lobby.JoinAsync(seek.Id, _anna.Id));

        Assert.Equal(DomainErrors.DuplicatePlayer, exception.Code);
    }

    [Fact]
    public async Task A_seek_can_only_be_joined_once()
    {
        var seek = await _lobby.CreateSeekAsync(_anna.Id, opponentUsername: null);
        await _lobby.JoinAsync(seek.Id, _bram.Id);

        await Assert.ThrowsAsync<SeekNotFoundException>(() => _lobby.JoinAsync(seek.Id, _charlie.Id));
    }

    [Fact]
    public async Task Joining_the_seek_of_a_player_who_went_offline_fails()
    {
        var seek = await _lobby.CreateSeekAsync(_anna.Id, opponentUsername: null);
        _presence.Online.Remove(_anna.Id);

        await Assert.ThrowsAsync<SeekUnavailableException>(() => _lobby.JoinAsync(seek.Id, _bram.Id));
        Assert.Empty(_notifier.Started);
    }

    [Fact]
    public async Task Joining_withdraws_your_own_seek()
    {
        var annasSeek = await _lobby.CreateSeekAsync(_anna.Id, opponentUsername: null);
        await _lobby.CreateSeekAsync(_bram.Id, opponentUsername: null);

        await _lobby.JoinAsync(annasSeek.Id, _bram.Id);

        Assert.Empty(await _lobby.ListSeeksAsync(_charlie.Id));
    }

    [Fact]
    public async Task Creator_can_cancel_their_seek_others_cannot()
    {
        var seek = await _lobby.CreateSeekAsync(_anna.Id, opponentUsername: null);

        await Assert.ThrowsAsync<SeekNotFoundException>(() => _lobby.CancelAsync(seek.Id, _bram.Id));
        await _lobby.CancelAsync(seek.Id, _anna.Id);

        Assert.Empty(await _lobby.ListSeeksAsync(_anna.Id));
    }
}
