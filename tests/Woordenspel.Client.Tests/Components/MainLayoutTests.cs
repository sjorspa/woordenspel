using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Woordenspel.Client.Layout;
using Woordenspel.Client.Services;
using Woordenspel.Client.Tests.Pages;
using static Woordenspel.Client.Tests.TestData;

namespace Woordenspel.Client.Tests.Components;

public class MainLayoutTests : PageTestContext
{
    [Fact]
    public async Task Guests_get_a_badge_next_to_their_name()
    {
        await Services.GetRequiredService<AuthState>().SignInAsync(AnnaLogin with { Username = "SnelleVos42", IsGuest = true });

        var layout = Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(_ => { })));

        Assert.Contains("SnelleVos42", layout.Find(".user").TextContent);
        Assert.Equal("Gast", layout.Find(".badge").TextContent);
    }

    [Fact]
    public async Task Registered_users_have_no_badge()
    {
        await SignInAsync();

        var layout = Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(_ => { })));

        Assert.Empty(layout.FindAll(".badge"));
    }

    [Fact]
    public async Task Signing_out_goes_to_the_login_page()
    {
        await SignInAsync();
        var layout = Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(_ => { })));

        layout.Find("button.link").Click();

        Assert.Equal("/login", CurrentPath);
        Assert.Empty(layout.FindAll(".user"));
    }
}

public class MainLayoutRealtimeTests : PageTestContext
{
    [Fact]
    public async Task Logged_in_user_connects_so_others_see_them_online()
    {
        await SignInAsync();

        Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(_ => { })));

        Assert.Equal(1, Updates.ConnectCount);
    }

    [Fact]
    public void Visitors_who_are_not_logged_in_do_not_connect()
    {
        Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(_ => { })));

        Assert.Equal(0, Updates.ConnectCount);
    }

    [Fact]
    public async Task Logging_in_after_the_page_loaded_also_connects()
    {
        var layout = Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(_ => { })));
        Assert.Equal(0, Updates.ConnectCount);

        await layout.InvokeAsync(SignInAsync);

        layout.WaitForAssertion(() => Assert.Equal(1, Updates.ConnectCount));
    }

    [Fact]
    public async Task When_a_game_starts_both_players_are_taken_to_it()
    {
        await SignInAsync();
        var layout = Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(_ => { })));

        await layout.InvokeAsync(() => Updates.StartGameAsync(GameState()));

        Assert.Equal($"/games/{GameId}", CurrentPath);
    }
}
