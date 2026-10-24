using Woordenspel.Domain.Boards;
using Woordenspel.Domain.Games;
using Woordenspel.Domain.Moves;
using Woordenspel.Domain.Tiles;
using Woordenspel.TestSupport;

namespace Woordenspel.Domain.Tests.Games;

public class GameTests
{
    private const string Rack1 = "KATBEDS";
    private const string Rack2 = "WOORDEN";
    private const string RestOfBag = "AEIOUNRSTL";

    private static readonly DateTimeOffset Now = TestGames.Now;

    private static readonly FakeWordDictionary Dictionary = new("KAT", "BED", "WOORDEN", "KATTEN", "KATS");

    // --- Start ---

    [Fact]
    public void Start_deals_a_full_rack_to_every_player()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);

        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(Rack1, RackOf(game.Players[0]));
        Assert.Equal(Rack2, RackOf(game.Players[1]));
        Assert.Equal(RestOfBag.Length, game.TilesInBag);
        Assert.Same(game.Players[0], game.CurrentPlayer);
        Assert.True(game.Board.IsEmpty);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Start_requires_two_players(int playerCount)
    {
        var names = Enumerable.Range(1, playerCount).Select(i => $"Speler {i}").ToList();

        var exception = Assert.Throws<DomainException>(() => StartWith(names));

        Assert.Equal(DomainErrors.InvalidPlayerCount, exception.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Een naam die veel te lang is voor dit spel")]
    public void Start_rejects_invalid_player_names(string name)
    {
        var exception = Assert.Throws<DomainException>(() => StartWith(["Anna", name]));

        Assert.Equal(DomainErrors.InvalidPlayerName, exception.Code);
    }

    [Fact]
    public void Start_lets_the_randomizer_pick_who_begins()
    {
        var game = Game.Start(
            Guid.NewGuid(), TestGames.Players("Anna", "Bram"), GameRules.Default, BoardLayout.Standard, TestGames.Bag(new string('A', 30)),
            new LastChoiceRandomizer(), Now);

        Assert.Same(game.Players[1], game.CurrentPlayer);
    }

    [Fact]
    public void Start_uses_the_given_player_ids()
    {
        var players = TestGames.Players("Anna", "Bram");

        var game = StartWith(players);

        Assert.Equal(players.Select(p => p.Id), game.Players.Select(p => p.Id));
    }

    [Fact]
    public void Start_rejects_playing_against_yourself()
    {
        var anna = new PlayerIdentity(Guid.NewGuid(), "Anna");

        var exception = Assert.Throws<DomainException>(() => StartWith([anna, anna]));

        Assert.Equal(DomainErrors.DuplicatePlayer, exception.Code);
    }

    // --- Woord leggen ---

    [Fact]
    public void PlaceTiles_scores_refills_rack_and_passes_turn()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);
        var anna = game.Players[0];

        var move = game.PlaceTiles(anna.Id, TestGames.Word("KAT", 7, 7), Dictionary, Now);

        Assert.Equal(MoveType.Placement, move.Type);
        Assert.Equal(["KAT"], move.Words);
        Assert.Equal(6, move.Score);
        Assert.Equal(6, anna.Score);
        Assert.Equal("BEDSAEI", RackOf(anna));
        Assert.Equal(RestOfBag.Length - 3, game.TilesInBag);
        Assert.Equal('K', game.Board.GetTile(new Position(7, 7))?.Letter);
        Assert.Same(game.Players[1], game.CurrentPlayer);
        Assert.Equal([move], game.Moves);
    }

    [Fact]
    public void Players_take_turns()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);

        game.PlaceTiles(game.Players[0].Id, TestGames.Word("KAT", 7, 7), Dictionary, Now);
        game.PlaceTiles(game.Players[1].Id, TestGames.Word("EN", 7, 10), new FakeWordDictionary("KATEN"), Now);

        Assert.Same(game.Players[0], game.CurrentPlayer);
        Assert.Equal((3 + 1 + 2 + 1 + 1) * 2, game.Players[1].Score);
    }

    [Fact]
    public void Playing_out_of_turn_is_rejected()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);

        AssertRejected(DomainErrors.NotYourTurn, () => game.PlaceTiles(game.Players[1].Id, TestGames.Word("WOORDEN", 7, 4), Dictionary, Now));
        AssertRejected(DomainErrors.NotYourTurn, () => game.Pass(game.Players[1].Id, Now));
    }

    [Fact]
    public void Unknown_player_is_rejected()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);

        AssertRejected(DomainErrors.PlayerNotInGame, () => game.Pass(Guid.NewGuid(), Now));
    }

    [Fact]
    public void Tiles_must_be_on_the_players_rack()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);

        AssertRejected(DomainErrors.TilesNotOnRack, () => game.PlaceTiles(game.Players[0].Id, TestGames.Word("KATTEN", 7, 7), Dictionary, Now));
    }

    [Fact]
    public void Rejected_move_does_not_change_the_game()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);
        var anna = game.Players[0];

        Assert.Throws<InvalidWordsException>(() => game.PlaceTiles(anna.Id, TestGames.Word("BAK", 7, 7), Dictionary, Now));

        Assert.Equal(Rack1, RackOf(anna));
        Assert.Equal(0, anna.Score);
        Assert.True(game.Board.IsEmpty);
        Assert.Equal(RestOfBag.Length, game.TilesInBag);
        Assert.Same(anna, game.CurrentPlayer);
        Assert.Empty(game.Moves);
    }

    [Fact]
    public void Blank_can_be_played_as_any_letter()
    {
        var game = TestGames.Start("KA?BEDS", Rack2, RestOfBag);

        var move = game.PlaceTiles(game.Players[0].Id, TestGames.Word("KAt", 7, 7), Dictionary, Now);

        Assert.Equal(3 + 1 + 0, move.Score);
        var tile = game.Board.GetTile(new Position(7, 9));
        Assert.Equal('T', tile?.Letter);
        Assert.True(tile?.IsBlank);
    }

    [Fact]
    public void PreviewPlacement_returns_score_without_playing()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);
        var anna = game.Players[0];

        var preview = game.PreviewPlacement(anna.Id, TestGames.Word("KAT", 7, 7), Dictionary);

        Assert.Equal(6, preview.Score);
        Assert.True(game.Board.IsEmpty);
        Assert.Equal(Rack1, RackOf(anna));
        Assert.Same(anna, game.CurrentPlayer);
    }

    // --- Ruilen en passen ---

    [Fact]
    public void SwapTiles_exchanges_tiles_with_the_bag()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);
        var anna = game.Players[0];

        var move = game.SwapTiles(anna.Id, ['K', 'B'], Now);

        Assert.Equal(MoveType.Swap, move.Type);
        Assert.Equal(2, move.TileCount);
        Assert.Equal("ATEDSAE", RackOf(anna));
        Assert.Equal(RestOfBag.Length, game.TilesInBag);
        Assert.Same(game.Players[1], game.CurrentPlayer);
    }

    [Fact]
    public void SwapTiles_needs_enough_tiles_in_the_bag()
    {
        var game = TestGames.Start(Rack1, Rack2, "ABCDEF");

        AssertRejected(DomainErrors.NotEnoughTilesToSwap, () => game.SwapTiles(game.Players[0].Id, ['K'], Now));
    }

    [Fact]
    public void SwapTiles_needs_at_least_one_tile()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);

        AssertRejected(DomainErrors.NoTiles, () => game.SwapTiles(game.Players[0].Id, [], Now));
    }

    [Fact]
    public void Pass_gives_the_turn_to_the_next_player()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);

        var move = game.Pass(game.Players[0].Id, Now);

        Assert.Equal(MoveType.Pass, move.Type);
        Assert.Same(game.Players[1], game.CurrentPlayer);
        Assert.Equal(GameStatus.InProgress, game.Status);
    }

    // --- Einde van het spel ---

    [Fact]
    public void Game_ends_after_every_player_passed_twice_in_a_row()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);
        game.PlaceTiles(game.Players[0].Id, TestGames.Word("KAT", 7, 7), Dictionary, Now);
        var anna = game.Players[0];
        var bram = game.Players[1];

        game.Pass(bram.Id, Now);
        game.Pass(anna.Id, Now);
        game.Pass(bram.Id, Now);
        Assert.Equal(GameStatus.InProgress, game.Status);
        game.Pass(anna.Id, Now);

        Assert.Equal(GameStatus.Finished, game.Status);
        Assert.Equal(6 - anna.RackPoints, anna.Score);
        Assert.Equal(-bram.RackPoints, bram.Score);
        Assert.Equal([anna], game.Winners);
    }

    [Fact]
    public void Placing_a_word_resets_the_scoreless_turn_count()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);
        var anna = game.Players[0];
        var bram = game.Players[1];

        game.Pass(anna.Id, Now);
        game.Pass(bram.Id, Now);
        game.Pass(anna.Id, Now);
        game.PlaceTiles(bram.Id, TestGames.Word("WOORDEN", 7, 4), Dictionary, Now);
        game.Pass(anna.Id, Now);
        game.Pass(bram.Id, Now);
        game.Pass(anna.Id, Now);

        Assert.Equal(GameStatus.InProgress, game.Status);
    }

    [Fact]
    public void Playing_out_with_an_empty_bag_ends_the_game_and_earns_the_opponents_tile_points()
    {
        var game = TestGames.Start(Rack2, Rack1);
        var anna = game.Players[0];
        var bram = game.Players[1];

        game.PlaceTiles(anna.Id, TestGames.Word("WOORDEN", 7, 4), Dictionary, Now);

        Assert.Equal(GameStatus.Finished, game.Status);
        var bramsTilePoints = 3 + 1 + 2 + 4 + 1 + 2 + 2;
        Assert.Equal(13 + 40 + bramsTilePoints, anna.Score);
        Assert.Equal(-bramsTilePoints, bram.Score);
        Assert.Equal([anna], game.Winners);
    }

    [Fact]
    public void Resigning_ends_the_game_and_the_opponent_wins()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);
        var bram = game.Players[1];

        var move = game.Resign(bram.Id, Now); // ook als het niet je beurt is

        Assert.Equal(MoveType.Resign, move.Type);
        Assert.Equal(GameStatus.Finished, game.Status);
        Assert.True(bram.HasResigned);
        Assert.Equal([game.Players[0]], game.Winners);
    }

    [Fact]
    public void Equal_scores_give_multiple_winners()
    {
        var game = TestGames.Start("AAAAAAA", "AAAAAAA", RestOfBag);
        for (var i = 0; i < 2; i++)
        {
            game.Pass(game.Players[0].Id, Now);
            game.Pass(game.Players[1].Id, Now);
        }

        Assert.Equal(game.Players, game.Winners);
    }

    [Fact]
    public void No_moves_are_allowed_after_the_game_has_finished()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);
        game.Resign(game.Players[1].Id, Now);

        AssertRejected(DomainErrors.GameFinished, () => game.Pass(game.Players[0].Id, Now));
        AssertRejected(DomainErrors.GameFinished, () => game.Resign(game.Players[0].Id, Now));
    }

    [Fact]
    public void Winners_is_empty_while_the_game_is_in_progress()
    {
        Assert.Empty(TestGames.Start(Rack1, Rack2, RestOfBag).Winners);
    }

    // --- Bedenktijd ---

    [Fact]
    public void Turn_deadline_is_the_time_limit_after_the_turn_started()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);

        Assert.Equal(Now + TimeSpan.FromHours(72), game.TurnDeadline);
    }

    [Fact]
    public void Each_new_turn_gets_a_fresh_deadline()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);
        var later = Now.AddHours(50);

        game.Pass(game.Players[0].Id, later);

        Assert.Equal(later, game.TurnStartedAt);
        Assert.Equal(later.AddHours(72), game.TurnDeadline);
    }

    [Fact]
    public void Turn_does_not_expire_until_the_deadline_has_passed()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);

        Assert.False(game.ExpireTurnIfOverdue(Now.AddHours(72)));
        Assert.Equal(GameStatus.InProgress, game.Status);
    }

    [Fact]
    public void Missing_the_deadline_loses_the_game()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);
        var anna = game.Players[0];

        Assert.True(game.ExpireTurnIfOverdue(Now.AddHours(72).AddSeconds(1)));

        Assert.Equal(GameStatus.Finished, game.Status);
        Assert.True(anna.HasResigned);
        var move = Assert.Single(game.Moves);
        Assert.Equal((anna.Id, MoveType.Timeout), (move.PlayerId, move.Type));
        Assert.Equal([game.Players[1]], game.Winners);
        Assert.Null(game.TurnDeadline);
    }

    [Fact]
    public void Moving_after_the_deadline_is_rejected_and_counts_as_timeout()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag);

        AssertRejected(
            DomainErrors.GameFinished,
            () => game.PlaceTiles(game.Players[0].Id, TestGames.Word("KAT", 7, 7), Dictionary, Now.AddDays(4)));

        Assert.Equal(MoveType.Timeout, Assert.Single(game.Moves).Type);
        Assert.True(game.Board.IsEmpty);
    }

    [Fact]
    public void Without_a_time_limit_turns_never_expire()
    {
        var game = TestGames.Start(Rack1, Rack2, RestOfBag, GameRules.Default with { TurnTimeLimit = null });

        Assert.Null(game.TurnDeadline);
        Assert.False(game.ExpireTurnIfOverdue(Now.AddYears(1)));
    }

    private static Game StartWith(IReadOnlyList<string> names) => StartWith(TestGames.Players([.. names]));

    private static Game StartWith(IReadOnlyList<PlayerIdentity> players) =>
        Game.Start(Guid.NewGuid(), players, GameRules.Default, BoardLayout.Standard, TestGames.Bag(new string('A', 30)), new PredictableRandomizer(), Now);

    private static string RackOf(Player player) => new(player.Rack.Select(t => t.RackSymbol).ToArray());

    private static void AssertRejected(string expectedCode, Action action)
    {
        var exception = Assert.Throws<DomainException>(action);
        Assert.Equal(expectedCode, exception.Code);
    }

    private sealed class LastChoiceRandomizer : IRandomizer
    {
        public int Next(int maxExclusive) => maxExclusive - 1;
    }
}
