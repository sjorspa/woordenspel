using Woordenspel.Contracts;
using Woordenspel.Domain.Boards;
using Woordenspel.Domain.Games;
using Woordenspel.Domain.Moves;

namespace Woordenspel.Application;

internal static class GameMapper
{
    public static GameDto ToDto(Game game, Guid viewerId, DateTimeOffset now)
    {
        var viewer = game.GetPlayer(viewerId);

        return new GameDto(
            game.Id,
            game.Status,
            now,
            CurrentPlayerId(game),
            game.TurnDeadline,
            game.TilesInBag,
            ToDto(game, now),
            ToDto(game.Board),
            game.Moves.Select(ToDto).ToList(),
            game.Winners.Select(p => p.Id).ToList(),
            viewer.Rack.Select(t => new RackTileDto(t.RackSymbol, t.IsBlank, t.Points)).ToList());
    }

    public static GameSummaryDto ToSummary(Game game, Guid viewerId, DateTimeOffset now) =>
        new(
            game.Id,
            game.Status,
            now,
            CurrentPlayerId(game),
            CurrentPlayerId(game) == viewerId,
            game.TurnDeadline,
            ToDto(game, now),
            game.Winners.Select(p => p.Id).ToList());

    public static MoveDto ToDto(MoveRecord move) =>
        new(
            move.PlayerId,
            move.Type,
            move.Score,
            move.Words,
            move.TileCount,
            (move.Positions ?? []).Select(p => new PositionDto(p.Row, p.Column)).ToList());

    public static PreviewDto ToDto(PlacementResult result) =>
        new(result.Score, result.IsBingo, result.Words.Select(w => new WordDto(w.Text, w.Score)).ToList());

    private static Guid? CurrentPlayerId(Game game) => game.Status == GameStatus.InProgress ? game.CurrentPlayer.Id : null;

    private static List<PlayerDto> ToDto(Game game, DateTimeOffset now) =>
        game.Players
            .Select(p => new PlayerDto(
                p.Id,
                p.Name,
                p.Score,
                p.Rack.Count,
                p.HasResigned,
                game.GetTimeRemaining(p, now) is { } remaining ? (long)remaining.TotalMilliseconds : null))
            .ToList();

    private static BoardDto ToDto(IReadOnlyBoard board)
    {
        var size = board.Layout.Size;
        var tiles = board.GetTiles().ToList();

        var grid = Enumerable.Range(0, size).Select(_ => Enumerable.Repeat('.', size).ToArray()).ToArray();
        foreach (var (position, tile) in tiles)
        {
            grid[position.Row][position.Column] = tile.IsBlank ? char.ToLowerInvariant(tile.Letter) : tile.Letter;
        }

        return new BoardDto(
            size,
            grid.Select(row => new string(row)).ToList(),
            tiles.Select(t => new BoardTileDto(t.Position.Row, t.Position.Column, t.Tile.Letter, t.Tile.IsBlank, t.Tile.Points)).ToList());
    }
}
