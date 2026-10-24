namespace Woordenspel.Domain.Boards;

public enum Direction
{
    Horizontal,
    Vertical,
}

public readonly record struct Position(int Row, int Column)
{
    public Position Next(Direction direction) =>
        direction == Direction.Horizontal ? this with { Column = Column + 1 } : this with { Row = Row + 1 };

    public Position Previous(Direction direction) =>
        direction == Direction.Horizontal ? this with { Column = Column - 1 } : this with { Row = Row - 1 };

    public IEnumerable<Position> Neighbours()
    {
        yield return Previous(Direction.Vertical);
        yield return Next(Direction.Vertical);
        yield return Previous(Direction.Horizontal);
        yield return Next(Direction.Horizontal);
    }

    public override string ToString() => $"({Row},{Column})";
}
