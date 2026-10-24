using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Woordenspel.Application;
using Woordenspel.Contracts;
using Woordenspel.Infrastructure;
using Woordenspel.Domain.Tiles;
using Woordenspel.Domain.Words;
using Woordenspel.TestSupport;

namespace Woordenspel.Api.Tests;

/// <summary>
/// De echte API met een eigen SQLite-database in een tijdelijke map, een kleine woordenlijst, een nep-klok
/// en een letterzak die niet schudt: wie het spel aanmaakt begint en krijgt AAAAAAA, de tegenstander BBCCDDD.
/// Gastnamen zijn wel willekeurig.
/// </summary>
public sealed class WoordenspelApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dataDirectory = Path.Combine(Path.GetTempPath(), $"woordenspel-tests-{Guid.NewGuid():N}");

    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly List<HubListener> _listeners = [];

    public FakeTimeProvider Clock { get; } = new();

    /// <summary>
    /// Registreert een nieuwe gebruiker met een unieke naam en geeft een client die als die gebruiker is ingelogd.
    /// De gebruiker is ook online (verbonden met de hub), net als in de echte frontend.
    /// </summary>
    public async Task<(HttpClient Client, AuthResponse User)> CreateUserClientAsync(bool online = true)
    {
        var client = CreateClient();
        var username = $"u{Guid.NewGuid():N}"[..13];
        var response = await client.PostAsJsonAsync("/api/account/register", new { username, password = "geheim123" });
        response.EnsureSuccessStatusCode();

        var user = (await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        if (online)
        {
            _listeners.Add(await ConnectAsync(user));
        }

        return (client, user);
    }

    /// <summary>Anna daagt Bram uit en Bram accepteert. Anna begint (de letterzak schudt niet).</summary>
    /// <returns>Het spel zoals Anna het ziet.</returns>
    public async Task<(HttpClient Anna, AuthResponse AnnaUser, HttpClient Bram, AuthResponse BramUser, GameDto Game)> StartGameAsync()
    {
        var (anna, annaUser) = await CreateUserClientAsync();
        var (bram, bramUser) = await CreateUserClientAsync();

        var seek = await ReadAsync<SeekDto>(await anna.PostAsJsonAsync("/api/seeks", new { opponent = bramUser.Username }));
        (await bram.PostAsync($"/api/seeks/{seek.Id}/join", null)).EnsureSuccessStatusCode();
        var game = (await anna.GetFromJsonAsync<List<GameSummaryDto>>("/api/games", JsonOptions))!.Single();
        return (anna, annaUser, bram, bramUser, (await anna.GetFromJsonAsync<GameDto>($"/api/games/{game.Id}", JsonOptions))!);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }

    /// <summary>Opent een realtime-verbinding als deze gebruiker en houdt bij wat er binnenkomt.</summary>
    public async Task<HubListener> ConnectAsync(AuthResponse user)
    {
        var listener = new HubListener();
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(Server.BaseAddress, GameHubContract.Path), options =>
            {
                options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(user.Token);
            })
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();
        connection.On<GameDto>(GameHubContract.GameUpdated, game => listener.Record(listener.Updates, game));
        connection.On<GameDto>(GameHubContract.GameStarted, game => listener.Record(listener.Started, game));
        connection.On(GameHubContract.SeeksChanged, () => Interlocked.Increment(ref listener.SeeksChangedCount));

        await connection.StartAsync();
        listener.Connection = connection;
        return listener;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Storage:DataDirectory", _dataDirectory);
        builder.UseSetting("TurnExpiry:Interval", "00:00:00.100");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IWordDictionary>();
            services.AddSingleton<IWordDictionary>(new FakeWordDictionary("AA", "AAP"));
            services.RemoveAll<IRandomizer>();
            services.AddSingleton<IRandomizer, PredictableRandomizer>();
            // Gastnamen wel echt willekeurig, anders krijgt elke gast in de tests dezelfde naam.
            services.AddScoped(_ => new GuestNameGenerator(new SystemRandomizer()));
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    protected override void Dispose(bool disposing)
    {
        foreach (var listener in _listeners)
        {
            listener.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_dataDirectory))
        {
            Directory.Delete(_dataDirectory, recursive: true);
        }
    }
}
