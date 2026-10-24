using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Woordenspel.Contracts;
using Woordenspel.Application;

namespace Woordenspel.Api.Auth;

/// <remarks>
/// Gebruikt bewust de echte klok: de geldigheid van een token wordt door de JWT-middleware ook
/// tegen de echte klok gecontroleerd.
/// </remarks>
public sealed class JwtTokenIssuer(IOptions<JwtOptions> options, SigningKeyProvider keys)
{
    private readonly JsonWebTokenHandler _handler = new();

    public AuthResponse Issue(UserDto user)
    {
        var jwt = options.Value;
        var expiresAt = DateTimeOffset.UtcNow + jwt.TokenLifetime;

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Expires = expiresAt.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Name] = user.Username,
                [AuthenticationSetup.GuestClaim] = user.IsGuest,
            },
            SigningCredentials = new SigningCredentials(keys.Key, SecurityAlgorithms.HmacSha256),
        });

        return new AuthResponse(user.Id, user.Username, token, expiresAt, user.IsGuest);
    }
}
