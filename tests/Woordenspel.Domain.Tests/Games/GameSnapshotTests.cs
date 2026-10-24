using System.Text.Json;
using Woordenspel.Domain.Games;
using Woordenspel.TestSupport;

namespace Woordenspel.Domain.Tests.Games;

public class GameSnapshotTests
{
    private static readonly DateTimeOffset Now = TestGames.Now;
    private static readonly FakeWordDictionary Dictionary = new("KAT", "KATEN");

    [Fact]
    public void Restored_game_has_exactly_the_same_state()
    {
        var game = PlayedGame();

        var restored = Game.Restore(game.ToSnapshot(), new PredictableRandomizer());

        Assert.Equal(Json(game.ToSnapshot()), Json(restored.ToSnapshot()));
        Assert.Equal(game.CurrentPlayer.Id, restored.CurrentPlayer.Id);
        Assert.Equal(game.Players.Select(p => p.Score), restored.Players.Select(p => p.Score));
        Assert.Equal('t', char.ToLowerInvariant(restored.Board.GetTile(new(7, 9))!.Letter));
        Assert.True(restored.Board.GetTile(new(7, 9))!.IsBlank);
    }

    [Fact]
    public void Restored_game_can_be_played_further()
    {
        var restored = Game.Restore(PlayedGame().ToSnapshot(), new PredictableRandomizer());
        var bram = restored.CurrentPlayer;

        var move = restored.PlaceTiles(bram.Id, TestGames.Word("EN", 7, 10), Dictionary, Now);

        Assert.Equal(["KATEN"], move.Words);
        Assert.Equal(GameStatus.InProgress, restored.Status);
    }

    [Fact]
    public void Scoreless_turn_count_survives_a_restore()
    {
        var game = TestGames.Start("KATBEDS", "WOORDEN", "AEIOUNRSTL");
        game.Pass(game.Players[0].Id, Now);
        game.Pass(game.Players[1].Id, Now);
        game.Pass(game.Players[0].Id, Now);

        var restored = Game.Restore(game.ToSnapshot(), new PredictableRandomizer());
        restored.Pass(restored.Players[1].Id, Now);

        Assert.Equal(GameStatus.Finished, restored.Status);
    }

    [Fact]
    public void Clocks_survive_a_restore()
    {
        var game = TestGames.Start("KA?BEDS", "WOORDEN", "AEIOUNRSTL");
        game.Pass(game.Players[0].Id, Now.AddSeconds(30));

        var restored = Game.Restore(game.ToSnapshot(), new PredictableRandomizer());

        Assert.Equal(TimeSpan.FromSeconds(270), restored.Players[0].TimeRemaining);
        Assert.Equal(game.TurnDeadline, restored.TurnDeadline);
    }

    [Fact]
    public void Snapshot_survives_json_serialization()
    {
        var snapshot = PlayedGame().ToSnapshot();

        var deserialized = JsonSerializer.Deserialize<GameSnapshot>(Json(snapshot))!;

        Assert.Equal(Json(snapshot), Json(Game.Restore(deserialized, new PredictableRandomizer()).ToSnapshot()));
    }

    /// <summary>Anna legt KAt (blanco T) en het is de beurt aan Bram.</summary>
    private static Game PlayedGame()
    {
        var game = TestGames.Start("KA?BEDS", "WOORDEN", "AEIOUNRSTL");
        game.PlaceTiles(game.Players[0].Id, TestGames.Word("KAt", 7, 7), Dictionary, Now);
        return game;
    }

    private static string Json(GameSnapshot snapshot) => JsonSerializer.Serialize(snapshot);
}
