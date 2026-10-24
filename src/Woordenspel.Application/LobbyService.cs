using Woordenspel.Application.Abstractions;
using Woordenspel.Contracts;
using Woordenspel.Domain.Games;
using Woordenspel.Domain.Seeks;
using Woordenspel.Domain.Users;

namespace Woordenspel.Application;

/// <summary>
/// Spelers zoeken een tegenstander: ze maken een uitnodiging (open, of gericht aan één speler) en het spel
/// begint zodra iemand meedoet. Een uitnodiging telt alleen zolang de maker online is.
/// </summary>
public sealed class LobbyService(
    ISeekRepository seeks,
    IUserRepository users,
    IPresence presence,
    IGameNotifier notifier,
    GameService games,
    GameSetup setup,
    TimeProvider timeProvider)
{
    /// <summary>Maakt een uitnodiging; een eerdere uitnodiging van dezelfde speler vervalt.</summary>
    /// <param name="opponentUsername">Wie je uitdaagt; leeg voor een open spel.</param>
    public async Task<SeekDto> CreateSeekAsync(Guid creatorId, string? opponentUsername, CancellationToken cancellationToken = default)
    {
        var creator = Identity(await users.FindByIdAsync(creatorId, cancellationToken)
            ?? throw new InvalidOperationException($"Ingelogde gebruiker {creatorId} bestaat niet."));

        GameSeek seek;
        if (string.IsNullOrWhiteSpace(opponentUsername))
        {
            seek = GameSeek.Open(Guid.NewGuid(), creator, timeProvider.GetUtcNow());
        }
        else
        {
            var opponent = await users.FindByUsernameAsync(opponentUsername, cancellationToken)
                ?? throw new UserNotFoundException(opponentUsername.Trim());
            seek = GameSeek.Challenge(Guid.NewGuid(), creator, Identity(opponent), timeProvider.GetUtcNow());
        }

        await seeks.RemoveByCreatorAsync(creatorId, cancellationToken);
        await seeks.AddAsync(seek, cancellationToken);
        await notifier.SeeksChangedAsync(cancellationToken);
        return ToDto(seek, creatorId);
    }

    /// <summary>
    /// De uitnodigingen die deze speler kan zien: zijn eigen, open spellen en uitdagingen aan hem, maar alleen
    /// van spelers die online zijn. Uitdagingen aan hem staan bovenaan.
    /// </summary>
    public async Task<IReadOnlyList<SeekDto>> ListSeeksAsync(Guid viewerId, CancellationToken cancellationToken = default) =>
        (await seeks.ListAsync(cancellationToken))
            .Where(s => s.IsVisibleTo(viewerId) && (s.Creator.Id == viewerId || presence.IsOnline(s.Creator.Id)))
            .OrderByDescending(s => s.Opponent?.Id == viewerId)
            .ThenBy(s => s.CreatedAt)
            .Select(s => ToDto(s, viewerId))
            .ToList();

    /// <summary>Doet mee met een uitnodiging: het spel begint en beide spelers krijgen het te zien.</summary>
    public async Task<GameDto> JoinAsync(Guid seekId, Guid joinerId, CancellationToken cancellationToken = default)
    {
        var seek = await seeks.FindAsync(seekId, cancellationToken) ?? throw new SeekNotFoundException(seekId);
        var joiner = Identity(await users.FindByIdAsync(joinerId, cancellationToken)
            ?? throw new InvalidOperationException($"Ingelogde gebruiker {joinerId} bestaat niet."));

        var players = seek.Accept(joiner);
        if (!presence.IsOnline(seek.Creator.Id))
        {
            throw new SeekUnavailableException(seek.Creator.Name);
        }

        if (await seeks.TakeAsync(seekId, cancellationToken) is null)
        {
            throw new SeekNotFoundException(seekId);
        }

        // Wie meedoet zoekt niet meer zelf een tegenstander.
        await seeks.RemoveByCreatorAsync(joinerId, cancellationToken);

        var game = await games.StartGameAsync(players, joinerId, cancellationToken);
        await notifier.SeeksChangedAsync(cancellationToken);
        return game;
    }

    /// <summary>Trekt een uitnodiging in. Alleen de maker kan dat; een onbekende uitnodiging is al weg.</summary>
    public async Task CancelAsync(Guid seekId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (await seeks.FindAsync(seekId, cancellationToken) is not { } seek || seek.Creator.Id != userId)
        {
            throw new SeekNotFoundException(seekId);
        }

        await seeks.TakeAsync(seekId, cancellationToken);
        await notifier.SeeksChangedAsync(cancellationToken);
    }

    private SeekDto ToDto(GameSeek seek, Guid viewerId) =>
        new(
            seek.Id,
            seek.Creator.Id,
            seek.Creator.Name,
            seek.Opponent?.Id,
            seek.Opponent?.Name,
            seek.Creator.Id == viewerId,
            setup.Rules.TimePerPlayer is { } time ? (long)time.TotalMilliseconds : null,
            seek.CreatedAt);

    private static PlayerIdentity Identity(User user) => new(user.Id, user.Username);
}
