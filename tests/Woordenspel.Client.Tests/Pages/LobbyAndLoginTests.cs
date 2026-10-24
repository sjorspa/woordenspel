using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Woordenspel.Client.Services;
using Woordenspel.Client.Pages;
using Woordenspel.Contracts;
using Woordenspel.Domain.Games;
using Woordenspel.TestSupport;
using static Woordenspel.Client.Tests.TestData;

namespace Woordenspel.Client.Tests.Pages;

public class LobbyTests : PageTestContext
{
    [Fact]
    public async Task Lists_running_and_finished_games_with_whose_turn_it_is()
    {
        Server.On("GET /api/games", new List<GameSummaryDto>
        {
            Summary(GameStatus.InProgress, Anna),
            Summary(GameStatus.Finished, null, winners: [Bram]),
        });
        await SignInAsync();

        var lobby = Render<Lobby>();

        lobby.WaitForAssertion(() => Assert.Equal(2, lobby.FindAll(".game-list li").Count));
        var running = lobby.FindAll(".game-list li")[0];
        Assert.Contains("your-turn", running.ClassList);
        Assert.Contains("Jouw beurt", running.TextContent);
        Assert.Equal(2, running.QuerySelectorAll(".clock").Length);
        Assert.Contains("bram heeft gewonnen.", lobby.FindAll(".game-list li")[1].TextContent);
    }

    [Theory]
    [InlineData(true, "Geef die aan je tegenstander")]
    [InlineData(false, "Daarmee kunnen anderen jou uitdagen")]
    public async Task Shows_your_own_name_so_you_can_share_it(bool isGuest, string hint)
    {
        Server.On("GET /api/games", new List<GameSummaryDto>());
        await Services.GetRequiredService<AuthState>().SignInAsync(AnnaLogin with { Username = "SnelleVos42", IsGuest = isGuest });

        var lobby = Render<Lobby>();

        Assert.Equal("SnelleVos42", lobby.Find(".own-name").TextContent);
        Assert.Contains(hint, lobby.Find(".card").TextContent);
    }

    [Fact]
    public async Task Live_update_reloads_the_list()
    {
        Server.On("GET /api/games", new List<GameSummaryDto>());
        await SignInAsync();
        var lobby = Render<Lobby>();
        lobby.WaitForElement(".card");

        Server.On("GET /api/games", new List<GameSummaryDto> { Summary(GameStatus.InProgress, Bram) });
        await lobby.InvokeAsync(() => Updates.PushAsync(GameState()));

        lobby.WaitForAssertion(() => Assert.Contains("Wacht op tegenstander", lobby.Find(".game-list").TextContent));
    }

    [Fact]
    public async Task Creating_a_game_opens_it()
    {
        Server.On("GET /api/games", new List<GameSummaryDto>());
        Server.On("POST /api/games", GameState(), HttpStatusCode.Created);
        await SignInAsync();
        var lobby = Render<Lobby>();

        lobby.Find("input").Change("bram");
        lobby.Find("form").Submit();

        lobby.WaitForAssertion(() => Assert.Equal($"/games/{GameId}", CurrentPath));
        Assert.Contains("\"opponent\":\"bram\"", Server.BodiesFor("POST /api/games").Single());
    }

    [Fact]
    public async Task Unknown_opponent_shows_the_error()
    {
        Server.On("GET /api/games", new List<GameSummaryDto>());
        Server.OnProblem("POST /api/games", HttpStatusCode.NotFound, "UserNotFound", "Gebruiker 'xyz' bestaat niet.");
        await SignInAsync();
        var lobby = Render<Lobby>();

        lobby.Find("input").Change("xyz");
        lobby.Find("form").Submit();

        lobby.WaitForAssertion(() => Assert.Equal("Gebruiker 'xyz' bestaat niet.", lobby.Find(".error").TextContent));
    }

    private static GameSummaryDto Summary(GameStatus status, Guid? currentPlayer, IReadOnlyList<Guid>? winners = null) =>
        new(Guid.NewGuid(), status, FakeTimeProvider.Start, currentPlayer, currentPlayer == Anna, null,
            [new(Anna, "anna", 10, 7, false, 200_000), new(Bram, "bram", 12, 7, false, 250_000)], winners ?? []);
}

public class LoginTests : PageTestContext
{
    [Fact]
    public void Logging_in_stores_the_user_and_goes_to_the_lobby()
    {
        Server.On("POST /api/account/login", AnnaLogin);
        var login = Render<Login>();

        login.Find("input[autocomplete='username']").Change("anna");
        login.Find("input[type='password']").Change("geheim123");
        login.Find("form").Submit();

        login.WaitForAssertion(() => Assert.Equal("/", CurrentPath));
        Assert.Contains("token-anna", Storage.Items["woordenspel.auth"]);
    }

    [Fact]
    public void Registering_uses_the_register_endpoint()
    {
        Server.On("POST /api/account/register", AnnaLogin, HttpStatusCode.Created);
        var login = Render<Login>();

        login.Find("input[autocomplete='username']").Change("anna");
        login.Find("input[type='password']").Change("geheim123");
        login.FindAll("button").Single(b => b.TextContent == "Registreren").Click();

        login.WaitForAssertion(() => Assert.Equal("/", CurrentPath));
    }

    [Fact]
    public void Playing_as_guest_needs_no_name_or_password()
    {
        Server.On("POST /api/account/guest", AnnaLogin with { Username = "SnelleVos42", IsGuest = true }, HttpStatusCode.Created);
        var login = Render<Login>();

        login.FindAll("button").Single(b => b.TextContent == "Speel als gast").Click();

        login.WaitForAssertion(() => Assert.Equal("/", CurrentPath));
        Assert.Contains("SnelleVos42", Storage.Items["woordenspel.auth"]);
        Assert.Null(Server.Requests.Single().Body);
    }

    [Fact]
    public void Wrong_password_shows_the_message()
    {
        Server.OnProblem("POST /api/account/login", HttpStatusCode.Unauthorized, "AuthenticationFailed", "Gebruikersnaam of wachtwoord klopt niet.");
        var login = Render<Login>();

        login.Find("input[autocomplete='username']").Change("anna");
        login.Find("input[type='password']").Change("fout");
        login.Find("form").Submit();

        login.WaitForAssertion(() => Assert.Equal("Gebruikersnaam of wachtwoord klopt niet.", login.Find(".error").TextContent));
        Assert.Empty(Storage.Items);
    }
}
