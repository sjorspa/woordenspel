using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Woordenspel.Contracts;
using Woordenspel.Application;
using static Woordenspel.Api.Tests.ApiAssert;

namespace Woordenspel.Api.Tests;

public class AccountApiTests(WoordenspelApiFactory factory) : IClassFixture<WoordenspelApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_returns_a_token_that_identifies_the_user()
    {
        var response = await _client.PostAsJsonAsync("/api/account/register", new { username = "Anna", password = "geheim123" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await ReadAsync<AuthResponse>(response);
        Assert.Equal("Anna", auth.Username);
        Assert.True(auth.ExpiresAt > DateTimeOffset.UtcNow);

        var me = await GetMeAsync(auth.Token);
        Assert.Equal(new UserDto(auth.UserId, "Anna", IsGuest: false), me);
    }

    [Fact]
    public async Task Login_with_the_right_password_returns_a_token()
    {
        await _client.PostAsJsonAsync("/api/account/register", new { username = "Bram", password = "geheim123" });

        var response = await _client.PostAsJsonAsync("/api/account/login", new { username = "bram", password = "geheim123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await ReadAsync<AuthResponse>(response);
        Assert.Equal("Bram", (await GetMeAsync(auth.Token)).Username);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_is_unauthorized()
    {
        await _client.PostAsJsonAsync("/api/account/register", new { username = "Charlie", password = "geheim123" });

        var response = await _client.PostAsJsonAsync("/api/account/login", new { username = "Charlie", password = "fout" });

        await ProblemAsync(response, HttpStatusCode.Unauthorized, "AuthenticationFailed");
    }

    [Fact]
    public async Task Registering_a_taken_username_is_a_conflict()
    {
        await _client.PostAsJsonAsync("/api/account/register", new { username = "Dirk", password = "geheim123" });

        var response = await _client.PostAsJsonAsync("/api/account/register", new { username = "DIRK", password = "geheim456" });

        await ProblemAsync(response, HttpStatusCode.Conflict, "UsernameTaken");
    }

    [Theory]
    [InlineData("x", "geheim123", "InvalidUsername")]
    [InlineData("Eva", "kort", "InvalidPassword")]
    public async Task Register_validates_input(string username, string password, string code)
    {
        var response = await _client.PostAsJsonAsync("/api/account/register", new { username, password });

        await ProblemAsync(response, HttpStatusCode.UnprocessableEntity, code);
    }

    [Fact]
    public async Task Playing_as_guest_gives_a_random_name_and_a_working_token()
    {
        var response = await _client.PostAsync("/api/account/guest", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var guest = await ReadAsync<AuthResponse>(response);
        Assert.True(guest.IsGuest);
        Assert.Matches("^[A-Z][a-z]+[A-Z][a-z]+[0-9]{2}$", guest.Username);
        Assert.Equal(new UserDto(guest.UserId, guest.Username, IsGuest: true), await GetMeAsync(guest.Token));
    }

    [Fact]
    public async Task Guest_can_play_against_a_registered_player()
    {
        var guest = await ReadAsync<AuthResponse>(await _client.PostAsync("/api/account/guest", null));
        await _client.PostAsJsonAsync("/api/account/register", new { username = "Fleur", password = "geheim123" });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/games") { Content = JsonContent.Create(new { opponent = "fleur" }) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", guest.Token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Guest_cannot_log_in_again()
    {
        var guest = await ReadAsync<AuthResponse>(await _client.PostAsync("/api/account/guest", null));

        var response = await _client.PostAsJsonAsync("/api/account/login", new { username = guest.Username, password = "" });

        await ProblemAsync(response, HttpStatusCode.Unauthorized, "AuthenticationFailed");
    }

    [Fact]
    public async Task Registered_user_is_not_a_guest()
    {
        var auth = await ReadAsync<AuthResponse>(
            await _client.PostAsJsonAsync("/api/account/register", new { username = "Gijs", password = "geheim123" }));

        Assert.False(auth.IsGuest);
        Assert.False((await GetMeAsync(auth.Token)).IsGuest);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("geen.geldig.token")]
    public async Task Protected_endpoints_require_a_valid_token(string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/games");
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<UserDto> GetMeAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/account/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await ReadAsync<UserDto>(response);
    }
}
