using System.Collections.Concurrent;
using Woordenspel.Application.Abstractions;

namespace Woordenspel.Api.Realtime;

/// <summary>
/// Houdt bij wie er een realtime-verbinding heeft. Iemand kan meerdere tabbladen open hebben; hij is online
/// zolang er minstens één verbinding is.
/// </summary>
public sealed class PresenceTracker : IPresence
{
    private readonly ConcurrentDictionary<Guid, int> _connections = new();

    public bool IsOnline(Guid userId) => _connections.TryGetValue(userId, out var count) && count > 0;

    /// <returns>True als de gebruiker hierdoor online kwam.</returns>
    public bool Connect(Guid userId) => _connections.AddOrUpdate(userId, 1, (_, count) => count + 1) == 1;

    /// <returns>True als de gebruiker hierdoor offline ging.</returns>
    public bool Disconnect(Guid userId)
    {
        while (_connections.TryGetValue(userId, out var count))
        {
            if (count <= 1)
            {
                if (_connections.TryRemove(new KeyValuePair<Guid, int>(userId, count)))
                {
                    return true;
                }
            }
            else if (_connections.TryUpdate(userId, count - 1, count))
            {
                return false;
            }
        }

        return false;
    }
}
