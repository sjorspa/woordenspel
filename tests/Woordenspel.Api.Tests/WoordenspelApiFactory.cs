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

    public FakeTimeProvider Clock { get; } = new();

    /// <summary>Registreert een nieuwe gebruiker met een unieke naam en geeft een client die als die gebruiker is ingelogd.</summary>
    public async Task<(HttpClient Client, AuthResponse User)> CreateUserClientAsync()
    {
        var client = CreateClient();
        var username = $"u{Guid.NewGuid():N}"[..13];
        var response = await client.PostAsJsonAsync("/api/account/register", new { username, password = "geheim123" });
        response.EnsureSuccessStatusCode();

        var user = (await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        return (client, user);
    }

    /// <summary>Opent een realtime-verbinding als deze gebruiker; ontvangen spelupdates komen in de lijst.</summary>
    public async Task<(HubConnection Connection, List<GameDto> Updates)> ConnectAsync(AuthResponse user)
    {
        var updates = new List<GameDto>();
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(Server.BaseAddress, GameHubContract.Path), options =>
            {
                options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(user.Token);
            })
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();
        connection.On<GameDto>(GameHubContract.GameUpdated, game =>
        {
            lock (updates)
            {
                updates.Add(game);
            }
        });

        await connection.StartAsync();
        return (connection, updates);
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
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_dataDirectory))
        {
            Directory.Delete(_dataDirectory, recursive: true);
        }
    }
}
