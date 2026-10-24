using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Woordenspel.Contracts;

namespace Woordenspel.Client.Services;

/// <summary>Realtime spelupdates van de server.</summary>
public interface IGameUpdates
{
    /// <summary>Een spel waarin je meedoet is gewijzigd.</summary>
    event Func<GameDto, Task>? GameUpdated;

    /// <summary>De verbinding is hersteld; updates van tussendoor kunnen gemist zijn.</summary>
    event Func<Task>? Reconnected;

    Task EnsureConnectedAsync();
}

public sealed class SignalRGameUpdates(NavigationManager navigation, AuthState auth) : IGameUpdates, IAsyncDisposable
{
    private HubConnection? _connection;

    public event Func<GameDto, Task>? GameUpdated;

    public event Func<Task>? Reconnected;

    public async Task EnsureConnectedAsync()
    {
        if (_connection is null)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(navigation.ToAbsoluteUri(GameHubContract.Path.TrimStart('/')), options =>
                    options.AccessTokenProvider = () => Task.FromResult(auth.User?.Token))
                .WithAutomaticReconnect()
                .AddJsonProtocol(options => ContractJson.Configure(options.PayloadSerializerOptions))
                .Build();

            _connection.On<GameDto>(GameHubContract.GameUpdated, game => GameUpdated?.Invoke(game) ?? Task.CompletedTask);
            _connection.Reconnected += _ => Reconnected?.Invoke() ?? Task.CompletedTask;
            auth.Changed += OnAuthChanged;
        }

        if (_connection.State == HubConnectionState.Disconnected && auth.IsLoggedIn)
        {
            await _connection.StartAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        auth.Changed -= OnAuthChanged;
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    /// <summary>Na uitloggen of inloggen als iemand anders moet de verbinding opnieuw, met het nieuwe token.</summary>
    private async void OnAuthChanged()
    {
        try
        {
            if (_connection is not null)
            {
                await _connection.StopAsync();
            }
        }
        catch (Exception)
        {
            // Stoppen mag mislukken; bij de volgende EnsureConnectedAsync wordt opnieuw verbonden.
        }
    }
}
