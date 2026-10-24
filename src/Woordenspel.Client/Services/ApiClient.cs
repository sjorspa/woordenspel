using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Woordenspel.Contracts;

namespace Woordenspel.Client.Services;

/// <summary>Een fout die de API teruggaf, met de stabiele <see cref="Code"/> uit de ProblemDetails.</summary>
public sealed class ApiException(HttpStatusCode status, string? code, string message, IReadOnlyList<string> invalidWords)
    : Exception(message)
{
    public HttpStatusCode Status { get; } = status;

    public string? Code { get; } = code;

    public IReadOnlyList<string> InvalidWords { get; } = invalidWords;

    public static async Task<ApiException> FromResponseAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            var title = problem.TryGetProperty("title", out var t) ? t.GetString() : null;
            var code = problem.TryGetProperty("code", out var c) ? c.GetString() : null;
            var words = problem.TryGetProperty("invalidWords", out var w)
                ? w.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList()
                : [];
            return new ApiException(response.StatusCode, code, title ?? DefaultMessage(response.StatusCode), words);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return new ApiException(response.StatusCode, null, DefaultMessage(response.StatusCode), []);
        }
    }

    private static string DefaultMessage(HttpStatusCode status) =>
        status == HttpStatusCode.Unauthorized ? "Je bent niet (meer) ingelogd." : $"Er ging iets mis ({(int)status}).";
}

public sealed class ApiClient(HttpClient http, AuthState auth)
{
    public Task<AuthResponse> RegisterAsync(string username, string password) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/account/register", new RegisterRequest(username, password), authorize: false);

    public Task<AuthResponse> LoginAsync(string username, string password) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/account/login", new LoginRequest(username, password), authorize: false);

    public Task<AuthResponse> PlayAsGuestAsync() =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/account/guest", authorize: false);

    public Task<SetupDto> GetSetupAsync() => SendAsync<SetupDto>(HttpMethod.Get, "api/setup", authorize: false);

    public Task<IReadOnlyList<GameSummaryDto>> ListGamesAsync() =>
        SendAsync<IReadOnlyList<GameSummaryDto>>(HttpMethod.Get, "api/games");

    /// <param name="opponent">Wie je uitdaagt; null voor een open spel.</param>
    public Task<SeekDto> CreateSeekAsync(string? opponent) =>
        SendAsync<SeekDto>(HttpMethod.Post, "api/seeks", new CreateSeekRequest(opponent));

    public Task<IReadOnlyList<SeekDto>> ListSeeksAsync() => SendAsync<IReadOnlyList<SeekDto>>(HttpMethod.Get, "api/seeks");

    public Task<GameDto> JoinSeekAsync(Guid seekId) => SendAsync<GameDto>(HttpMethod.Post, $"api/seeks/{seekId}/join");

    public Task CancelSeekAsync(Guid seekId) => SendAsync(HttpMethod.Delete, $"api/seeks/{seekId}");

    public Task<GameDto> GetGameAsync(Guid gameId) => SendAsync<GameDto>(HttpMethod.Get, $"api/games/{gameId}");

    public Task<PreviewDto> PreviewAsync(Guid gameId, IReadOnlyList<TilePlacementDto> tiles, CancellationToken cancellationToken = default) =>
        SendAsync<PreviewDto>(HttpMethod.Post, $"api/games/{gameId}/preview", new PlaceTilesRequest(tiles), cancellationToken: cancellationToken);

    public Task<MoveResultDto> PlaceAsync(Guid gameId, IReadOnlyList<TilePlacementDto> tiles) =>
        SendAsync<MoveResultDto>(HttpMethod.Post, $"api/games/{gameId}/place", new PlaceTilesRequest(tiles));

    public Task<MoveResultDto> SwapAsync(Guid gameId, IReadOnlyList<char> letters) =>
        SendAsync<MoveResultDto>(HttpMethod.Post, $"api/games/{gameId}/swap", new SwapTilesRequest(letters));

    public Task<MoveResultDto> PassAsync(Guid gameId) => SendAsync<MoveResultDto>(HttpMethod.Post, $"api/games/{gameId}/pass");

    public Task<MoveResultDto> ResignAsync(Guid gameId) => SendAsync<MoveResultDto>(HttpMethod.Post, $"api/games/{gameId}/resign");

    private async Task<T> SendAsync<T>(
        HttpMethod method, string url, object? body = null, bool authorize = true, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(method, url, body, authorize, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<T>(ContractJson.Options, cancellationToken))!;
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string url, object? body = null, bool authorize = true, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: ContractJson.Options);
        }

        if (authorize && auth.User is { } user)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        }

        var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            using (response)
            {
                if (authorize && response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await auth.SignOutAsync();
                }

                throw await ApiException.FromResponseAsync(response);
            }
        }

        return response;
    }
}
