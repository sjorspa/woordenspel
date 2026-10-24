using Woordenspel.Domain.Boards;

namespace Woordenspel.Domain.Moves;

public sealed record FormedWord(string Text, Position Start, Direction Direction, int Score);

public sealed record PlacementResult(IReadOnlyList<FormedWord> Words, int BingoBonus)
{
    public int Score => Words.Sum(w => w.Score) + BingoBonus;

    public bool IsBingo => BingoBonus > 0;
}
