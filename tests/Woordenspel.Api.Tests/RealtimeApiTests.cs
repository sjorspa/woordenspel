using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Woordenspel.Contracts;
using Woordenspel.Domain.Games;
using static Woordenspel.Api.Tests.ApiAssert;

namespace Woordenspel.Api.Tests;

public class RealtimeApiTests(WoordenspelApiFactory factory) : IClassFixture<WoordenspelApiFactory>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Opponent_is_told_about_a_new_game()
    {
        var (anna, _) = await factory.CreateUserClientAsync();
        var (_, bramUser) = await factory.CreateUserClientAsync();
        var (connection, updates) = await factory.ConnectAsync(bramUser);
        await using var _ = connection;

        var game = await ReadAsync<GameDto>(await anna.PostAsJsonAsync("/api/games", new { opponent = bramUser.Username }));

        var update = await WaitForAsync(updates, g => g.Id == game.Id);
        Assert.Equal("BBCCDDD", new string(update.Rack.Select(t => t.Letter).ToArray()));
    }

    [Fact]
    public async Task Opponent_sees_a_move_immediately()
    {
        var (anna, bramUser, game) = await StartGameAsync();
        var (connection, updates) = await factory.ConnectAsync(bramUser);
        await using var _ = connection;

        await anna.PostAsJsonAsync(
            $"/api/games/{game.Id}/place",
            new { tiles = new[] { new { row = 7, column = 7, letter = "A" }, new { row = 7, column = 8, letter = "A" } } });

        var update = await WaitForAsync(updates, g => g.Id == game.Id && g.Moves.Count == 1);
        Assert.Equal(".......AA......", update.Board.Rows[7]);
        Assert.Equal(bramUser.UserId, update.CurrentPlayerId);
    }

    [Fact]
    public async Task Both_players_hear_when_a_clock_runs_out_without_anyone_doing_anything()
    {
        var (_, bramUser, game) = await StartGameAsync();
        var (connection, updates) = await factory.ConnectAsync(bramUser);
        await using var _ = connection;

        factory.Clock.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));

        var update = await WaitForAsync(updates, g => g.Id == game.Id && g.Status == GameStatus.Finished);
        Assert.Equal(MoveType.Timeout, update.Moves[^1].Type);
        Assert.Equal([bramUser.UserId], update.WinnerIds);
        Assert.Equal(0, update.Players[0].TimeRemainingMs);
    }

    [Fact]
    public async Task Connecting_without_a_token_is_refused()
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, GameHubContract.Path), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => connection.StartAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task Game_view_contains_the_clocks()
    {
        var (anna, _, game) = await StartGameAsync();
        factory.Clock.Advance(TimeSpan.FromSeconds(10));

        var view = (await anna.GetFromJsonAsync<GameDto>($"/api/games/{game.Id}", WoordenspelApiFactory.JsonOptions))!;

        Assert.Equal(factory.Clock.Now, view.ServerTime);
        Assert.Equal([290_000L, 300_000L], view.Players.Select(p => p.TimeRemainingMs));
    }

    private async Task<(HttpClient Anna, AuthResponse Bram, GameDto Game)> StartGameAsync()
    {
        var (anna, _) = await factory.CreateUserClientAsync();
        var (_, bramUser) = await factory.CreateUserClientAsync();
        var response = await anna.PostAsJsonAsync("/api/games", new { opponent = bramUser.Username });
        response.EnsureSuccessStatusCode();
        return (anna, bramUser, await ReadAsync<GameDto>(response));
    }

    private static async Task<GameDto> WaitForAsync(List<GameDto> updates, Func<GameDto, bool> predicate)
    {
        var giveUpAt = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < giveUpAt)
        {
            lock (updates)
            {
                if (updates.LastOrDefault(predicate) is { } match)
                {
                    return match;
                }
            }

            await Task.Delay(20);
        }

        throw new TimeoutException("De verwachte spelupdate kwam niet binnen.");
    }
}
