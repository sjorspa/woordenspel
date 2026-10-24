using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Woordenspel.Api.Auth;
using Woordenspel.Contracts;

namespace Woordenspel.Api.Realtime;

/// <summary>
/// Realtime-verbinding voor spelers. Clients hoeven niets aan te roepen: de server stuurt
/// <see cref="Contracts.GameHubContract.GameUpdated"/> naar iedereen die meedoet aan een gewijzigd spel.
/// </summary>
[Authorize]
public sealed class GameHub(PresenceTracker presence) : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (presence.Connect(Context.User!.GetUserId()))
        {
            // Uitnodigingen van deze speler zijn weer zichtbaar.
            await Clients.All.SendAsync(GameHubContract.SeeksChanged);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (presence.Disconnect(Context.User!.GetUserId()))
        {
            await Clients.All.SendAsync(GameHubContract.SeeksChanged);
        }

        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>Koppelt SignalR-verbindingen aan de gebruikers-id uit het token.</summary>
public sealed class UserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) => connection.User.GetUserId().ToString();
}
