using Woordenspel.Domain.Tiles;

namespace Woordenspel.Domain.Games;

/// <summary>De volledige toestand van een spel, bedoeld om op te slaan en later te herstellen.</summary>
public sealed record GameSnapshot(
    Guid Id,
    GameRules Rules,
    IReadOnlyList<string> LayoutRows,
    GameStatus Status,
    IReadOnlyList<PlayerSnapshot> Players,
    int CurrentPlayerIndex,
    int ConsecutiveScorelessTurns,
    DateTimeOffset TurnStartedAt,
    IReadOnlyList<BoardTileSnapshot> Board,
    IReadOnlyList<TileSnapshot> Bag,
    IReadOnlyList<MoveRecord> Moves);

public sealed record PlayerSnapshot(
    Guid Id, string Name, int Score, bool HasResigned, IReadOnlyList<TileSnapshot> Rack, TimeSpan? TimeRemaining = null);

public sealed record BoardTileSnapshot(int Row, int Column, TileSnapshot Tile);

public sealed record TileSnapshot(char Letter, int Points, bool IsBlank)
{
    public static TileSnapshot From(Tile tile) => new(tile.Letter, tile.Points, tile.IsBlank);

    public Tile ToTile() => IsBlank
        ? Letter == Tile.BlankSymbol ? Tile.Blank() : Tile.Blank().WithBlankLetter(Letter)
        : Tile.ForLetter(Letter, Points);
}
