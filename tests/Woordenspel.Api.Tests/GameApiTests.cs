using System.Net;
using System.Net.Http.Json;
using Woordenspel.Contracts;
using Woordenspel.Application;
using Woordenspel.Domain.Games;
using static Woordenspel.Api.Tests.ApiAssert;

namespace Woordenspel.Api.Tests;

public class GameApiTests(WoordenspelApiFactory factory) : IClassFixture<WoordenspelApiFactory>
{
    [Fact]
    public async Task Create_game_against_an_opponent()
    {
        var (anna, annaUser) = await factory.CreateUserClientAsync();
        var (_, bramUser) = await factory.CreateUserClientAsync();

        var response = await anna.PostAsJsonAsync("/api/games", new { opponent = bramUser.Username });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var game = await ReadAsync<GameDto>(response);
        Assert.Equal($"/api/games/{game.Id}", response.Headers.Location?.ToString());
        Assert.Equal([annaUser.UserId, bramUser.UserId], game.Players.Select(p => p.Id));
        Assert.Equal("AAAAAAA", Letters(game));
    }

    [Fact]
    public async Task Both_players_see_the_game_with_their_own_rack()
    {
        var (anna, bram, game) = await StartGameAsync();

        var forAnna = await GetGameAsync(anna, game.Id);
        var forBram = await GetGameAsync(bram, game.Id);
        var bramsGames = await bram.GetFromJsonAsync<List<GameSummaryDto>>("/api/games", WoordenspelApiFactory.JsonOptions);

        Assert.Equal("AAAAAAA", Letters(forAnna));
        Assert.Equal("BBCCDDD", Letters(forBram));
        var summary = Assert.Single(bramsGames!);
        Assert.Equal(game.Id, summary.Id);
        Assert.False(summary.IsYourTurn);
    }

    [Fact]
    public async Task Outsiders_cannot_see_the_game()
    {
        var (_, _, game) = await StartGameAsync();
        var (outsider, _) = await factory.CreateUserClientAsync();

        var response = await outsider.GetAsync($"/api/games/{game.Id}");

        await ProblemAsync(response, HttpStatusCode.NotFound, "PlayerNotInGame");
    }

    [Fact]
    public async Task Unknown_opponent_returns_not_found()
    {
        var (anna, _) = await factory.CreateUserClientAsync();

        var response = await anna.PostAsJsonAsync("/api/games", new { opponent = "bestaatniet" });

        await ProblemAsync(response, HttpStatusCode.NotFound, "UserNotFound");
    }

    [Fact]
    public async Task Playing_against_yourself_is_rejected()
    {
        var (anna, annaUser) = await factory.CreateUserClientAsync();

        var response = await anna.PostAsJsonAsync("/api/games", new { opponent = annaUser.Username });

        await ProblemAsync(response, HttpStatusCode.UnprocessableEntity, "DuplicatePlayer");
    }

    [Fact]
    public async Task Placing_a_valid_word_scores_points_and_is_stored()
    {
        var (anna, bram, game) = await StartGameAsync();

        var response = await anna.PostAsJsonAsync($"/api/games/{game.Id}/place", PlaceAa());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ReadAsync<MoveResultDto>(response);
        Assert.Equal(2, result.Move.Score);
        Assert.Equal(game.Players[1].Id, result.Game.CurrentPlayerId);
        Assert.Equal(".......AA......", (await GetGameAsync(bram, game.Id)).Board.Rows[7]);
    }

    [Fact]
    public async Task Preview_returns_the_score()
    {
        var (anna, _, game) = await StartGameAsync();

        var preview = await ReadAsync<PreviewDto>(await anna.PostAsJsonAsync($"/api/games/{game.Id}/preview", PlaceAa()));

        Assert.Equal(2, preview.Score);
    }

    [Fact]
    public async Task Playing_out_of_turn_returns_conflict()
    {
        var (_, bram, game) = await StartGameAsync();

        var response = await bram.PostAsync($"/api/games/{game.Id}/pass", null);

        await ProblemAsync(response, HttpStatusCode.Conflict, "NotYourTurn");
    }

    [Fact]
    public async Task Invalid_word_returns_unprocessable_entity_with_the_words()
    {
        var (anna, _, game) = await StartGameAsync();

        var response = await anna.PostAsJsonAsync(
            $"/api/games/{game.Id}/place",
            new { tiles = new[] { new { row = 7, column = 7, letter = "A" }, new { row = 7, column = 8, letter = "A" }, new { row = 7, column = 9, letter = "A" } } });

        var problem = await ProblemAsync(response, HttpStatusCode.UnprocessableEntity, "InvalidWords");
        Assert.Equal("AAA", problem.GetProperty("invalidWords")[0].GetString());
    }

    [Fact]
    public async Task Swap_pass_and_resign_endpoints_work()
    {
        var (anna, bram, game) = await StartGameAsync();

        var swap = await anna.PostAsJsonAsync($"/api/games/{game.Id}/swap", new { letters = new[] { "A" } });
        var pass = await bram.PostAsync($"/api/games/{game.Id}/pass", null);
        var resign = await bram.PostAsync($"/api/games/{game.Id}/resign", null);

        Assert.Equal(HttpStatusCode.OK, swap.StatusCode);
        Assert.Equal(HttpStatusCode.OK, pass.StatusCode);
        var result = await ReadAsync<MoveResultDto>(resign);
        Assert.Equal(GameStatus.Finished, result.Game.Status);
        Assert.Equal([game.Players[0].Id], result.Game.WinnerIds);
    }

    [Fact]
    public async Task Player_who_misses_the_deadline_loses()
    {
        var (anna, bram, game) = await StartGameAsync();
        factory.Clock.Advance(TimeSpan.FromHours(73));

        var view = await GetGameAsync(bram, game.Id);
        var lateMove = await anna.PostAsync($"/api/games/{game.Id}/pass", null);

        Assert.Equal(GameStatus.Finished, view.Status);
        Assert.Equal(MoveType.Timeout, Assert.Single(view.Moves).Type);
        Assert.Equal([game.Players[1].Id], view.WinnerIds);
        await ProblemAsync(lateMove, HttpStatusCode.Conflict, "GameFinished");
    }

    [Fact]
    public async Task Unknown_game_returns_not_found()
    {
        var (anna, _) = await factory.CreateUserClientAsync();

        var response = await anna.GetAsync($"/api/games/{Guid.NewGuid()}");

        await ProblemAsync(response, HttpStatusCode.NotFound, "GameNotFound");
    }

    [Theory]
    [InlineData("aap", true)]
    [InlineData("noot", false)]
    public async Task Word_check_is_public(string word, bool expected)
    {
        var result = await factory.CreateClient().GetFromJsonAsync<WordCheckDto>($"/api/words/{word}", WoordenspelApiFactory.JsonOptions);

        Assert.Equal(expected, result!.IsValid);
    }

    [Fact]
    public async Task Setup_is_public()
    {
        var setup = await factory.CreateClient().GetFromJsonAsync<SetupDto>("/api/setup", WoordenspelApiFactory.JsonOptions);

        Assert.Equal(15, setup!.BoardSize);
        Assert.Equal(26, setup.Letters.Count);
    }

    /// <summary>Anna maakt het spel aan en begint.</summary>
    private async Task<(HttpClient Anna, HttpClient Bram, GameDto Game)> StartGameAsync()
    {
        var (anna, _) = await factory.CreateUserClientAsync();
        var (bram, bramUser) = await factory.CreateUserClientAsync();
        var response = await anna.PostAsJsonAsync("/api/games", new { opponent = bramUser.Username });
        response.EnsureSuccessStatusCode();
        return (anna, bram, await ReadAsync<GameDto>(response));
    }

    private static async Task<GameDto> GetGameAsync(HttpClient client, Guid gameId) =>
        (await client.GetFromJsonAsync<GameDto>($"/api/games/{gameId}", WoordenspelApiFactory.JsonOptions))!;

    private static string Letters(GameDto game) => new(game.Rack.Select(t => t.Letter).ToArray());

    private static object PlaceAa() =>
        new { tiles = new[] { new { row = 7, column = 7, letter = "A" }, new { row = 7, column = 8, letter = "A" } } };
}
