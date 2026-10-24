using Woordenspel.Domain.Games;

namespace Woordenspel.Domain.Seeks;

/// <summary>
/// Een uitnodiging voor een spel: open voor iedereen, of gericht aan één tegenstander. Het spel (en de klok)
/// begint pas als iemand meedoet, zodat niemand tijd verliest terwijl de ander er nog niet is.
/// </summary>
public sealed class GameSeek
{
    private GameSeek(Guid id, PlayerIdentity creator, PlayerIdentity? opponent, DateTimeOffset createdAt)
    {
        Id = id;
        Creator = creator;
        Opponent = opponent;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public PlayerIdentity Creator { get; }

    /// <summary>De uitgedaagde speler; null bij een open spel waaraan iedereen mag meedoen.</summary>
    public PlayerIdentity? Opponent { get; }

    public DateTimeOffset CreatedAt { get; }

    public bool IsOpen => Opponent is null;

    public static GameSeek Open(Guid id, PlayerIdentity creator, DateTimeOffset now) => new(id, creator, null, now);

    public static GameSeek Challenge(Guid id, PlayerIdentity creator, PlayerIdentity opponent, DateTimeOffset now)
    {
        if (creator.Id == opponent.Id)
        {
            throw new DomainException(DomainErrors.DuplicatePlayer, "Je kunt jezelf niet uitdagen.");
        }

        return new GameSeek(id, creator, opponent, now);
    }

    /// <summary>Of deze speler de uitnodiging in zijn overzicht hoort te zien.</summary>
    public bool IsVisibleTo(Guid userId) => IsOpen || Opponent?.Id == userId || Creator.Id == userId;

    /// <summary>Controleert of deze speler mag meedoen.</summary>
    /// <returns>De spelers voor het nieuwe spel: de maker en wie meedoet.</returns>
    public IReadOnlyList<PlayerIdentity> Accept(PlayerIdentity joiner)
    {
        if (joiner.Id == Creator.Id)
        {
            throw new DomainException(DomainErrors.DuplicatePlayer, "Je kunt niet meedoen met je eigen spel.");
        }

        if (Opponent is { } opponent && opponent.Id != joiner.Id)
        {
            throw new DomainException(DomainErrors.SeekNotForYou, $"Deze uitdaging is voor {opponent.Name}.");
        }

        return [Creator, joiner];
    }
}
