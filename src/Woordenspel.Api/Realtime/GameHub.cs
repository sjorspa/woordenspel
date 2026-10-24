using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Woordenspel.Api.Auth;

namespace Woordenspel.Api.Realtime;

/// <summary>
/// Realtime-verbinding voor spelers. Clients hoeven niets aan te roepen: de server stuurt
/// <see cref="Contracts.GameHubContract.GameUpdated"/> naar iedereen die meedoet aan een gewijzigd spel.
/// </summary>
[Authorize]
public sealed class GameHub : Hub;

/// <summary>Koppelt SignalR-verbindingen aan de gebruikers-id uit het token.</summary>
public sealed class UserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) => connection.User.GetUserId().ToString();
}
