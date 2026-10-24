using System.Net;
using System.Text;
using System.Text.Json;
using Woordenspel.Client.Services;
using Woordenspel.Contracts;

namespace Woordenspel.Client.Tests;

internal sealed class FakeBrowserStorage : IBrowserStorage
{
    public Dictionary<string, string> Items { get; } = [];

    public ValueTask<string?> GetAsync(string key) => ValueTask.FromResult(Items.GetValueOrDefault(key));

    public ValueTask SetAsync(string key, string value)
    {
        Items[key] = value;
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(string key)
    {
        Items.Remove(key);
        return ValueTask.CompletedTask;
    }
}

/// <summary>Een nep-server: per "METHODE pad" een antwoord, en een lijst van alle ontvangen verzoeken.</summary>
internal sealed class FakeApi : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _routes = [];

    public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = [];

    public FakeApi On(string route, object response, HttpStatusCode status = HttpStatusCode.OK)
    {
        _routes[route] = _ => Json(response, status);
        return this;
    }

    public FakeApi OnProblem(string route, HttpStatusCode status, string code, string title, params string[] invalidWords)
    {
        _routes[route] = _ => Json(new { status = (int)status, title, code, invalidWords }, status, "application/problem+json");
        return this;
    }

    public IEnumerable<string?> BodiesFor(string route) =>
        Requests.Where(r => Route(r.Request) == route).Select(r => r.Body);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request, body));
        return _routes.TryGetValue(Route(request), out var respond)
            ? respond(request)
            : new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static string Route(HttpRequestMessage request) => $"{request.Method} {request.RequestUri!.AbsolutePath}";

    private static HttpResponseMessage Json(object value, HttpStatusCode status, string mediaType = "application/json") =>
        new(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(value, ContractJson.Options), Encoding.UTF8, mediaType),
        };
}

internal sealed class FakeGameUpdates : IGameUpdates
{
    public event Func<GameDto, Task>? GameUpdated;

    public event Func<Task>? Reconnected;

    public int ConnectCount { get; private set; }

    public Task EnsureConnectedAsync()
    {
        ConnectCount++;
        return Task.CompletedTask;
    }

    public Task PushAsync(GameDto game) => GameUpdated?.Invoke(game) ?? Task.CompletedTask;

    public Task ReconnectAsync() => Reconnected?.Invoke() ?? Task.CompletedTask;
}
