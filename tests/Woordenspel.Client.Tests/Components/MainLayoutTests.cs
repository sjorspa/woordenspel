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
