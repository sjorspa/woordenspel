using Woordenspel.Contracts;

namespace Woordenspel.Client.Game;

/// <summary>Een steen op het plankje. De <see cref="Id"/> blijft gelijk als het plankje wordt geschud.</summary>
public sealed record RackSlot(int Id, RackTileDto Tile);

/// <summary>Een steen die de speler op het bord heeft gezet maar nog niet heeft gespeeld.</summary>
public sealed record PendingTile(int SlotId, int Row, int Column, char Letter, bool IsBlank, int Points);

/// <summary>
/// De zet die een speler aan het samenstellen is: welke stenen van het plankje waar op het bord liggen,
/// welke steen geselecteerd is, en welke stenen geruild moeten worden. Bevat geen spelregels; die controleert
/// de server.
/// </summary>
public sealed class PlacementDraft
{
    private readonly List<RackSlot> _rack = [];
    private readonly Dictionary<(int Row, int Column), PendingTile> _pending = [];
    private readonly HashSet<int> _swapSelection = [];
    private Func<int, int, bool> _isOccupied = (_, _) => false;
    private int _boardSize;

    /// <summary>Het plankje in weergavevolgorde, inclusief stenen die al op het bord liggen.</summary>
    public IReadOnlyList<RackSlot> Rack => _rack;

    public IReadOnlyCollection<PendingTile> Pending => _pending.Values;

    public int? SelectedSlotId { get; private set; }

    public IReadOnlySet<int> SwapSelection => _swapSelection;

    public bool HasPending => _pending.Count > 0;

    public bool IsOnBoard(int slotId) => _pending.Values.Any(p => p.SlotId == slotId);

    public PendingTile? PendingAt(int row, int column) => _pending.GetValueOrDefault((row, column));

    /// <summary>
    /// Neemt een nieuw plankje en bord over. Is het plankje hetzelfde gebleven (de tegenstander speelde), dan
    /// blijven volgorde en neergelegde stenen staan, behalve op vakjes die inmiddels bezet zijn.
    /// </summary>
    public void Load(IReadOnlyList<RackTileDto> rack, int boardSize, Func<int, int, bool> isOccupied)
    {
        _boardSize = boardSize;
        _isOccupied = isOccupied;

        if (!SameTiles(rack))
        {
            _rack.Clear();
            _rack.AddRange(rack.Select((tile, i) => new RackSlot(i, tile)));
            _pending.Clear();
            _swapSelection.Clear();
            SelectedSlotId = null;
            return;
        }

        foreach (var key in _pending.Keys.Where(k => isOccupied(k.Row, k.Column)).ToList())
        {
            _pending.Remove(key);
        }
    }

    /// <summary>Selecteert een steen op het plankje, of heft de selectie op als hij al geselecteerd was.</summary>
    public void Select(int slotId)
    {
        if (IsOnBoard(slotId))
        {
            return;
        }

        SelectedSlotId = SelectedSlotId == slotId ? null : slotId;
    }

    public bool CanPlaceAt(int row, int column) =>
        row >= 0 && row < _boardSize && column >= 0 && column < _boardSize
        && !_isOccupied(row, column) && !_pending.ContainsKey((row, column));

    public bool IsBlank(int slotId) => Slot(slotId)?.Tile.IsBlank == true;

    /// <summary>Zet een steen op het bord. Een blanco steen heeft een <paramref name="blankLetter"/> nodig.</summary>
    /// <returns>False als dat vakje niet kan of de steen al ligt.</returns>
    public bool Place(int slotId, int row, int column, char? blankLetter = null)
    {
        if (Slot(slotId) is not { } slot || IsOnBoard(slotId) || !CanPlaceAt(row, column))
        {
            return false;
        }

        char letter;
        if (slot.Tile.IsBlank)
        {
            if (blankLetter is not { } chosen || !char.IsAsciiLetter(chosen))
            {
                return false;
            }

            letter = char.ToUpperInvariant(chosen);
        }
        else
        {
            letter = slot.Tile.Letter;
        }

        _pending[(row, column)] = new PendingTile(slotId, row, column, letter, slot.Tile.IsBlank, slot.Tile.Points);
        _swapSelection.Remove(slotId);
        if (SelectedSlotId == slotId)
        {
            SelectedSlotId = null;
        }

        return true;
    }

    /// <summary>Zet de geselecteerde steen op dit vakje.</summary>
    public bool PlaceSelected(int row, int column, char? blankLetter = null) =>
        SelectedSlotId is { } id && Place(id, row, column, blankLetter);

    public void Recall(int row, int column) => _pending.Remove((row, column));

    public void RecallAll() => _pending.Clear();

    public void Shuffle(Random random)
    {
        var shuffled = _rack.ToArray();
        random.Shuffle(shuffled);
        _rack.Clear();
        _rack.AddRange(shuffled);
    }

    public void ToggleSwap(int slotId)
    {
        if (IsOnBoard(slotId) || !_swapSelection.Add(slotId))
        {
            _swapSelection.Remove(slotId);
        }
    }

    public void ClearSwapSelection() => _swapSelection.Clear();

    public IReadOnlyList<TilePlacementDto> ToPlacements() =>
        _pending.Values
            .OrderBy(p => p.Row)
            .ThenBy(p => p.Column)
            .Select(p => new TilePlacementDto(p.Row, p.Column, p.Letter, p.IsBlank))
            .ToList();

    public IReadOnlyList<char> SwapLetters() =>
        _rack.Where(s => _swapSelection.Contains(s.Id)).Select(s => s.Tile.Letter).ToList();

    private RackSlot? Slot(int slotId) => _rack.FirstOrDefault(s => s.Id == slotId);

    private bool SameTiles(IReadOnlyList<RackTileDto> rack) =>
        rack.Count == _rack.Count
        && rack.Select(t => t.Letter).Order().SequenceEqual(_rack.Select(s => s.Tile.Letter).Order());
}
