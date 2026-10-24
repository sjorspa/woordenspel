using Woordenspel.Domain.Boards;
using Woordenspel.Domain.Games;
using Woordenspel.Domain.Moves;

namespace Woordenspel.Application;

internal static class GameMapper
{
    public static GameDto ToDto(Game game, Guid viewerId)
    {
        var viewer = game.GetPlayer(viewerId);

        return new GameDto(
            game.Id,
            game.Status,
            game.Status == GameStatus.InProgress ? game.CurrentPlayer.Id : null,
            game.TurnDeadline,
            game.TilesInBag,
            ToDto(game.Players),
            ToDto(game.Board),
            game.Moves.Select(ToDto).ToList(),
            game.Winners.Select(p => p.Id).ToList(),
            viewer.Rack.Select(t => new RackTileDto(t.RackSymbol, t.IsBlank, t.Points)).ToList());
    }

    public static GameSummaryDto ToSummary(Game game, Guid viewerId) =>
        new(
            game.Id,
            game.Status,
            game.Status == GameStatus.InProgress && game.CurrentPlayer.Id == viewerId,
            game.TurnDeadline,
            ToDto(game.Players),
            game.Winners.Select(p => p.Id).ToList());

    private static List<PlayerDto> ToDto(IEnumerable<Player> players) =>
        players.Select(p => new PlayerDto(p.Id, p.Name, p.Score, p.Rack.Count, p.HasResigned)).ToList();

    public static MoveDto ToDto(MoveRecord move) =>
        new(move.PlayerId, move.Type, move.Score, move.Words, move.TileCount);

    public static PreviewDto ToDto(PlacementResult result) =>
        new(result.Score, result.IsBingo, result.Words.Select(w => new WordDto(w.Text, w.Score)).ToList());

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
