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

    /// <summary>Een nieuw spel waarin je meedoet is begonnen.</summary>
    event Func<GameDto, Task>? GameStarted;

    /// <summary>De uitnodigingen in het overzicht zijn veranderd.</summary>
    event Func<Task>? SeeksChanged;

    /// <summary>De verbinding is hersteld; updates van tussendoor kunnen gemist zijn.</summary>
    event Func<Task>? Reconnected;

    Task EnsureConnectedAsync();
}

public sealed class SignalRGameUpdates(NavigationManager navigation, AuthState auth) : IGameUpdates, IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private HubConnection? _connection;

    public event Func<GameDto, Task>? GameUpdated;

    public event Func<GameDto, Task>? GameStarted;

    public event Func<Task>? SeeksChanged;

    public event Func<Task>? Reconnected;

    public async Task EnsureConnectedAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await ConnectAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task ConnectAsync()
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
            _connection.On<GameDto>(GameHubContract.GameStarted, game => GameStarted?.Invoke(game) ?? Task.CompletedTask);
            _connection.On(GameHubContract.SeeksChanged, () => SeeksChanged?.Invoke() ?? Task.CompletedTask);
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

    /// <summary>
    /// Na uitloggen verbreken we de verbinding (je bent dan niet meer online); na inloggen verbinden we opnieuw,
    /// met het nieuwe token.
    /// </summary>
    private async void OnAuthChanged()
    {
        await _lock.WaitAsync();
        try
        {
            if (_connection is not null)
            {
                await _connection.StopAsync();
                await ConnectAsync();
            }
        }
        catch (Exception)
        {
            // Mislukt dit, dan verbindt de volgende EnsureConnectedAsync opnieuw.
        }
        finally
        {
            _lock.Release();
        }
    }
}
