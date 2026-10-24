using Microsoft.AspNetCore.Identity;
using Woordenspel.Domain.Users;

namespace Woordenspel.Infrastructure.Security;

/// <summary>Gebruikt de PBKDF2-hasher van ASP.NET Core Identity, zonder de rest van Identity.</summary>
public sealed class IdentityPasswordHasher : Application.Abstractions.IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(null!, passwordHash, password) is not PasswordVerificationResult.Failed;
}
