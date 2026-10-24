namespace Woordenspel.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "woordenspel";

    public string Audience { get; set; } = "woordenspel";

    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Optioneel; minstens 32 tekens. Zonder sleutel wordt er één gegenereerd en in de datamap bewaard.
    /// </summary>
    public string? SigningKey { get; set; }
}
