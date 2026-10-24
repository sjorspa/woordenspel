using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Woordenspel.Api.Contracts;
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
        Assert.Equal(new UserDto(auth.UserId, "Anna"), me);
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
