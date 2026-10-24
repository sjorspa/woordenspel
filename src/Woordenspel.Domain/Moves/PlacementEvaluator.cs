using Woordenspel.Domain.Boards;
using Woordenspel.Domain.Games;
using Woordenspel.Domain.Tiles;
using Woordenspel.Domain.Words;

namespace Woordenspel.Domain.Moves;

/// <summary>
/// Controleert of een zet volgens de regels is, bepaalt welke woorden er ontstaan en berekent de score.
/// Wijzigt het bord niet.
/// </summary>
public sealed class PlacementEvaluator(IWordDictionary dictionary, GameRules rules)
{
    private static readonly Direction[] Directions = [Direction.Horizontal, Direction.Vertical];

    public PlacementResult Evaluate(IReadOnlyBoard board, IReadOnlyList<PlacedTile> placements)
    {
        var newTiles = CollectNewTiles(board, placements);

        if (DetermineDirection(placements) is { } direction)
        {
            EnsureContiguous(board, newTiles, direction);
        }

        EnsureConnected(board, newTiles);

        var words = FindWords(board, newTiles);
        if (words.Count == 0)
        {
            throw new DomainException(DomainErrors.WordTooShort, "Een woord moet uit minstens twee letters bestaan.");
        }

        var invalidWords = words.Select(w => w.Text).Where(w => !dictionary.Contains(w)).Distinct().ToList();
        if (invalidWords.Count > 0)
        {
            throw new InvalidWordsException(invalidWords);
        }

        var bingoBonus = placements.Count == rules.RackSize ? rules.BingoBonus : 0;
        return new PlacementResult(words, bingoBonus);
    }

    private static Dictionary<Position, Tile> CollectNewTiles(IReadOnlyBoard board, IReadOnlyList<PlacedTile> placements)
    {
        if (placements.Count == 0)
        {
            throw new DomainException(DomainErrors.NoTiles, "Leg minstens één steen.");
        }

        var newTiles = new Dictionary<Position, Tile>();
        foreach (var (position, tile) in placements)
        {
            if (tile.IsUnassignedBlank)
            {
                throw new DomainException(DomainErrors.BlankLetterRequired, "Een blanco steen moet een letter krijgen.");
            }

            if (!board.IsInside(position))
            {
                throw new DomainException(DomainErrors.OutsideBoard, $"Positie {position} ligt buiten het bord.");
            }

            if (board.GetTile(position) is not null)
            {
                throw new DomainException(DomainErrors.SquareOccupied, $"Vakje {position} is al bezet.");
            }

            if (!newTiles.TryAdd(position, tile))
            {
                throw new DomainException(DomainErrors.DuplicatePosition, $"Er worden meerdere stenen op {position} gelegd.");
            }
        }

        return newTiles;
    }

    /// <returns>De richting van de zet, of null bij één enkele steen.</returns>
    private static Direction? DetermineDirection(IReadOnlyList<PlacedTile> placements)
    {
        if (placements.Count == 1)
        {
            return null;
        }

        if (placements.All(p => p.Position.Row == placements[0].Position.Row))
        {
            return Direction.Horizontal;
        }

        if (placements.All(p => p.Position.Column == placements[0].Position.Column))
        {
            return Direction.Vertical;
        }

        throw new DomainException(DomainErrors.NotInLine, "Alle stenen moeten in één rij of één kolom liggen.");
    }

    private static void EnsureContiguous(IReadOnlyBoard board, Dictionary<Position, Tile> newTiles, Direction direction)
    {
        var ordered = newTiles.Keys
            .OrderBy(p => direction == Direction.Horizontal ? p.Column : p.Row)
            .ToList();

        for (var current = ordered[0]; current != ordered[^1]; )
        {
            current = current.Next(direction);
            if (!newTiles.ContainsKey(current) && board.GetTile(current) is null)
            {
                throw new DomainException(DomainErrors.NotContiguous, "Er mogen geen lege vakjes tussen de gelegde stenen zitten.");
            }
        }
    }

    private static void EnsureConnected(IReadOnlyBoard board, Dictionary<Position, Tile> newTiles)
    {
        if (board.IsEmpty)
        {
            if (!newTiles.ContainsKey(board.Layout.Center))
            {
                throw new DomainException(DomainErrors.MustCoverCenter, "Het eerste woord moet over het startvakje gaan.");
            }

            return;
        }

        var touchesExistingTile = newTiles.Keys.Any(p => p.Neighbours().Any(n => board.GetTile(n) is not null));
        if (!touchesExistingTile)
        {
            throw new DomainException(DomainErrors.NotConnected, "Het woord moet aansluiten op stenen die al op het bord liggen.");
        }
    }

    /// <summary>Alle woorden van twee of meer letters waarin minstens één nieuwe steen zit.</summary>
    private static List<FormedWord> FindWords(IReadOnlyBoard board, Dictionary<Position, Tile> newTiles)
    {
        Tile? TileAt(Position p) => newTiles.TryGetValue(p, out var tile) ? tile : board.GetTile(p);

        var words = new List<FormedWord>();
        var seen = new HashSet<(Position Start, Direction Direction)>();

        foreach (var direction in Directions)
        {
            foreach (var position in newTiles.Keys)
            {
                var start = position;
                while (TileAt(start.Previous(direction)) is not null)
                {
                    start = start.Previous(direction);
                }

                if (!seen.Add((start, direction)))
                {
                    continue;
                }

                var letters = new List<(Position Position, Tile Tile)>();
                for (var current = start; TileAt(current) is { } tile; current = current.Next(direction))
                {
                    letters.Add((current, tile));
                }

                if (letters.Count >= 2)
                {
                    var text = new string(letters.Select(l => l.Tile.Letter).ToArray());
                    words.Add(new FormedWord(text, start, direction, ScoreWord(board.Layout, letters, newTiles)));
                }
            }
        }

        return words;
    }

    /// <summary>Premievakjes tellen alleen voor stenen die in deze beurt zijn gelegd.</summary>
    private static int ScoreWord(BoardLayout layout, List<(Position Position, Tile Tile)> letters, Dictionary<Position, Tile> newTiles)
    {
        var letterTotal = 0;
        var wordMultiplier = 1;

        foreach (var (position, tile) in letters)
        {
            var bonus = newTiles.ContainsKey(position) ? layout.GetBonus(position) : SquareBonus.None;
            letterTotal += tile.Points * bonus.LetterMultiplier();
            wordMultiplier *= bonus.WordMultiplier();
        }

        return letterTotal * wordMultiplier;
    }
}
