namespace Woordenspel.Contracts;

public sealed record RegisterRequest(string? Username, string? Password);

public sealed record LoginRequest(string? Username, string? Password);

public sealed record AuthResponse(Guid UserId, string Username, string Token, DateTimeOffset ExpiresAt, bool IsGuest = false);

/// <param name="Opponent">De gebruikersnaam van de tegenstander.</param>
public sealed record CreateGameRequest(string? Opponent);

public sealed record PlaceTilesRequest(IReadOnlyList<TilePlacementDto>? Tiles);

/// <param name="Letters">De te ruilen letters; gebruik "?" voor een blanco steen.</param>
public sealed record SwapTilesRequest(IReadOnlyList<char>? Letters);
