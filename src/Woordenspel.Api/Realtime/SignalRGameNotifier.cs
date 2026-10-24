using Microsoft.AspNetCore.SignalR;
using Woordenspel.Application.Abstractions;
using Woordenspel.Contracts;

namespace Woordenspel.Api.Realtime;

public sealed class SignalRGameNotifier(IHubContext<GameHub> hub) : IGameNotifier
{
    public Task GameUpdatedAsync(Guid playerId, GameDto game, CancellationToken cancellationToken) =>
        hub.Clients.User(playerId.ToString()).SendAsync(GameHubContract.GameUpdated, game, cancellationToken);

    public Task GameStartedAsync(Guid playerId, GameDto game, CancellationToken cancellationToken) =>
        hub.Clients.User(playerId.ToString()).SendAsync(GameHubContract.GameStarted, game, cancellationToken);

    public Task SeeksChangedAsync(CancellationToken cancellationToken) =>
        hub.Clients.All.SendAsync(GameHubContract.SeeksChanged, cancellationToken);
}
