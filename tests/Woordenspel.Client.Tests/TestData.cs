using Woordenspel.Contracts;
using Woordenspel.Domain.Boards;
using Woordenspel.Domain.Games;
using Woordenspel.TestSupport;

namespace Woordenspel.Client.Tests;

/// <summary>Kant-en-klare DTO's zoals de API ze stuurt.</summary>
internal static class TestData
{
    public static readonly Guid Anna = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid Bram = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    public static readonly Guid GameId = Guid.Parse("99999999-0000-0000-0000-000000000009");

    public static AuthResponse AnnaLogin { get; } = new(Anna, "anna", "token-anna", FakeTimeProvider.Start.AddDays(7));

    public static SetupDto Setup { get; } = new(
        15, 7, 7,
        [new(0, 0, SquareBonus.TripleWord), new(1, 1, SquareBonus.DoubleLetter), new(7, 3, SquareBonus.DoubleWord)],
        [new('A', 7, 1), new('K', 3, 3), new('T', 5, 2)],
        2,
        GameRules.Default);

    public static GameDto GameState(
        string rack = "KAT?EEN",
        Guid? currentPlayer = null,
        GameStatus status = GameStatus.InProgress,
        IReadOnlyList<BoardTileDto>? tiles = null,
        IReadOnlyList<MoveDto>? moves = null,
        long annaMs = 300_000,
        long bramMs = 300_000,
        IReadOnlyList<Guid>? winners = null) =>
        new(
            GameId,
            status,
            FakeTimeProvider.Start,
            status == GameStatus.InProgress ? currentPlayer ?? Anna : null,
            null,
            86,
            [new(Anna, "anna", 0, 7, false, annaMs), new(Bram, "bram", 0, 7, false, bramMs)],
            Board(tiles ?? []),
            moves ?? [],
            winners ?? [],
            rack.Select(c => new RackTileDto(c, c == '?', c switch { '?' => 0, 'K' => 3, 'T' => 2, _ => 1 })).ToList());

    public static BoardDto Board(IReadOnlyList<BoardTileDto> tiles)
    {
        var rows = Enumerable.Range(0, 15).Select(_ => new string('.', 15).ToCharArray()).ToArray();
        foreach (var tile in tiles)
        {
            rows[tile.Row][tile.Column] = tile.Letter;
        }

        return new BoardDto(15, rows.Select(r => new string(r)).ToList(), tiles);
    }
}
