using Woordenspel.Domain.Boards;

namespace Woordenspel.Domain.Tests.Boards;

public class BoardLayoutTests
{
    private static readonly BoardLayout Standard = BoardLayout.Standard;

    [Fact]
    public void Standard_board_is_15_by_15_with_center_start()
    {
        Assert.Equal(15, Standard.Size);
        Assert.Equal(new Position(7, 7), Standard.Center);
        Assert.Equal(SquareBonus.None, Standard.GetBonus(Standard.Center));
    }

    [Theory]
    [InlineData(0, 0, SquareBonus.TripleLetter)]
    [InlineData(0, 4, SquareBonus.TripleWord)]
    [InlineData(0, 7, SquareBonus.DoubleLetter)]
    [InlineData(2, 2, SquareBonus.DoubleWord)]
    [InlineData(1, 1, SquareBonus.DoubleLetter)]
    [InlineData(7, 3, SquareBonus.DoubleWord)]
    [InlineData(7, 8, SquareBonus.None)]
    public void Standard_board_has_wordfeud_bonuses(int row, int column, SquareBonus expected)
    {
        Assert.Equal(expected, Standard.GetBonus(new Position(row, column)));
    }

    [Fact]
    public void Standard_board_is_symmetric()
    {
        var last = Standard.Size - 1;
        for (var row = 0; row < Standard.Size; row++)
        {
            for (var column = 0; column < Standard.Size; column++)
            {
                var bonus = Standard.GetBonus(new Position(row, column));
                Assert.Equal(bonus, Standard.GetBonus(new Position(column, row)));
                Assert.Equal(bonus, Standard.GetBonus(new Position(last - row, column)));
                Assert.Equal(bonus, Standard.GetBonus(new Position(row, last - column)));
            }
        }
    }

    [Fact]
    public void Parse_reads_custom_layouts()
    {
        var layout = BoardLayout.Parse(["T.d", ".*.", "D.t"]);

        Assert.Equal(3, layout.Size);
        Assert.Equal(new Position(1, 1), layout.Center);
        Assert.Equal(SquareBonus.TripleWord, layout.GetBonus(new Position(0, 0)));
        Assert.Equal(SquareBonus.DoubleLetter, layout.GetBonus(new Position(0, 2)));
        Assert.Equal(SquareBonus.DoubleWord, layout.GetBonus(new Position(2, 0)));
        Assert.Equal(SquareBonus.TripleLetter, layout.GetBonus(new Position(2, 2)));
    }

    [Fact]
    public void ToRows_produces_input_for_Parse()
    {
        var rows = Standard.ToRows();

        Assert.Equal("t...T..d..T...t", rows[0]);
        Assert.Equal("d..D...*...D..d", rows[7]);
        Assert.Equal(rows, BoardLayout.Parse(rows).ToRows());
    }

    [Theory]
    [InlineData("..|.")]   // niet vierkant
    [InlineData("..|..")]  // geen startvakje
    [InlineData("**|..")]  // twee startvakjes
    [InlineData("*x|..")]  // onbekend teken
    public void Parse_rejects_invalid_layouts(string rows)
    {
        Assert.Throws<FormatException>(() => BoardLayout.Parse(rows.Split('|')));
    }
}
