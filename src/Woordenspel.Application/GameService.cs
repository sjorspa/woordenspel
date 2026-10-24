using Woordenspel.Application.Abstractions;
using Woordenspel.Domain.Boards;
using Woordenspel.Domain.Games;
using Woordenspel.Domain.Moves;
using Woordenspel.Domain.Tiles;
using Woordenspel.Domain.Words;

namespace Woordenspel.Application;

/// <summary>De use cases van het spel. Laadt het aggregate, laat het domein het werk doen en slaat op.</summary>
public sealed class GameService(
    IGameRepository repository,
    IUserRepository users,
    IWordDictionary dictionary,
    IRandomizer randomizer,
    TimeProvider timeProvider,
    GameSetup setup)
{
    /// <summary>Start een spel tussen de ingelogde gebruiker en een tegenstander.</summary>
    public async Task<GameDto> CreateGameAsync(Guid creatorId, string opponentUsername, CancellationToken cancellationToken = default)
    {
        var opponent = await users.FindByUsernameAsync(opponentUsername ?? string.Empty, cancellationToken)
            ?? throw new UserNotFoundException(opponentUsername ?? string.Empty);
        var creator = await users.FindByIdAsync(creatorId, cancellationToken)
            ?? throw new InvalidOperationException($"Ingelogde gebruiker {creatorId} bestaat niet.");

        var bag = new TileBag(setup.LetterSet.CreateTiles(), randomizer);
        var game = Game.Start(
            Guid.NewGuid(),
            [new PlayerIdentity(creator.Id, creator.Username), new PlayerIdentity(opponent.Id, opponent.Username)],
            setup.Rules,
            setup.Layout,
            bag,
            randomizer,
            timeProvider.GetUtcNow());

        await repository.SaveAsync(game, cancellationToken);
        return GameMapper.ToDto(game, creatorId);
    }

    /// <summary>De spelstand zoals deze speler hem ziet, inclusief het eigen plankje.</summary>
    public async Task<GameDto> GetGameAsync(Guid gameId, Guid viewerId, CancellationToken cancellationToken = default)
    {
        var game = await LoadAsync(gameId, cancellationToken);
        return GameMapper.ToDto(game, viewerId);
    }

    /// <summary>Alle spellen van deze speler: lopende spellen waarin hij aan de beurt is eerst.</summary>
    public async Task<IReadOnlyList<GameSummaryDto>> ListGamesAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var games = await repository.ListForPlayerAsync(playerId, cancellationToken);
        var now = timeProvider.GetUtcNow();

        foreach (var game in games)
        {
            if (game.ExpireTurnIfOverdue(now))
            {
                await repository.SaveAsync(game, cancellationToken);
            }
        }

        return games
            .Select(g => GameMapper.ToSummary(g, playerId))
            .OrderBy(g => g.Status)
            .ThenByDescending(g => g.IsYourTurn)
            .ThenBy(g => g.TurnDeadline)
            .ToList();
    }

    public async Task<PreviewDto> PreviewPlacementAsync(
        Guid gameId, Guid playerId, IReadOnlyList<TilePlacementDto> tiles, CancellationToken cancellationToken = default)
    {
        var game = await LoadAsync(gameId, cancellationToken);
        return GameMapper.ToDto(game.PreviewPlacement(playerId, ToPlacements(tiles), dictionary));
    }

    public Task<MoveResultDto> PlaceTilesAsync(
        Guid gameId, Guid playerId, IReadOnlyList<TilePlacementDto> tiles, CancellationToken cancellationToken = default) =>
        ExecuteMoveAsync(gameId, playerId, (game, now) => game.PlaceTiles(playerId, ToPlacements(tiles), dictionary, now), cancellationToken);

    public Task<MoveResultDto> SwapTilesAsync(
        Guid gameId, Guid playerId, IReadOnlyList<char> letters, CancellationToken cancellationToken = default) =>
        ExecuteMoveAsync(gameId, playerId, (game, now) => game.SwapTiles(playerId, letters, now), cancellationToken);

    public Task<MoveResultDto> PassAsync(Guid gameId, Guid playerId, CancellationToken cancellationToken = default) =>
        ExecuteMoveAsync(gameId, playerId, (game, now) => game.Pass(playerId, now), cancellationToken);

    public Task<MoveResultDto> ResignAsync(Guid gameId, Guid playerId, CancellationToken cancellationToken = default) =>
        ExecuteMoveAsync(gameId, playerId, (game, now) => game.Resign(playerId, now), cancellationToken);

    public WordCheckDto CheckWord(string word)
    {
        var normalized = word.Trim().ToUpperInvariant();
        return new WordCheckDto(normalized, normalized.Length >= 2 && dictionary.Contains(normalized));
    }

    public SetupDto GetSetup()
    {
        var layout = setup.Layout;
        var bonusSquares =
            from row in Enumerable.Range(0, layout.Size)
            from column in Enumerable.Range(0, layout.Size)
            let bonus = layout.GetBonus(new Position(row, column))
            where bonus != SquareBonus.None
            select new BonusSquareDto(row, column, bonus);

        return new SetupDto(
            layout.Size,
            layout.Center.Row,
            layout.Center.Column,
            bonusSquares.ToList(),
            setup.LetterSet.Letters.OrderBy(l => l.Letter).Select(l => new LetterDto(l.Letter, l.Count, l.Points)).ToList(),
            setup.LetterSet.BlankCount,
            setup.Rules);
    }

    private async Task<MoveResultDto> ExecuteMoveAsync(
        Guid gameId, Guid playerId, Func<Game, DateTimeOffset, MoveRecord> move, CancellationToken cancellationToken)
    {
        var game = await LoadAsync(gameId, cancellationToken);
        var record = move(game, timeProvider.GetUtcNow());
        await repository.SaveAsync(game, cancellationToken);
        return new MoveResultDto(GameMapper.ToDto(record), GameMapper.ToDto(game, playerId));
    }

    /// <summary>
    /// Laadt het spel en verwerkt een verlopen bedenktijd meteen, zodat ook wie alleen kijkt de juiste stand ziet.
    /// </summary>
    private async Task<Game> LoadAsync(Guid gameId, CancellationToken cancellationToken)
    {
        var game = await repository.FindAsync(gameId, cancellationToken) ?? throw new GameNotFoundException(gameId);
        if (game.ExpireTurnIfOverdue(timeProvider.GetUtcNow()))
        {
            await repository.SaveAsync(game, cancellationToken);
        }

        return game;
    }

    private static List<TilePlacement> ToPlacements(IReadOnlyList<TilePlacementDto> tiles) =>
        tiles.Select(t => new TilePlacement(new Position(t.Row, t.Column), t.Letter, t.IsBlank)).ToList();
}
