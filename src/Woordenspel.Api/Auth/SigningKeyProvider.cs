using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Woordenspel.Infrastructure;

namespace Woordenspel.Api.Auth;

/// <summary>
/// Levert de sleutel waarmee tokens worden ondertekend. Staat er geen in de configuratie, dan wordt er
/// eenmalig een willekeurige sleutel gemaakt en in de datamap bewaard, zodat tokens een herstart overleven.
/// </summary>
public sealed class SigningKeyProvider(IOptions<JwtOptions> jwtOptions, IOptions<StorageOptions> storageOptions)
{
    private const string KeyFileName = "jwt-signing.key";
    private const int MinimumKeyLength = 32;

    private readonly Lazy<SymmetricSecurityKey> _key = new(() => LoadKey(jwtOptions.Value, storageOptions.Value));

    public SymmetricSecurityKey Key => _key.Value;

    private static SymmetricSecurityKey LoadKey(JwtOptions jwt, StorageOptions storage)
    {
        if (!string.IsNullOrEmpty(jwt.SigningKey))
        {
            var configured = Encoding.UTF8.GetBytes(jwt.SigningKey);
            if (configured.Length < MinimumKeyLength)
            {
                throw new InvalidOperationException($"Jwt:SigningKey moet minstens {MinimumKeyLength} tekens hebben.");
            }

            return new SymmetricSecurityKey(configured);
        }

        var directory = storage.ResolveDataDirectory();
        var path = Path.Combine(directory, KeyFileName);
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
        }

        return new SymmetricSecurityKey(Convert.FromBase64String(File.ReadAllText(path)));
    }
}
