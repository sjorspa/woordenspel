using Woordenspel.Domain.Boards;
using Woordenspel.Domain.Moves;
using Woordenspel.Domain.Tiles;
using Woordenspel.Domain.Words;

namespace Woordenspel.Domain.Games;

public enum GameStatus
{
    InProgress,
    Finished,
}

/// <summary>
/// Aggregate root van één potje. Alle wijzigingen lopen via deze klasse en worden eerst volledig
/// gevalideerd, zodat een ongeldige zet de toestand nooit half verandert.
/// </summary>
/// <remarks>
/// Het domein leest zelf geen klok: acties krijgen het huidige tijdstip mee, zodat de bedenktijd testbaar is.
/// </remarks>
public sealed class Game
{
    private readonly List<Player> _players;
    private readonly List<MoveRecord> _moves = [];
    private readonly Board _board;
    private readonly TileBag _bag;
    private int _currentPlayerIndex;
    private int _consecutiveScorelessTurns;

    private Game(Guid id, List<Player> players, GameRules rules, Board board, TileBag bag, int startingPlayerIndex, DateTimeOffset now)
    {
        Id = id;
        _players = players;
        Rules = rules;
        _board = board;
        _bag = bag;
        _currentPlayerIndex = startingPlayerIndex;
        TurnStartedAt = now;
    }

    public Guid Id { get; }

    public GameRules Rules { get; }

    public GameStatus Status { get; private set; } = GameStatus.InProgress;

    public IReadOnlyList<Player> Players => _players;

    public Player CurrentPlayer => _players[_currentPlayerIndex];

    public DateTimeOffset TurnStartedAt { get; private set; }

    /// <summary>Uiterlijk moment voor de huidige zet; null als er geen limiet is of het spel voorbij is.</summary>
    public DateTimeOffset? TurnDeadline =>
        Status == GameStatus.InProgress && Rules.TurnTimeLimit is { } limit ? TurnStartedAt + limit : null;

    public IReadOnlyBoard Board => _board;

    public int TilesInBag => _bag.Count;

    public IReadOnlyList<MoveRecord> Moves => _moves;

    /// <summary>De speler(s) met de hoogste score die niet hebben opgegeven; leeg zolang het spel loopt.</summary>
    public IReadOnlyList<Player> Winners
    {
        get
        {
            if (Status != GameStatus.Finished)
            {
                return [];
            }

            var contenders = _players.Where(p => !p.HasResigned).ToList();
            if (contenders.Count == 0)
            {
                return [];
            }

            var best = contenders.Max(p => p.Score);
            return contenders.Where(p => p.Score == best).ToList();
        }
    }

    /// <param name="randomizer">Bepaalt wie er begint.</param>
    public static Game Start(
        Guid id,
        IReadOnlyList<PlayerIdentity> players,
        GameRules rules,
        BoardLayout layout,
        TileBag bag,
        IRandomizer randomizer,
        DateTimeOffset now)
    {
        if (players.Count < rules.MinPlayers || players.Count > rules.MaxPlayers)
        {
            var range = rules.MinPlayers == rules.MaxPlayers
                ? $"{rules.MinPlayers}"
                : $"{rules.MinPlayers} tot {rules.MaxPlayers}";
            throw new DomainException(DomainErrors.InvalidPlayerCount, $"Een spel heeft {range} spelers nodig.");
        }

        if (players.DistinctBy(p => p.Id).Count() != players.Count)
        {
            throw new DomainException(DomainErrors.DuplicatePlayer, "Je kunt niet tegen jezelf spelen.");
        }

        var gamePlayers = players.Select(p => new Player(p.Id, p.Name)).ToList();
        foreach (var player in gamePlayers)
        {
            player.AddTiles(bag.Draw(rules.RackSize));
        }

        var startingPlayerIndex = randomizer.Next(gamePlayers.Count);
        return new Game(id, gamePlayers, rules, new Board(layout), bag, startingPlayerIndex, now);
    }

    /// <summary>Legt de volledige toestand vast, zodat het spel kan worden opgeslagen.</summary>
    public GameSnapshot ToSnapshot() => new(
        Id,
        Rules,
        _board.Layout.ToRows(),
        Status,
        _players.Select(p => new PlayerSnapshot(p.Id, p.Name, p.Score, p.HasResigned, p.Rack.Select(TileSnapshot.From).ToList())).ToList(),
        _currentPlayerIndex,
        _consecutiveScorelessTurns,
        TurnStartedAt,
        _board.GetTiles().Select(t => new BoardTileSnapshot(t.Position.Row, t.Position.Column, TileSnapshot.From(t.Tile))).ToList(),
        _bag.Tiles.Select(TileSnapshot.From).ToList(),
        _moves.ToList());

    /// <param name="randomizer">Voor het schudden van de zak bij latere ruilbeurten.</param>
    public static Game Restore(GameSnapshot snapshot, IRandomizer randomizer)
    {
        var board = new Board(BoardLayout.Parse(snapshot.LayoutRows));
        foreach (var tile in snapshot.Board)
        {
            board.Place(new Position(tile.Row, tile.Column), tile.Tile.ToTile());
        }

        var players = snapshot.Players
            .Select(p => Player.Restore(p.Id, p.Name, p.Score, p.HasResigned, p.Rack.Select(t => t.ToTile())))
            .ToList();
        var bag = TileBag.Restore(snapshot.Bag.Select(t => t.ToTile()), randomizer);

        var game = new Game(snapshot.Id, players, snapshot.Rules, board, bag, snapshot.CurrentPlayerIndex, snapshot.TurnStartedAt)
        {
            Status = snapshot.Status,
            _consecutiveScorelessTurns = snapshot.ConsecutiveScorelessTurns,
        };
        game._moves.AddRange(snapshot.Moves);
        return game;
    }

    public Player GetPlayer(Guid playerId) =>
        _players.FirstOrDefault(p => p.Id == playerId)
        ?? throw new DomainException(DomainErrors.PlayerNotInGame, "Deze speler doet niet mee aan dit spel.");

    /// <summary>
    /// Laat de speler die aan de beurt is verliezen als de bedenktijd om is.
    /// </summary>
    /// <returns>True als het spel hierdoor is beëindigd.</returns>
    public bool ExpireTurnIfOverdue(DateTimeOffset now)
    {
        if (TurnDeadline is not { } deadline || now <= deadline)
        {
            return false;
        }

        CurrentPlayer.Resign();
        _moves.Add(MoveRecord.Timeout(CurrentPlayer.Id));
        Status = GameStatus.Finished;
        return true;
    }

    /// <summary>Berekent wat een zet zou opleveren, zonder hem te spelen.</summary>
    public PlacementResult PreviewPlacement(Guid playerId, IReadOnlyList<TilePlacement> placements, IWordDictionary dictionary)
    {
        EnsureInProgress();
        var player = GetPlayer(playerId);
        return new PlacementEvaluator(dictionary, Rules).Evaluate(_board, ToPlacedTiles(player, placements));
    }

    public MoveRecord PlaceTiles(Guid playerId, IReadOnlyList<TilePlacement> placements, IWordDictionary dictionary, DateTimeOffset now)
    {
        var player = EnsurePlayersTurn(playerId, now);
        var placedTiles = ToPlacedTiles(player, placements);
        var result = new PlacementEvaluator(dictionary, Rules).Evaluate(_board, placedTiles);

        player.TakeTiles(placements.Select(p => p.RackSymbol));
        foreach (var (position, tile) in placedTiles)
        {
            _board.Place(position, tile);
        }

        player.AddScore(result.Score);
        player.AddTiles(_bag.Draw(Rules.RackSize - player.Rack.Count));
        _consecutiveScorelessTurns = 0;

        var move = MoveRecord.Placement(player.Id, result, placedTiles.Count);
        _moves.Add(move);

        if (player.Rack.Count == 0)
        {
            Finish(finisher: player);
        }
        else
        {
            AdvanceTurn(now);
        }

        return move;
    }

    /// <param name="symbols">De te ruilen stenen: letters, of '?' voor een blanco steen.</param>
    public MoveRecord SwapTiles(Guid playerId, IReadOnlyList<char> symbols, DateTimeOffset now)
    {
        var player = EnsurePlayersTurn(playerId, now);
        if (symbols.Count == 0)
        {
            throw new DomainException(DomainErrors.NoTiles, "Kies minstens één steen om te ruilen.");
        }

        if (_bag.Count < Rules.MinTilesInBagToSwap)
        {
            throw new DomainException(
                DomainErrors.NotEnoughTilesToSwap,
                $"Ruilen kan alleen als er nog minstens {Rules.MinTilesInBagToSwap} stenen in de zak zitten.");
        }

        var returned = player.TakeTiles(symbols);
        player.AddTiles(_bag.Draw(returned.Count));
        _bag.Return(returned);

        return RecordScorelessTurn(MoveRecord.Swap(player.Id, returned.Count), now);
    }

    public MoveRecord Pass(Guid playerId, DateTimeOffset now)
    {
        var player = EnsurePlayersTurn(playerId, now);
        return RecordScorelessTurn(MoveRecord.Pass(player.Id), now);
    }

    /// <summary>Opgeven kan op elk moment, ook als je niet aan de beurt bent. Het spel eindigt direct.</summary>
    public MoveRecord Resign(Guid playerId, DateTimeOffset now)
    {
        ExpireTurnIfOverdue(now);
        EnsureInProgress();
        var player = GetPlayer(playerId);
        player.Resign();

        var move = MoveRecord.Resign(player.Id);
        _moves.Add(move);
        Status = GameStatus.Finished;
        return move;
    }

    private static List<PlacedTile> ToPlacedTiles(Player player, IReadOnlyList<TilePlacement> placements)
    {
        var tiles = player.FindTiles(placements.Select(p => p.RackSymbol));
        return placements
            .Select((placement, i) => new PlacedTile(
                placement.Position,
                placement.IsBlank ? tiles[i].WithBlankLetter(placement.Letter) : tiles[i]))
            .ToList();
    }

    private MoveRecord RecordScorelessTurn(MoveRecord move, DateTimeOffset now)
    {
        _moves.Add(move);
        _consecutiveScorelessTurns++;

        if (_consecutiveScorelessTurns >= Rules.ScorelessTurnsPerPlayerToEnd * _players.Count)
        {
            Finish(finisher: null);
        }
        else
        {
            AdvanceTurn(now);
        }

        return move;
    }

    /// <summary>
    /// Iedereen verliest de punten van de stenen op zijn plankje. Wie zijn plankje leeg speelde,
    /// krijgt die punten van de tegenstanders erbij.
    /// </summary>
    private void Finish(Player? finisher)
    {
        var remainingPoints = 0;
        foreach (var player in _players)
        {
            var points = player.RackPoints;
            player.AddScore(-points);
            remainingPoints += points;
        }

        finisher?.AddScore(remainingPoints);
        Status = GameStatus.Finished;
    }

    private void AdvanceTurn(DateTimeOffset now)
    {
        _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
        TurnStartedAt = now;
    }

    private Player EnsurePlayersTurn(Guid playerId, DateTimeOffset now)
    {
        ExpireTurnIfOverdue(now);
        EnsureInProgress();
        var player = GetPlayer(playerId);
        if (player != CurrentPlayer)
        {
            throw new DomainException(DomainErrors.NotYourTurn, $"Het is de beurt van {CurrentPlayer.Name}.");
        }

        return player;
    }

    private void EnsureInProgress()
    {
        if (Status != GameStatus.InProgress)
        {
            throw new DomainException(DomainErrors.GameFinished, "Dit spel is al afgelopen.");
        }
    }
}
