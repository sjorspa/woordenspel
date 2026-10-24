using Woordenspel.Domain.Boards;
using Woordenspel.Domain.Games;

namespace Woordenspel.Contracts;

/// <param name="ServerTime">
/// Het moment waarop deze stand is gemaakt. De resterende tijd van de spelers geldt voor dat moment; een client
/// telt vanaf daar zelf af.
/// </param>
/// <param name="Rack">Het plankje van de speler die het spel bekijkt.</param>
public sealed record GameDto(
    Guid Id,
    GameStatus Status,
    DateTimeOffset ServerTime,
    Guid? CurrentPlayerId,
    DateTimeOffset? TurnDeadline,
    int TilesInBag,
    IReadOnlyList<PlayerDto> Players,
    BoardDto Board,
    IReadOnlyList<MoveDto> Moves,
    IReadOnlyList<Guid> WinnerIds,
    IReadOnlyList<RackTileDto> Rack);

/// <param name="IsGuest">Een gast heeft geen wachtwoord en kan niet opnieuw inloggen.</param>
/// <summary>Een uitnodiging die wacht op een tegenstander.</summary>
/// <param name="OpponentId">De uitgedaagde speler; null bij een open spel.</param>
/// <param name="IsMine">Of de bekijker deze uitnodiging zelf heeft gemaakt.</param>
public sealed record SeekDto(
    Guid Id,
    Guid CreatorId,
    string CreatorName,
    Guid? OpponentId,
    string? OpponentName,
    bool IsMine,
    long? TimePerPlayerMs,
    DateTimeOffset CreatedAt);

public sealed record UserDto(Guid Id, string Username, bool IsGuest = false);

public sealed record GameSummaryDto(
    Guid Id,
    GameStatus Status,
    DateTimeOffset ServerTime,
    Guid? CurrentPlayerId,
    bool IsYourTurn,
    DateTimeOffset? TurnDeadline,
    IReadOnlyList<PlayerDto> Players,
    IReadOnlyList<Guid> WinnerIds);

/// <param name="TimeRemainingMs">Bedenktijd die de speler nog heeft op <c>ServerTime</c>; null zonder klok.</param>
public sealed record PlayerDto(Guid Id, string Name, int Score, int TileCount, bool HasResigned, long? TimeRemainingMs);

/// <param name="Rows">
/// Het bord als tekst: '.' is leeg, een hoofdletter is een gewone steen, een kleine letter een blanco steen.
/// </param>
public sealed record BoardDto(int Size, IReadOnlyList<string> Rows, IReadOnlyList<BoardTileDto> Tiles);

public sealed record BoardTileDto(int Row, int Column, char Letter, bool IsBlank, int Points);

/// <param name="Letter">De letter, of '?' voor een blanco steen.</param>
public sealed record RackTileDto(char Letter, bool IsBlank, int Points);

/// <param name="Positions">Waar de stenen zijn gelegd; leeg bij andere zetten.</param>
public sealed record MoveDto(
    Guid PlayerId, MoveType Type, int Score, IReadOnlyList<string> Words, int TileCount, IReadOnlyList<PositionDto> Positions);

public sealed record PositionDto(int Row, int Column);

public sealed record MoveResultDto(MoveDto Move, GameDto Game);

public sealed record PreviewDto(int Score, bool IsBingo, IReadOnlyList<WordDto> Words);

public sealed record WordDto(string Word, int Score);

public sealed record WordCheckDto(string Word, bool IsValid);

/// <param name="Letter">De letter, of bij een blanco steen de letter die hij voorstelt.</param>
public sealed record TilePlacementDto(int Row, int Column, char Letter, bool IsBlank = false);

public sealed record SetupDto(
    int BoardSize,
    int CenterRow,
    int CenterColumn,
    IReadOnlyList<BonusSquareDto> BonusSquares,
    IReadOnlyList<LetterDto> Letters,
    int BlankCount,
    GameRules Rules);

public sealed record BonusSquareDto(int Row, int Column, SquareBonus Bonus);

public sealed record LetterDto(char Letter, int Count, int Points);
