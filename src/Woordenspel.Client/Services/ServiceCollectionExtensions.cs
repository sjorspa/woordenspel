using Woordenspel.Contracts;

namespace Woordenspel.Client.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWoordenspelClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddScoped(_ => new HttpClient { BaseAddress = baseAddress });
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IBrowserStorage, LocalBrowserStorage>();
        services.AddScoped<AuthState>();
        services.AddScoped<ApiClient>();
        services.AddScoped<SetupCache>();
        services.AddScoped<IGameUpdates, SignalRGameUpdates>();
        return services;
    }
}

/// <summary>De bordindeling en letterwaarden veranderen niet; die hoeven maar één keer opgehaald te worden.</summary>
public sealed class SetupCache(ApiClient api)
{
    private Task<SetupDto>? _setup;

    public Task<SetupDto> GetAsync() => _setup ??= api.GetSetupAsync();
}
