using Woordenspel.Application.Abstractions;
using Woordenspel.Contracts;
using Woordenspel.Domain.Boards;
using Woordenspel.Domain.Games;
using Woordenspel.Domain.Moves;
using Woordenspel.Domain.Tiles;
using Woordenspel.Domain.Words;

namespace Woordenspel.Application;

/// <summary>
/// De use cases van het spel. Laadt het aggregate, laat het domein het werk doen, slaat op en brengt
/// de spelers op de hoogte.
/// </summary>
public sealed class GameService(
    IGameRepository repository,
    IWordDictionary dictionary,
    IRandomizer randomizer,
    IGameNotifier notifier,
    TimeProvider timeProvider,
    GameSetup setup)
{
    /// <summary>Start een spel tussen deze spelers en laat ze dat weten, zodat ze het openen.</summary>
    /// <returns>Het spel zoals <paramref name="viewerId"/> het ziet.</returns>
    public async Task<GameDto> StartGameAsync(
        IReadOnlyList<PlayerIdentity> players, Guid viewerId, CancellationToken cancellationToken = default)
    {
        var bag = new TileBag(setup.LetterSet.CreateTiles(), randomizer);
        var game = Game.Start(Guid.NewGuid(), players, setup.Rules, setup.Layout, bag, randomizer, timeProvider.GetUtcNow());
        await repository.SaveAsync(game, cancellationToken);

        var now = timeProvider.GetUtcNow();
        foreach (var player in game.Players)
        {
            await notifier.GameStartedAsync(player.Id, GameMapper.ToDto(game, player.Id, now), cancellationToken);
        }

        return GameMapper.ToDto(game, viewerId, now);
    }

    /// <summary>De spelstand zoals deze speler hem ziet, inclusief het eigen plankje.</summary>
    public async Task<GameDto> GetGameAsync(Guid gameId, Guid viewerId, CancellationToken cancellationToken = default)
    {
        var game = await LoadAsync(gameId, cancellationToken);
        return GameMapper.ToDto(game, viewerId, timeProvider.GetUtcNow());
    }

    /// <summary>Alle spellen van deze speler: lopende spellen waarin hij aan de beurt is eerst.</summary>
    public async Task<IReadOnlyList<GameSummaryDto>> ListGamesAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var games = await repository.ListForPlayerAsync(playerId, cancellationToken);

        foreach (var game in games)
        {
            try
            {
                await ExpireIfOverdueAsync(game, cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                // Tegelijk door de achtergrondtaak beëindigd; de lokale kopie toont al dezelfde uitkomst.
            }
        }

        var now = timeProvider.GetUtcNow();
        return games
            .Select(g => GameMapper.ToSummary(g, playerId, now))
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

    /// <summary>
    /// Beëindigt alle spellen waarin de speler aan de beurt door zijn tijd heen is, en laat dat de spelers weten.
    /// Bedoeld om regelmatig op de achtergrond te draaien, zodat een tegenstander het ziet zonder dat iemand iets doet.
    /// </summary>
    /// <returns>Het aantal beëindigde spellen.</returns>
    public async Task<int> ExpireOverdueGamesAsync(CancellationToken cancellationToken = default)
    {
        var expired = 0;
        foreach (var game in await repository.ListOverdueAsync(timeProvider.GetUtcNow(), cancellationToken))
        {
            try
            {
                if (await ExpireIfOverdueAsync(game, cancellationToken))
                {
                    expired++;
                }
            }
            catch (ConcurrencyConflictException)
            {
                // Er werd tegelijk een zet gedaan; de volgende ronde kijken we opnieuw.
            }
        }

        return expired;
    }

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
        await SaveAndNotifyAsync(game, cancellationToken);
        return new MoveResultDto(GameMapper.ToDto(record), GameMapper.ToDto(game, playerId, timeProvider.GetUtcNow()));
    }

    /// <summary>
    /// Laadt het spel en verwerkt een verlopen bedenktijd meteen, zodat ook wie alleen kijkt de juiste stand ziet.
    /// </summary>
    private async Task<Game> LoadAsync(Guid gameId, CancellationToken cancellationToken)
    {
        var game = await repository.FindAsync(gameId, cancellationToken) ?? throw new GameNotFoundException(gameId);
        try
        {
            await ExpireIfOverdueAsync(game, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // Iemand anders (meestal de achtergrondtaak) was ons net voor; gebruik zijn versie.
            game = await repository.FindAsync(gameId, cancellationToken) ?? throw new GameNotFoundException(gameId);
        }

        return game;
    }

    private async Task<bool> ExpireIfOverdueAsync(Game game, CancellationToken cancellationToken)
    {
        if (!game.ExpireTurnIfOverdue(timeProvider.GetUtcNow()))
        {
            return false;
        }

        await SaveAndNotifyAsync(game, cancellationToken);
        return true;
    }

    private async Task SaveAndNotifyAsync(Game game, CancellationToken cancellationToken)
    {
        await repository.SaveAsync(game, cancellationToken);

        var now = timeProvider.GetUtcNow();
        foreach (var player in game.Players)
        {
            await notifier.GameUpdatedAsync(player.Id, GameMapper.ToDto(game, player.Id, now), cancellationToken);
        }
    }

    private static List<TilePlacement> ToPlacements(IReadOnlyList<TilePlacementDto> tiles) =>
        tiles.Select(t => new TilePlacement(new Position(t.Row, t.Column), t.Letter, t.IsBlank)).ToList();
}
