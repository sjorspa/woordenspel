using Woordenspel.Domain.Tiles;

namespace Woordenspel.Domain.Boards;

public readonly record struct BoardTile(Position Position, Tile Tile);

public interface IReadOnlyBoard
{
    BoardLayout Layout { get; }

    bool IsEmpty { get; }

    bool IsInside(Position position);

    /// <summary>De steen op deze positie, of null als het vakje leeg is of buiten het bord ligt.</summary>
    Tile? GetTile(Position position);

    IEnumerable<BoardTile> GetTiles();
}

public sealed class Board(BoardLayout layout) : IReadOnlyBoard
{
    private readonly Tile?[,] _squares = new Tile?[layout.Size, layout.Size];
    private int _tileCount;

    public BoardLayout Layout { get; } = layout;

    public bool IsEmpty => _tileCount == 0;

    public bool IsInside(Position position) => Layout.IsInside(position);

    public Tile? GetTile(Position position) => IsInside(position) ? _squares[position.Row, position.Column] : null;

    public IEnumerable<BoardTile> GetTiles()
    {
        for (var row = 0; row < Layout.Size; row++)
        {
            for (var column = 0; column < Layout.Size; column++)
            {
                if (_squares[row, column] is { } tile)
                {
                    yield return new BoardTile(new Position(row, column), tile);
                }
            }
        }
    }

    public void Place(Position position, Tile tile)
    {
        if (!IsInside(position))
        {
            throw new DomainException(DomainErrors.OutsideBoard, $"Positie {position} ligt buiten het bord.");
        }

        if (tile.IsUnassignedBlank)
        {
            throw new DomainException(DomainErrors.BlankLetterRequired, "Een blanco steen moet een letter krijgen.");
        }

        if (_squares[position.Row, position.Column] is not null)
        {
            throw new DomainException(DomainErrors.SquareOccupied, $"Vakje {position} is al bezet.");
        }

        _squares[position.Row, position.Column] = tile;
        _tileCount++;
    }
}
