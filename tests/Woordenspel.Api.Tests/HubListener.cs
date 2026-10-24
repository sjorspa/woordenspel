using Microsoft.AspNetCore.SignalR.Client;
using Woordenspel.Contracts;

namespace Woordenspel.Api.Tests;

/// <summary>Een realtime-verbinding als één gebruiker, met alles wat er binnenkwam.</summary>
public sealed class HubListener : IAsyncDisposable
{
    public int SeeksChangedCount;

    public HubConnection Connection { get; set; } = default!;

    public List<GameDto> Updates { get; } = [];

    public List<GameDto> Started { get; } = [];

    public void Record(List<GameDto> list, GameDto game)
    {
        lock (list)
        {
            list.Add(game);
        }
    }

    /// <summary>Wacht tot er in <paramref name="list"/> een spel binnenkomt dat aan de voorwaarde voldoet.</summary>
    public static async Task<GameDto> WaitForAsync(List<GameDto> list, Func<GameDto, bool> predicate)
    {
        var giveUpAt = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (DateTime.UtcNow < giveUpAt)
        {
            lock (list)
            {
                if (list.LastOrDefault(predicate) is { } match)
                {
                    return match;
                }
            }

            await Task.Delay(20);
        }

        throw new TimeoutException("Het verwachte bericht kwam niet binnen.");
    }

    public ValueTask DisposeAsync() => Connection.DisposeAsync();
}
