using Woordenspel.Client.Pages;
using Woordenspel.Contracts;
using Woordenspel.Domain.Games;
using static Woordenspel.Client.Tests.TestData;

namespace Woordenspel.Client.Tests.Pages;

public class GamePageTests : PageTestContext
{
    private static readonly string GameUrl = $"/api/games/{GameId}";

    public GamePageTests()
    {
        Server.On("GET /api/setup", Setup);
        Server.On($"GET {GameUrl}", GameState());
    }

    [Fact]
    public async Task Shows_both_players_with_their_clocks_and_my_rack()
    {
        var page = await RenderPageAsync();

        var bars = page.FindAll(".player-bar");
        Assert.Contains("bram", bars[0].TextContent);
        Assert.Contains("Jij", bars[1].TextContent);
        Assert.All(page.FindAll(".clock"), clock => Assert.Equal("5:00", clock.TextContent));
        Assert.Contains("to-move", bars[1].ClassList);
        Assert.Equal(7, page.FindAll(".rack-slot .tile").Count);
        Assert.Equal(1, Updates.ConnectCount);
    }

    [Fact]
    public async Task Placing_tiles_shows_the_score_and_playing_sends_the_move()
    {
        Server.On($"POST {GameUrl}/preview", new PreviewDto(6, false, [new("KAT", 6)]));
        var afterMove = GameState(rack: "EENZAAM", currentPlayer: Bram, tiles: [new(7, 7, 'K', false, 3), new(7, 8, 'A', false, 1), new(7, 9, 'T', false, 2)]);
        Server.On($"POST {GameUrl}/place", new MoveResultDto(new MoveDto(Anna, MoveType.Placement, 6, ["KAT"], 3, [new(7, 7), new(7, 8), new(7, 9)]), afterMove));
        var page = await RenderPageAsync();

        PlaceFromRack(page, slot: 0, row: 7, column: 7);
        PlaceFromRack(page, slot: 1, row: 7, column: 8);
        PlaceFromRack(page, slot: 2, row: 7, column: 9);

        page.WaitForAssertion(() => Assert.Equal("KAT (6) = 6 punten", page.Find(".status-line").TextContent), TimeSpan.FromSeconds(3));
        page.FindAll("button").Single(b => b.TextContent == "Spelen").Click();

        page.WaitForAssertion(() => Assert.Equal(3, page.FindAll(".square .tile:not(.pending)").Count));
        var body = Assert.Single(Server.BodiesFor($"POST {GameUrl}/place"));
        Assert.Contains("\"row\":7,\"column\":7,\"letter\":\"K\"", body);
        Assert.Equal("bram is aan de beurt", page.Find(".turn-banner").TextContent.Trim());
        Assert.Equal("EENZAAM", new string(page.FindAll(".rack-slot .letter").Select(l => l.TextContent[0]).ToArray()));
    }

    [Fact]
    public async Task Invalid_words_are_shown_while_placing()
    {
        Server.OnProblem($"POST {GameUrl}/preview", System.Net.HttpStatusCode.UnprocessableEntity, "InvalidWords", "Ongeldige woorden: TK.", "TK");
        var page = await RenderPageAsync();

        PlaceFromRack(page, slot: 2, row: 7, column: 7);
        PlaceFromRack(page, slot: 0, row: 7, column: 8);

        page.WaitForAssertion(() => Assert.Equal("Ongeldige woorden: TK.", page.Find(".status-line.error").TextContent), TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task Clicking_a_pending_tile_puts_it_back()
    {
        var page = await RenderPageAsync();
        PlaceFromRack(page, slot: 0, row: 7, column: 7);

        Square(page, 7, 7).Click();

        Assert.Empty(page.FindAll(".tile.pending"));
        Assert.Empty(page.FindAll(".rack-slot.empty"));
    }

    [Fact]
    public async Task Placing_a_blank_asks_for_its_letter()
    {
        var page = await RenderPageAsync();

        page.Find("[data-slot='3']").Click();
        Square(page, 7, 7).Click();
        page.FindAll(".letter-choice").Single(b => b.TextContent == "Q").Click();

        var tile = Square(page, 7, 7).QuerySelector(".tile.pending.blank");
        Assert.Equal("Q", tile?.QuerySelector(".letter")?.TextContent);
        Assert.Empty(page.FindAll(".overlay"));
    }

    [Fact]
    public async Task Turn_banner_shows_whose_turn_it_is()
    {
        var page = await RenderPageAsync();

        Assert.Equal("Jouw beurt", page.Find(".turn-banner.mine").TextContent.Trim());
    }

    [Fact]
    public async Task When_it_is_not_my_turn_the_page_says_so_and_moves_are_disabled()
    {
        Server.On($"GET {GameUrl}", GameState(currentPlayer: Bram));
        Server.On($"POST {GameUrl}/preview", new PreviewDto(3, false, [new("KA", 3)]));
        var page = await RenderPageAsync();

        PlaceFromRack(page, slot: 0, row: 7, column: 7);

        Assert.Equal("bram is aan de beurt", page.Find(".turn-banner").TextContent.Trim());
        Assert.DoesNotContain("mine", page.Find(".turn-banner").ClassList);
        Assert.True(Button(page, "Wacht op je beurt").HasAttribute("disabled"));
        Assert.True(Button(page, "Passen").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Play_button_is_enabled_once_tiles_are_placed_on_my_turn()
    {
        var page = await RenderPageAsync();
        Assert.True(Button(page, "Spelen").HasAttribute("disabled"));

        PlaceFromRack(page, slot: 0, row: 7, column: 7);

        Assert.False(Button(page, "Spelen").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Live_update_from_the_opponent_is_shown()
    {
        var page = await RenderPageAsync();

        await page.InvokeAsync(() => Updates.PushAsync(GameState(
            currentPlayer: Anna,
            tiles: [new(7, 7, 'Z', false, 5), new(7, 8, 'O', false, 1)],
            moves: [new(Bram, MoveType.Placement, 6, ["ZO"], 2, [new(7, 7), new(7, 8)])],
            bramMs: 250_000)));

        page.WaitForAssertion(() => Assert.Equal(2, page.FindAll(".tile.last-move").Count));
        Assert.Equal("4:10", page.FindAll(".player-bar")[0].QuerySelector(".clock")!.TextContent);
        Assert.Contains("bram: ZO (6)", page.Find(".moves").TextContent);
    }

    [Fact]
    public async Task Updates_for_other_games_are_ignored()
    {
        var page = await RenderPageAsync();

        await page.InvokeAsync(() => Updates.PushAsync(GameState(tiles: [new(0, 0, 'X', false, 8)]) with { Id = Guid.NewGuid() }));

        Assert.Empty(page.FindAll(".square .tile"));
    }

    [Fact]
    public async Task Finished_game_shows_the_result()
    {
        Server.On($"GET {GameUrl}", GameState(status: GameStatus.Finished, winners: [Anna],
            moves: [new(Bram, MoveType.Timeout, 0, [], 0, [])]));

        var page = await RenderPageAsync();

        Assert.Equal("Je hebt gewonnen!", page.Find(".result h2").TextContent);
        Assert.Contains("bram was door de tijd heen", page.Find(".moves").TextContent);
        Assert.Empty(page.FindAll(".actions button"));
    }

    [Fact]
    public async Task Resigning_asks_for_confirmation()
    {
        Server.On($"POST {GameUrl}/resign", new MoveResultDto(
            new MoveDto(Anna, MoveType.Resign, 0, [], 0, []), GameState(status: GameStatus.Finished, winners: [Bram])));
        var page = await RenderPageAsync();

        Button(page, "Opgeven").Click();
        Assert.Empty(Server.BodiesFor($"POST {GameUrl}/resign"));
        Button(page, "Zeker weten?").Click();

        page.WaitForAssertion(() => Assert.Equal("bram heeft gewonnen.", page.Find(".result h2").TextContent));
    }

    [Fact]
    public async Task Not_logged_in_goes_to_the_login_page()
    {
        Render<GamePage>(p => p.Add(g => g.GameId, GameId));

        await Task.Yield();
        Assert.Equal("/login", CurrentPath);
    }

    private async Task<IRenderedComponent<GamePage>> RenderPageAsync()
    {
        await SignInAsync();
        var page = Render<GamePage>(p => p.Add(g => g.GameId, GameId));
        page.WaitForElement(".board");
        return page;
    }

    private static void PlaceFromRack(IRenderedComponent<GamePage> page, int slot, int row, int column)
    {
        page.Find($"[data-slot='{slot}']").Click();
        Square(page, row, column).Click();
    }

    private static AngleSharp.Dom.IElement Square(IRenderedComponent<GamePage> page, int row, int column) =>
        page.Find($"[data-row='{row}'][data-column='{column}']");

    private static AngleSharp.Dom.IElement Button(IRenderedComponent<GamePage> page, string text) =>
        page.FindAll("button").Single(b => b.TextContent.Trim() == text);
}
