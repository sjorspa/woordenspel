using Woordenspel.Contracts;
using Woordenspel.Domain;
using Woordenspel.Domain.Boards;
using Woordenspel.Domain.Games;
using Woordenspel.Domain.Tiles;
using Woordenspel.Domain.Users;
using Woordenspel.TestSupport;

namespace Woordenspel.Application.Tests;

/// <remarks>
/// Zonder schudden begint de Nederlandse zak met AAAAAAA BBCCDDD: dat zijn de plankjes van Anna (die begint) en Bram.
/// </remarks>
public class GameServiceTests
{
    private readonly FakeGameRepository _repository = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeGameNotifier _notifier = new();
    private readonly FakeTimeProvider _clock = new();
    private readonly GameService _service;
    private readonly User _anna;
    private readonly User _bram;

    public GameServiceTests()
    {
        _service = CreateService(GameSetup.Wordfeud, new FakeWordDictionary("AA", "KAT"));
        _anna = _users.Add("Anna");
        _bram = _users.Add("Bram");
    }

    [Fact]
    public async Task StartGame_starts_a_game_between_the_players()
    {
        var game = await StartAsync();

        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal([(_anna.Id, "Anna"), (_bram.Id, "Bram")], game.Players.Select(p => (p.Id, p.Name)));
        Assert.All(game.Players, p => Assert.Equal(7, p.TileCount));
        Assert.Equal(_anna.Id, game.CurrentPlayerId);
        Assert.Equal(LetterSet.Dutch.TotalTiles - 14, game.TilesInBag);
        Assert.Equal("AAAAAAA", Letters(game.Rack));
        Assert.Equal(Enumerable.Repeat(new string('.', 15), 15), game.Board.Rows);
        Assert.NotNull(await _repository.FindAsync(game.Id, default));
    }

    [Fact]
    public async Task GetGame_shows_only_the_viewers_rack()
    {
        var created = await StartAsync();

        var forAnna = await _service.GetGameAsync(created.Id, _anna.Id);
        var forBram = await _service.GetGameAsync(created.Id, _bram.Id);

        Assert.Equal("AAAAAAA", Letters(forAnna.Rack));
        Assert.Equal("BBCCDDD", Letters(forBram.Rack));
    }

    [Fact]
    public async Task GetGame_throws_for_unknown_game()
    {
        await Assert.ThrowsAsync<GameNotFoundException>(() => _service.GetGameAsync(Guid.NewGuid(), _anna.Id));
    }

    [Fact]
    public async Task GetGame_is_not_allowed_for_outsiders()
    {
        var created = await StartAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(() => _service.GetGameAsync(created.Id, Guid.NewGuid()));
        Assert.Equal(DomainErrors.PlayerNotInGame, exception.Code);
    }

    [Fact]
    public async Task ListGames_returns_the_players_games_with_own_turn_first()
    {
        var charlie = _users.Add("Charlie");
        var annasTurn = await StartAsync();
        var charliesTurn = await StartAsync(players: [charlie, _anna]);
        await StartAsync(players: [_bram, charlie]);

        var games = await _service.ListGamesAsync(_anna.Id);

        Assert.Equal([annasTurn.Id, charliesTurn.Id], games.Select(g => g.Id));
        Assert.Equal([true, false], games.Select(g => g.IsYourTurn));
    }

    [Fact]
    public async Task PlaceTiles_plays_the_move_and_saves_the_game()
    {
        var created = await StartAsync();
        _repository.SaveCount = 0;

        var result = await _service.PlaceTilesAsync(created.Id, _anna.Id, [new(7, 7, 'a'), new(7, 8, 'A')]);

        Assert.Equal(2, result.Move.Score);
        Assert.Equal(["AA"], result.Move.Words);
        Assert.Equal(_bram.Id, result.Game.CurrentPlayerId);
        Assert.Equal(2, result.Game.Players[0].Score);
        Assert.Equal(".......AA......", result.Game.Board.Rows[7]);
        Assert.Equal(7, result.Game.Rack.Count);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task Blank_tiles_are_shown_in_lower_case_on_the_board()
    {
        var service = CreateService(
            GameSetup.Wordfeud with { LetterSet = new([new('A', 1, 1), new('K', 1, 3)], blankCount: 12) },
            new FakeWordDictionary("KAT"));
        var created = await StartAsync(service);

        var result = await service.PlaceTilesAsync(created.Id, _anna.Id, [new(7, 7, 'K'), new(7, 8, 'A'), new(7, 9, 'T', IsBlank: true)]);

        Assert.Equal(".......KAt.....", result.Game.Board.Rows[7]);
        Assert.Contains(result.Game.Board.Tiles, t => t is { Letter: 'T', IsBlank: true, Points: 0 });
    }

    [Fact]
    public async Task Invalid_move_is_not_saved()
    {
        var created = await StartAsync();
        _repository.SaveCount = 0;

        await Assert.ThrowsAsync<DomainException>(
            () => _service.PlaceTilesAsync(created.Id, _anna.Id, [new(0, 0, 'A'), new(0, 1, 'A')]));

        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task PreviewPlacement_does_not_change_the_game()
    {
        var created = await StartAsync();

        var preview = await _service.PreviewPlacementAsync(created.Id, _anna.Id, [new(7, 7, 'A'), new(7, 8, 'A')]);

        Assert.Equal(2, preview.Score);
        Assert.Equal([new WordDto("AA", 2)], preview.Words);
        Assert.Empty((await _service.GetGameAsync(created.Id, _anna.Id)).Board.Tiles);
    }

    [Fact]
    public async Task Swap_pass_and_resign_are_recorded_as_moves()
    {
        var created = await StartAsync();

        await _service.SwapTilesAsync(created.Id, _anna.Id, ['A', 'A']);
        await _service.PassAsync(created.Id, _bram.Id);
        var result = await _service.ResignAsync(created.Id, _anna.Id);

        Assert.Equal([MoveType.Swap, MoveType.Pass, MoveType.Resign], result.Game.Moves.Select(m => m.Type));
        Assert.Equal(GameStatus.Finished, result.Game.Status);
        Assert.Null(result.Game.CurrentPlayerId);
        Assert.Equal([_bram.Id], result.Game.WinnerIds);
    }

    [Fact]
    public async Task Game_shows_the_clocks_at_server_time()
    {
        var created = await StartAsync();
        _clock.Advance(TimeSpan.FromSeconds(20));

        var game = await _service.GetGameAsync(created.Id, _bram.Id);

        Assert.Equal(_clock.Now, game.ServerTime);
        Assert.Equal([280_000L, 300_000L], game.Players.Select(p => p.TimeRemainingMs));
        Assert.Equal(FakeTimeProvider.Start.AddMinutes(5), game.TurnDeadline);
    }

    [Fact]
    public async Task Moving_stops_your_clock()
    {
        var created = await StartAsync();
        _clock.Advance(TimeSpan.FromSeconds(30));

        var result = await _service.PassAsync(created.Id, _anna.Id);

        Assert.Equal([270_000L, 300_000L], result.Game.Players.Select(p => p.TimeRemainingMs));
        Assert.Equal(_clock.Now.AddMinutes(5), result.Game.TurnDeadline);
    }

    [Fact]
    public async Task Both_players_are_notified_with_their_own_view()
    {
        var created = await StartAsync();
        _notifier.Sent.Clear();

        await _service.PlaceTilesAsync(created.Id, _anna.Id, [new(7, 7, 'A'), new(7, 8, 'A')]);

        Assert.Equal([_anna.Id, _bram.Id], _notifier.Sent.Select(s => s.PlayerId));
        Assert.All(_notifier.Sent, s => Assert.Equal(".......AA......", s.Game.Board.Rows[7]));
        Assert.Equal("BBCCDDD", Letters(_notifier.Sent[1].Game.Rack));
    }

    [Fact]
    public async Task Starting_a_game_tells_both_players_to_open_it_with_their_own_rack()
    {
        var created = await StartAsync();

        Assert.Equal([_anna.Id, _bram.Id], _notifier.Started.Select(s => s.PlayerId));
        Assert.All(_notifier.Started, s => Assert.Equal(created.Id, s.Game.Id));
        Assert.Equal("BBCCDDD", Letters(_notifier.Started[1].Game.Rack));
    }

    [Fact]
    public async Task Rejected_move_sends_no_notification()
    {
        var created = await StartAsync();
        _notifier.Sent.Clear();

        await Assert.ThrowsAsync<DomainException>(() => _service.PassAsync(created.Id, _bram.Id));

        Assert.Empty(_notifier.Sent);
    }

    [Fact]
    public async Task ExpireOverdueGames_ends_games_whose_clock_ran_out_and_notifies_players()
    {
        var overdue = await StartAsync();
        _clock.Advance(TimeSpan.FromMinutes(4));
        var stillRunning = await StartAsync();
        _clock.Advance(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(1));
        _notifier.Sent.Clear();

        var expired = await _service.ExpireOverdueGamesAsync();

        Assert.Equal(1, expired);
        Assert.Equal(GameStatus.Finished, (await _service.GetGameAsync(overdue.Id, _anna.Id)).Status);
        Assert.Equal(GameStatus.InProgress, (await _service.GetGameAsync(stillRunning.Id, _anna.Id)).Status);
        Assert.Equal([_anna.Id, _bram.Id], _notifier.Sent.Select(s => s.PlayerId));
        Assert.All(_notifier.Sent, s => Assert.Equal([_bram.Id], s.Game.WinnerIds));
    }

    [Fact]
    public async Task Move_positions_are_included_in_the_history()
    {
        var created = await StartAsync();

        var result = await _service.PlaceTilesAsync(created.Id, _anna.Id, [new(7, 7, 'A'), new(7, 8, 'A')]);

        Assert.Equal([new PositionDto(7, 7), new PositionDto(7, 8)], result.Move.Positions);
    }

    [Fact]
    public async Task Overdue_turn_is_processed_and_saved_when_the_game_is_loaded()
    {
        var created = await StartAsync();
        _clock.Advance(TimeSpan.FromHours(73));
        _repository.SaveCount = 0;

        var game = await _service.GetGameAsync(created.Id, _bram.Id);

        Assert.Equal(GameStatus.Finished, game.Status);
        Assert.Equal(MoveType.Timeout, Assert.Single(game.Moves).Type);
        Assert.Equal([_bram.Id], game.WinnerIds);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task Overdue_turns_are_processed_when_listing_games()
    {
        await StartAsync();
        _clock.Advance(TimeSpan.FromHours(73));

        var games = await _service.ListGamesAsync(_bram.Id);

        Assert.Equal(GameStatus.Finished, Assert.Single(games).Status);
    }

    [Fact]
    public async Task Moving_after_the_deadline_is_rejected()
    {
        var created = await StartAsync();
        _clock.Advance(TimeSpan.FromDays(4));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _service.PassAsync(created.Id, _anna.Id));

        Assert.Equal(DomainErrors.GameFinished, exception.Code);
    }

    [Theory]
    [InlineData("kat", "KAT", true)]
    [InlineData(" Kat ", "KAT", true)]
    [InlineData("hond", "HOND", false)]
    public void CheckWord_normalizes_and_looks_up_the_word(string input, string normalized, bool isValid)
    {
        Assert.Equal(new WordCheckDto(normalized, isValid), _service.CheckWord(input));
    }

    [Fact]
    public void GetSetup_describes_board_letters_and_rules()
    {
        var setup = _service.GetSetup();

        Assert.Equal(15, setup.BoardSize);
        Assert.Equal((7, 7), (setup.CenterRow, setup.CenterColumn));
        Assert.Equal(8, setup.BonusSquares.Count(b => b.Bonus == SquareBonus.TripleWord));
        Assert.Contains(new LetterDto('Q', 1, 10), setup.Letters);
        Assert.Equal(2, setup.BlankCount);
        Assert.Equal(GameRules.Default, setup.Rules);
    }

    private GameService CreateService(GameSetup setup, FakeWordDictionary dictionary) =>
        new(_repository, dictionary, new PredictableRandomizer(), _notifier, _clock, setup);

    /// <summary>Start een spel; zonder <paramref name="players"/> tussen Anna (die begint) en Bram.</summary>
    private Task<GameDto> StartAsync(GameService? service = null, User[]? players = null)
    {
        players ??= [_anna, _bram];
        return (service ?? _service).StartGameAsync(
            players.Select(u => new PlayerIdentity(u.Id, u.Username)).ToList(), players[0].Id);
    }

    private static string Letters(IEnumerable<RackTileDto> rack) => new(rack.Select(t => t.Letter).ToArray());
}
