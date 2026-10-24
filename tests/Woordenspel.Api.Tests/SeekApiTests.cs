using System.Net;
using System.Net.Http.Json;
using Woordenspel.Contracts;
using Woordenspel.Domain.Games;
using static Woordenspel.Api.Tests.ApiAssert;

namespace Woordenspel.Api.Tests;

public class SeekApiTests(WoordenspelApiFactory factory) : IClassFixture<WoordenspelApiFactory>
{
    [Fact]
    public async Task Open_game_appears_for_other_players_and_they_are_told_live()
    {
        var (anna, annaUser) = await factory.CreateUserClientAsync();
        var (bram, bramUser) = await factory.CreateUserClientAsync();
        await using var bramListener = await factory.ConnectAsync(bramUser);

        var response = await anna.PostAsJsonAsync("/api/seeks", new { });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var seek = await ReadAsync<SeekDto>(response);
        Assert.Null(seek.OpponentId);
        Assert.True(seek.IsMine);
        Assert.Contains(await ListSeeksAsync(bram), s => s.Id == seek.Id && !s.IsMine && s.CreatorName == annaUser.Username);
        await WaitUntilAsync(() => bramListener.SeeksChangedCount > 0);
    }

    [Fact]
    public async Task Joining_starts_the_game_and_both_players_are_sent_to_it()
    {
        var (anna, annaUser) = await factory.CreateUserClientAsync();
        var (bram, bramUser) = await factory.CreateUserClientAsync();
        await using var annaListener = await factory.ConnectAsync(annaUser);
        await using var bramListener = await factory.ConnectAsync(bramUser);
        var seek = await ReadAsync<SeekDto>(await anna.PostAsJsonAsync("/api/seeks", new { }));

        var response = await bram.PostAsync($"/api/seeks/{seek.Id}/join", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var game = await ReadAsync<GameDto>(response);
        Assert.Equal([annaUser.UserId, bramUser.UserId], game.Players.Select(p => p.Id));
        Assert.Equal(GameStatus.InProgress, game.Status);

        var forAnna = await HubListener.WaitForAsync(annaListener.Started, g => g.Id == game.Id);
        var forBram = await HubListener.WaitForAsync(bramListener.Started, g => g.Id == game.Id);
        Assert.Equal("AAAAAAA", Letters(forAnna));
        Assert.Equal("BBCCDDD", Letters(forBram));
        Assert.DoesNotContain(await ListSeeksAsync(bram), s => s.Id == seek.Id);
    }

    [Fact]
    public async Task Challenge_is_only_for_the_challenged_player()
    {
        var (anna, _) = await factory.CreateUserClientAsync();
        var (bram, bramUser) = await factory.CreateUserClientAsync();
        var (charlie, _) = await factory.CreateUserClientAsync();
        var seek = await ReadAsync<SeekDto>(await anna.PostAsJsonAsync("/api/seeks", new { opponent = bramUser.Username }));

        Assert.Contains(await ListSeeksAsync(bram), s => s.Id == seek.Id);
        Assert.DoesNotContain(await ListSeeksAsync(charlie), s => s.Id == seek.Id);
        await ProblemAsync(await charlie.PostAsync($"/api/seeks/{seek.Id}/join", null), HttpStatusCode.Forbidden, "SeekNotForYou");
        Assert.Equal(HttpStatusCode.Created, (await bram.PostAsync($"/api/seeks/{seek.Id}/join", null)).StatusCode);
    }

    [Fact]
    public async Task A_seek_can_only_be_joined_once()
    {
        var (anna, _) = await factory.CreateUserClientAsync();
        var (bram, _) = await factory.CreateUserClientAsync();
        var (charlie, _) = await factory.CreateUserClientAsync();
        var seek = await ReadAsync<SeekDto>(await anna.PostAsJsonAsync("/api/seeks", new { }));
        await bram.PostAsync($"/api/seeks/{seek.Id}/join", null);

        var response = await charlie.PostAsync($"/api/seeks/{seek.Id}/join", null);

        await ProblemAsync(response, HttpStatusCode.NotFound, "SeekNotFound");
    }

    [Fact]
    public async Task Seeks_of_offline_players_are_hidden_and_cannot_be_joined()
    {
        var (anna, _) = await factory.CreateUserClientAsync(online: false);
        var (bram, _) = await factory.CreateUserClientAsync();
        var seek = await ReadAsync<SeekDto>(await anna.PostAsJsonAsync("/api/seeks", new { }));

        Assert.DoesNotContain(await ListSeeksAsync(bram), s => s.Id == seek.Id);
        await ProblemAsync(await bram.PostAsync($"/api/seeks/{seek.Id}/join", null), HttpStatusCode.Conflict, "SeekUnavailable");
    }

    [Fact]
    public async Task Seek_disappears_when_its_creator_goes_offline()
    {
        var (anna, annaUser) = await factory.CreateUserClientAsync(online: false);
        var (bram, _) = await factory.CreateUserClientAsync();
        var annaConnection = await factory.ConnectAsync(annaUser);
        var seek = await ReadAsync<SeekDto>(await anna.PostAsJsonAsync("/api/seeks", new { }));
        Assert.Contains(await ListSeeksAsync(bram), s => s.Id == seek.Id);

        await annaConnection.DisposeAsync();

        await WaitUntilAsync(async () => !(await ListSeeksAsync(bram)).Any(s => s.Id == seek.Id));
    }

    [Fact]
    public async Task Creator_can_cancel_a_seek()
    {
        var (anna, _) = await factory.CreateUserClientAsync();
        var (bram, _) = await factory.CreateUserClientAsync();
        var seek = await ReadAsync<SeekDto>(await anna.PostAsJsonAsync("/api/seeks", new { }));

        await ProblemAsync(await bram.DeleteAsync($"/api/seeks/{seek.Id}"), HttpStatusCode.NotFound, "SeekNotFound");
        Assert.Equal(HttpStatusCode.NoContent, (await anna.DeleteAsync($"/api/seeks/{seek.Id}")).StatusCode);
        Assert.DoesNotContain(await ListSeeksAsync(bram), s => s.Id == seek.Id);
    }

    [Fact]
    public async Task Challenging_an_unknown_player_returns_not_found()
    {
        var (anna, _) = await factory.CreateUserClientAsync();

        var response = await anna.PostAsJsonAsync("/api/seeks", new { opponent = "bestaatniet" });

        await ProblemAsync(response, HttpStatusCode.NotFound, "UserNotFound");
    }

    [Fact]
    public async Task Challenging_yourself_is_rejected()
    {
        var (anna, annaUser) = await factory.CreateUserClientAsync();

        var response = await anna.PostAsJsonAsync("/api/seeks", new { opponent = annaUser.Username });

        await ProblemAsync(response, HttpStatusCode.UnprocessableEntity, "DuplicatePlayer");
    }

    private static async Task<List<SeekDto>> ListSeeksAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<SeekDto>>("/api/seeks", WoordenspelApiFactory.JsonOptions))!;

    private static string Letters(GameDto game) => new(game.Rack.Select(t => t.Letter).ToArray());

    private static Task WaitUntilAsync(Func<bool> condition) => WaitUntilAsync(() => Task.FromResult(condition()));

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        var giveUpAt = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (!await condition())
        {
            if (DateTime.UtcNow > giveUpAt)
            {
                throw new TimeoutException("De verwachte toestand werd niet bereikt.");
            }

            await Task.Delay(20);
        }
    }
}
