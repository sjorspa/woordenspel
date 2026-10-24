using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Woordenspel.Contracts;

namespace Woordenspel.Api.Auth;

public static class AuthenticationSetup
{
    public const string GuestClaim = "guest";

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<SigningKeyProvider>();
        services.AddSingleton<JwtTokenIssuer>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<SigningKeyProvider, IOptions<JwtOptions>>((bearer, keys, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = keys.Key,
                    NameClaimType = JwtRegisteredClaimNames.Name,
                };

                // WebSockets kunnen geen Authorization-header meesturen; SignalR zet het token dan in de query.
                bearer.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.HttpContext.Request.Path.StartsWithSegments(GameHubContract.Path)
                            && context.Request.Query["access_token"] is { Count: > 0 } token)
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization();
        return services;
    }

    public static bool IsGuest(this ClaimsPrincipal user) =>
        string.Equals(user.FindFirstValue(GuestClaim), "true", StringComparison.OrdinalIgnoreCase);

    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : throw new InvalidOperationException("Het token bevat geen gebruikers-id.");
}
