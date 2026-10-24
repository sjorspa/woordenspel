using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Woordenspel.Client.Services;
using Woordenspel.TestSupport;

namespace Woordenspel.Client.Tests.Pages;

/// <summary>Een pagina met de echte services, maar met een nep-server, nep-opslag en nep-realtime-updates.</summary>
public abstract class PageTestContext : BunitContext
{
    protected PageTestContext()
    {
        Services.AddSingleton<TimeProvider>(Clock);
        Services.AddSingleton<IBrowserStorage>(Storage);
        Services.AddSingleton<IGameUpdates>(Updates);
        Services.AddSingleton(new HttpClient(Server) { BaseAddress = new Uri("http://localhost/") });
        Services.AddSingleton<AuthState>();
        Services.AddSingleton<ApiClient>();
        Services.AddSingleton<SetupCache>();
    }

    internal FakeApi Server { get; } = new();

    internal FakeGameUpdates Updates { get; } = new();

    internal FakeBrowserStorage Storage { get; } = new();

    internal FakeTimeProvider Clock { get; } = new();

    protected string CurrentPath => new Uri(Services.GetRequiredService<NavigationManager>().Uri).AbsolutePath;

    protected Task SignInAsync() => Services.GetRequiredService<AuthState>().SignInAsync(TestData.AnnaLogin);
}
