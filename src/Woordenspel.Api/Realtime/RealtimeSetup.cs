using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using Woordenspel.Application.Abstractions;

namespace Woordenspel.Api.Realtime;

public static class RealtimeSetup
{
    public static IServiceCollection AddRealtime(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSignalR().AddJsonProtocol(options =>
            options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddSingleton<IUserIdProvider, UserIdProvider>();
        services.AddSingleton<IGameNotifier, SignalRGameNotifier>();
        services.AddSingleton<PresenceTracker>();
        services.AddSingleton<IPresence>(sp => sp.GetRequiredService<PresenceTracker>());

        services.Configure<TurnExpiryOptions>(configuration.GetSection(TurnExpiryOptions.SectionName));
        services.AddHostedService<TurnExpiryWorker>();
        return services;
    }
}
