using Woordenspel.Contracts;
using Woordenspel.Domain.Games;

namespace Woordenspel.Client.Game;

/// <summary>Teksten over een spel, zoals ze in de interface staan.</summary>
public static class GameText
{
    public static string Describe(MoveDto move, string playerName) => move.Type switch
    {
        MoveType.Placement => $"{playerName}: {string.Join(", ", move.Words)} ({move.Score})",
        MoveType.Swap => $"{playerName} ruilde {move.TileCount} {(move.TileCount == 1 ? "steen" : "stenen")}",
        MoveType.Pass => $"{playerName} paste",
        MoveType.Resign => $"{playerName} gaf op",
        MoveType.Timeout => $"{playerName} was door de tijd heen",
        _ => playerName,
    };

    public static string Result(IReadOnlyList<PlayerDto> players, IReadOnlyList<Guid> winnerIds, Guid viewerId) =>
        winnerIds.Count switch
        {
            0 => "Het spel is afgelopen.",
            > 1 => "Gelijkspel!",
            _ when winnerIds[0] == viewerId => "Je hebt gewonnen!",
            _ => $"{players.First(p => p.Id == winnerIds[0]).Name} heeft gewonnen.",
        };

    public static string Preview(PreviewDto preview)
    {
        var words = string.Join(" + ", preview.Words.Select(w => $"{w.Word} ({w.Score})"));
        return preview.IsBingo ? $"{words} + bingo = {preview.Score} punten" : $"{words} = {preview.Score} punten";
    }
}
