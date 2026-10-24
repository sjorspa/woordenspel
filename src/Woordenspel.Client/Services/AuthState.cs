using System.Text.Json;
using Woordenspel.Contracts;

namespace Woordenspel.Client.Services;

/// <summary>Wie er is ingelogd. Het token wordt in de browser bewaard, zodat je ingelogd blijft.</summary>
public sealed class AuthState(IBrowserStorage storage, TimeProvider timeProvider)
{
    private const string StorageKey = "woordenspel.auth";

    private AuthResponse? _user;

    public event Action? Changed;

    public AuthResponse? User => _user is { } user && user.ExpiresAt > timeProvider.GetUtcNow() ? user : null;

    public bool IsLoggedIn => User is not null;

    public async Task InitializeAsync()
    {
        try
        {
            var json = await storage.GetAsync(StorageKey);
            _user = json is null ? null : JsonSerializer.Deserialize<AuthResponse>(json, ContractJson.Options);
        }
        catch (JsonException)
        {
            _user = null;
        }
    }

    public async Task SignInAsync(AuthResponse user)
    {
        _user = user;
        await storage.SetAsync(StorageKey, JsonSerializer.Serialize(user, ContractJson.Options));
        Changed?.Invoke();
    }

    public async Task SignOutAsync()
    {
        _user = null;
        await storage.RemoveAsync(StorageKey);
        Changed?.Invoke();
    }
}
