using System.Text.RegularExpressions;

namespace Woordenspel.Domain.Users;

public sealed partial class User
{
    public const int MinUsernameLength = 3;
    public const int MaxUsernameLength = 20;
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;

    private User(Guid id, string username, string? passwordHash, DateTimeOffset createdAt)
    {
        Id = id;
        Username = username;
        NormalizedUsername = Normalize(username);
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Username { get; private set; }

    /// <summary>Voor het hoofdletterongevoelig opzoeken en uniek houden van gebruikersnamen.</summary>
    public string NormalizedUsername { get; private set; }

    /// <summary>Null voor een gast: die heeft geen wachtwoord en kan dus niet opnieuw inloggen.</summary>
    public string? PasswordHash { get; private set; }

    public bool IsGuest => PasswordHash is null;

    public DateTimeOffset CreatedAt { get; private set; }

    /// <param name="passwordHash">De al gehashte versie van een wachtwoord dat <see cref="ValidatePassword"/> heeft doorstaan.</param>
    public static User Register(Guid id, string username, string passwordHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrEmpty(passwordHash);
        return new User(id, ValidateUsername(username), passwordHash, now);
    }

    /// <summary>Een gast speelt zonder wachtwoord, onder een (meestal willekeurig gekozen) naam.</summary>
    public static User RegisterGuest(Guid id, string username, DateTimeOffset now) =>
        new(id, ValidateUsername(username), passwordHash: null, now);

    public static bool IsValidUsername(string username) => UsernamePattern().IsMatch(username);

    public static void ValidatePassword(string password)
    {
        if (password is null || password.Length < MinPasswordLength || password.Length > MaxPasswordLength)
        {
            throw new DomainException(
                DomainErrors.InvalidPassword,
                $"Een wachtwoord heeft {MinPasswordLength} tot {MaxPasswordLength} tekens.");
        }
    }

    public static string Normalize(string username) => username.Trim().ToUpperInvariant();

    private static string ValidateUsername(string username)
    {
        var trimmed = username?.Trim() ?? string.Empty;
        if (!IsValidUsername(trimmed))
        {
            throw new DomainException(
                DomainErrors.InvalidUsername,
                $"Een gebruikersnaam heeft {MinUsernameLength} tot {MaxUsernameLength} tekens: letters, cijfers of '_'.");
        }

        return trimmed;
    }

    [GeneratedRegex("^[A-Za-z0-9_]{3,20}$")]
    private static partial Regex UsernamePattern();
}
