namespace Woordenspel.Contracts;

/// <summary>De realtime-verbinding (SignalR) tussen server en client.</summary>
public static class GameHubContract
{
    public const string Path = "/hubs/games";

    /// <summary>
    /// Server → client, met een <see cref="GameDto"/> zoals de ontvanger hem ziet. Wordt gestuurd na elke wijziging
    /// van een spel waarin de ontvanger meedoet: een nieuw spel, een zet, of een speler die door zijn tijd heen is.
    /// </summary>
    public const string GameUpdated = "GameUpdated";
}
