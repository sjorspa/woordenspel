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
    /// Bedenktijd per beurt. Wie niet op tijd speelt, verliest het spel. Null betekent geen limiet.
    /// </summary>
    public TimeSpan? TurnTimeLimit { get; init; } = TimeSpan.FromHours(72);
}
