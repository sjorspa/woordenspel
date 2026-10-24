using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Woordenspel.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(GameSetup.Wordfeud);
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<GameService>();
        services.AddScoped<AccountService>();
        return services;
    }
}
