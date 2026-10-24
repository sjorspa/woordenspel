namespace Woordenspel.Application;

public sealed class GameNotFoundException(Guid gameId) : Exception($"Spel {gameId} bestaat niet.")
{
    public Guid GameId { get; } = gameId;
}

public sealed class SeekNotFoundException(Guid seekId)
    : Exception("Dit spel is niet meer beschikbaar; misschien was iemand anders je voor.")
{
    public Guid SeekId { get; } = seekId;
}

public sealed class SeekUnavailableException(string creatorName)
    : Exception($"{creatorName} is niet meer online.");

public sealed class UserNotFoundException(string username) : Exception($"Gebruiker '{username}' bestaat niet.");

public sealed class UsernameTakenException(string username) : Exception($"De gebruikersnaam '{username}' is al in gebruik.");

/// <summary>Bewust één melding voor een onbekende gebruiker én een fout wachtwoord.</summary>
public sealed class AuthenticationFailedException() : Exception("Gebruikersnaam of wachtwoord klopt niet.");

/// <summary>Het spel is tussen laden en opslaan door een ander verzoek gewijzigd.</summary>
public sealed class ConcurrencyConflictException(Exception? innerException = null)
    : Exception("Het spel is intussen gewijzigd. Haal de nieuwe stand op en probeer het opnieuw.", innerException);
