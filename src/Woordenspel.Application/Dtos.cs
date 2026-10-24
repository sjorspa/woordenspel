using Woordenspel.Domain.Boards;
using Woordenspel.Domain.Games;

namespace Woordenspel.Application;

/// <param name="Rack">Het plankje van de speler die het spel bekijkt.</param>
public sealed record GameDto(
    Guid Id,
    GameStatus Status,
    Guid? CurrentPlayerId,
    DateTimeOffset? TurnDeadline,
    int TilesInBag,
    IReadOnlyList<PlayerDto> Players,
    BoardDto Board,
    IReadOnlyList<MoveDto> Moves,
    IReadOnlyList<Guid> WinnerIds,
    IReadOnlyList<RackTileDto> Rack);

public sealed record UserDto(Guid Id, string Username);

public sealed record GameSummaryDto(
    Guid Id,
    GameStatus Status,
    bool IsYourTurn,
    DateTimeOffset? TurnDeadline,
    IReadOnlyList<PlayerDto> Players,
    IReadOnlyList<Guid> WinnerIds);

public sealed record PlayerDto(Guid Id, string Name, int Score, int TileCount, bool HasResigned);

/// <param name="Rows">
/// Het bord als tekst: '.' is leeg, een hoofdletter is een gewone steen, een kleine letter een blanco steen.
/// </param>
public sealed record BoardDto(int Size, IReadOnlyList<string> Rows, IReadOnlyList<BoardTileDto> Tiles);

public sealed record BoardTileDto(int Row, int Column, char Letter, bool IsBlank, int Points);

/// <param name="Letter">De letter, of '?' voor een blanco steen.</param>
public sealed record RackTileDto(char Letter, bool IsBlank, int Points);

public sealed record MoveDto(Guid PlayerId, MoveType Type, int Score, IReadOnlyList<string> Words, int TileCount);

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
