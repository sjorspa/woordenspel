using System.Net;
using Woordenspel.Client.Services;
using Woordenspel.TestSupport;
using static Woordenspel.Client.Tests.TestData;

namespace Woordenspel.Client.Tests.Services;

public class ApiClientTests
{
    private readonly FakeApi _server = new();
    private readonly AuthState _auth = new(new FakeBrowserStorage(), new FakeTimeProvider());
    private readonly ApiClient _api;

    public ApiClientTests()
    {
        _api = new ApiClient(new HttpClient(_server) { BaseAddress = new Uri("http://test/") }, _auth);
    }

    [Fact]
    public async Task Requests_carry_the_token_of_the_logged_in_user()
    {
        await _auth.SignInAsync(AnnaLogin);
        _server.On($"GET /api/games/{GameId}", GameState());

        var game = await _api.GetGameAsync(GameId);

        Assert.Equal(GameId, game.Id);
        Assert.Equal("Bearer token-anna", _server.Requests.Single().Request.Headers.Authorization?.ToString());
    }

    [Fact]
    public async Task Login_sends_no_token()
    {
        await _auth.SignInAsync(AnnaLogin);
        _server.On("POST /api/account/login", AnnaLogin);

        await _api.LoginAsync("anna", "geheim123");

        Assert.Null(_server.Requests.Single().Request.Headers.Authorization);
        Assert.Contains("\"username\":\"anna\"", _server.Requests.Single().Body);
    }

    [Fact]
    public async Task Problem_details_become_an_api_exception()
    {
        await _auth.SignInAsync(AnnaLogin);
        _server.OnProblem($"POST /api/games/{GameId}/place", HttpStatusCode.UnprocessableEntity, "InvalidWords", "Ongeldige woorden: XQ.", "XQ");

        var exception = await Assert.ThrowsAsync<ApiException>(() => _api.PlaceAsync(GameId, [new(7, 7, 'X'), new(7, 8, 'Q')]));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, exception.Status);
        Assert.Equal("InvalidWords", exception.Code);
        Assert.Equal("Ongeldige woorden: XQ.", exception.Message);
        Assert.Equal(["XQ"], exception.InvalidWords);
    }

    [Fact]
    public async Task Unauthorized_response_signs_the_user_out()
    {
        await _auth.SignInAsync(AnnaLogin);
        _server.OnProblem("GET /api/games", HttpStatusCode.Unauthorized, "", "");

        await Assert.ThrowsAsync<ApiException>(() => _api.ListGamesAsync());

        Assert.False(_auth.IsLoggedIn);
    }

    [Fact]
    public async Task Response_without_problem_details_still_gives_a_readable_message()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(() => _api.GetSetupAsync());

        Assert.Equal(HttpStatusCode.NotFound, exception.Status);
        Assert.Equal("Er ging iets mis (404).", exception.Message);
    }
}
