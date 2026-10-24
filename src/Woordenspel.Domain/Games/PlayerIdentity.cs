namespace Woordenspel.Domain.Games;

/// <summary>Wie er meedoet aan een nieuw spel. De id is die van de gebruiker.</summary>
public readonly record struct PlayerIdentity(Guid Id, string Name);
