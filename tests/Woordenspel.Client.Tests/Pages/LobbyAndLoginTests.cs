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
    public LobbyTests()
    {
        Server.On("GET /api/games", new List<GameSummaryDto>());
        Server.On("GET /api/seeks", new List<SeekDto>());
    }

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

    [Fact]
    public async Task Live_game_update_reloads_the_games()
    {
        await SignInAsync();
        var lobby = Render<Lobby>();
        lobby.WaitForElement(".card");

        Server.On("GET /api/games", new List<GameSummaryDto> { Summary(GameStatus.InProgress, Bram) });
        await lobby.InvokeAsync(() => Updates.PushAsync(GameState()));

        lobby.WaitForAssertion(() => Assert.Contains("Wacht op tegenstander", lobby.Find(".game-list").TextContent));
    }

    [Fact]
    public async Task Shows_waiting_games_with_the_right_action()
    {
        Server.On("GET /api/seeks", new List<SeekDto>
        {
            Seek(Bram, "bram", opponent: Anna, opponentName: "anna"),
            Seek(Bram, "charlie"),
            Seek(Anna, "anna", isMine: true),
        });
        await SignInAsync();

        var lobby = Render<Lobby>();

        lobby.WaitForAssertion(() => Assert.Equal(3, lobby.FindAll(".seek-list li").Count));
        var items = lobby.FindAll(".seek-list li");
        Assert.Contains("bram daagt je uit!", items[0].TextContent);
        Assert.Equal("Accepteren", items[0].QuerySelector("button")!.TextContent);
        Assert.Contains("charlie zoekt een tegenstander", items[1].TextContent);
        Assert.Equal("Meedoen", items[1].QuerySelector("button")!.TextContent);
        Assert.Contains("Jouw open spel", items[2].TextContent);
        Assert.Equal("Annuleren", items[2].QuerySelector("button")!.TextContent);
    }

    [Fact]
    public async Task Nobody_waiting_shows_a_hint()
    {
        await SignInAsync();

        var lobby = Render<Lobby>();

        lobby.WaitForAssertion(() => Assert.Contains("Er wacht nu niemand", lobby.Markup));
    }

    [Fact]
    public async Task Starting_an_open_game_creates_a_seek_without_opponent()
    {
        Server.On("POST /api/seeks", Seek(Anna, "anna", isMine: true), HttpStatusCode.Created);
        await SignInAsync();
        var lobby = Render<Lobby>();

        Server.On("GET /api/seeks", new List<SeekDto> { Seek(Anna, "anna", isMine: true) });
        lobby.FindAll("button").Single(b => b.TextContent == "Open spel starten").Click();

        lobby.WaitForAssertion(() => Assert.Contains("Jouw open spel", lobby.Find(".seek-list").TextContent));
        Assert.Contains("\"opponent\":null", Server.BodiesFor("POST /api/seeks").Single());
    }

    [Fact]
    public async Task Challenging_sends_the_opponents_name()
    {
        Server.On("POST /api/seeks", Seek(Anna, "anna", opponent: Bram, opponentName: "bram", isMine: true), HttpStatusCode.Created);
        await SignInAsync();
        var lobby = Render<Lobby>();

        lobby.Find("input").Change("bram");
        lobby.Find("form").Submit();

        lobby.WaitForAssertion(() => Assert.Contains("\"opponent\":\"bram\"", Server.BodiesFor("POST /api/seeks").Single()));
    }

    [Fact]
    public async Task Joining_opens_the_game()
    {
        var seek = Seek(Bram, "bram");
        Server.On("GET /api/seeks", new List<SeekDto> { seek });
        Server.On($"POST /api/seeks/{seek.Id}/join", GameState(), HttpStatusCode.Created);
        await SignInAsync();
        var lobby = Render<Lobby>();

        lobby.WaitForElement(".seek-list button").Click();

        lobby.WaitForAssertion(() => Assert.Equal($"/games/{GameId}", CurrentPath));
    }

    [Fact]
    public async Task Cancelling_removes_your_seek()
    {
        var seek = Seek(Anna, "anna", isMine: true);
        Server.On("GET /api/seeks", new List<SeekDto> { seek });
        Server.OnEmpty($"DELETE /api/seeks/{seek.Id}");
        await SignInAsync();
        var lobby = Render<Lobby>();

        lobby.WaitForElement(".seek-list button");
        Server.On("GET /api/seeks", new List<SeekDto>());
        lobby.Find(".seek-list button").Click();

        lobby.WaitForAssertion(() => Assert.Empty(lobby.FindAll(".seek-list li")));
    }

    [Fact]
    public async Task Seek_changes_from_others_reload_the_list()
    {
        await SignInAsync();
        var lobby = Render<Lobby>();
        lobby.WaitForAssertion(() => Assert.Contains("Er wacht nu niemand", lobby.Markup));

        Server.On("GET /api/seeks", new List<SeekDto> { Seek(Bram, "bram") });
        await lobby.InvokeAsync(() => Updates.SeeksChangedAsync());

        lobby.WaitForAssertion(() => Assert.Contains("bram zoekt een tegenstander", lobby.Find(".seek-list").TextContent));
    }

    [Fact]
    public async Task Joining_a_seek_that_is_gone_shows_the_error()
    {
        var seek = Seek(Bram, "bram");
        Server.On("GET /api/seeks", new List<SeekDto> { seek });
        Server.OnProblem($"POST /api/seeks/{seek.Id}/join", HttpStatusCode.NotFound, "SeekNotFound", "Dit spel is niet meer beschikbaar.");
        await SignInAsync();
        var lobby = Render<Lobby>();

        lobby.WaitForElement(".seek-list button").Click();

        lobby.WaitForAssertion(() => Assert.Equal("Dit spel is niet meer beschikbaar.", lobby.Find(".error").TextContent));
    }

    [Theory]
    [InlineData(true, "Geef die aan je tegenstander")]
    [InlineData(false, "Daarmee kunnen anderen jou uitdagen")]
    public async Task Shows_your_own_name_so_you_can_share_it(bool isGuest, string hint)
    {
        await Services.GetRequiredService<AuthState>().SignInAsync(AnnaLogin with { Username = "SnelleVos42", IsGuest = isGuest });

        var lobby = Render<Lobby>();

        Assert.Equal("SnelleVos42", lobby.Find(".own-name").TextContent);
        Assert.Contains(hint, lobby.Find(".card").TextContent);
    }

    private static SeekDto Seek(Guid creator, string creatorName, Guid? opponent = null, string? opponentName = null, bool isMine = false) =>
        new(Guid.NewGuid(), creator, creatorName, opponent, opponentName, isMine, 300_000, FakeTimeProvider.Start);

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
