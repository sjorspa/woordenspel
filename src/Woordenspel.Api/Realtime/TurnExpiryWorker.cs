using Microsoft.Extensions.Options;
using Woordenspel.Application;

namespace Woordenspel.Api.Realtime;

public sealed class TurnExpiryOptions
{
    public const string SectionName = "TurnExpiry";

    /// <summary>Hoe vaak er wordt gekeken of iemand door zijn tijd heen is.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(1);
}

/// <summary>
/// Beëindigt op de achtergrond spellen waarin de speler aan de beurt door zijn tijd heen is. Zonder deze
/// taak zou dat pas gebeuren als iemand het spel opvraagt, en zou de tegenstander blijven wachten.
/// </summary>
public sealed class TurnExpiryWorker(
    IServiceScopeFactory scopes, IOptions<TurnExpiryOptions> options, ILogger<TurnExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var expired = await scope.ServiceProvider.GetRequiredService<GameService>().ExpireOverdueGamesAsync(stoppingToken);
                if (expired > 0)
                {
                    logger.LogInformation("{Count} spel(len) beëindigd omdat de tijd op was.", expired);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Controleren op verlopen beurten is mislukt.");
            }
        }
    }
}
