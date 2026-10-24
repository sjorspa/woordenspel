namespace Woordenspel.Domain.Games;

public sealed record GameRules
{
    /// <summary>Wordfeud-regels.</summary>
    public static GameRules Default { get; } = new();

    public int MinPlayers { get; init; } = 2;

    public int MaxPlayers { get; init; } = 2;

    public int RackSize { get; init; } = 7;

    /// <summary>Bonus voor het leggen van alle stenen van een vol plankje.</summary>
    public int BingoBonus { get; init; } = 40;

    /// <summary>Ruilen mag alleen als er minstens zoveel stenen in de zak zitten.</summary>
    public int MinTilesInBagToSwap { get; init; } = 7;

    /// <summary>
    /// Het spel eindigt als elke speler dit aantal keer achter elkaar heeft gepast of geruild.
    /// </summary>
    public int ScorelessTurnsPerPlayerToEnd { get; init; } = 2;

    /// <summary>
    /// Totale bedenktijd per speler, zoals een schaakklok: je klok loopt alleen als jij aan de beurt bent.
    /// Wie door zijn tijd heen is, verliest. Null betekent geen klok. Standaard een blitzpartij van 5 minuten.
    /// </summary>
    public TimeSpan? TimePerPlayer { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximale bedenktijd per beurt, bijvoorbeeld 72 uur voor correspondentiespellen. Wie te laat is, verliest.
    /// Null betekent geen limiet per beurt.
    /// </summary>
    public TimeSpan? TurnTimeLimit { get; init; }
}
