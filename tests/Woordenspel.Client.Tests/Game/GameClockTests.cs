using Woordenspel.Client.Game;
using Woordenspel.Contracts;
using Woordenspel.TestSupport;
using static Woordenspel.Client.Tests.TestData;

namespace Woordenspel.Client.Tests.Game;

public class GameClockTests
{
    private static readonly DateTimeOffset Received = FakeTimeProvider.Start;

    [Fact]
    public void Clock_of_the_player_to_move_counts_down_from_the_moment_of_receipt()
    {
        var clock = GameClock.For(GameState(annaMs: 120_000, bramMs: 90_000), Received);

        Assert.Equal(TimeSpan.FromSeconds(110), clock.Remaining(Anna, Received.AddSeconds(10)));
        Assert.Equal(TimeSpan.FromSeconds(90), clock.Remaining(Bram, Received.AddSeconds(10)));
        Assert.True(clock.IsRunning(Anna));
        Assert.False(clock.IsRunning(Bram));
    }

    [Fact]
    public void Clock_never_goes_below_zero()
    {
        var clock = GameClock.For(GameState(annaMs: 1_000), Received);

        Assert.Equal(TimeSpan.Zero, clock.Remaining(Anna, Received.AddMinutes(1)));
    }

    [Fact]
    public void No_clock_runs_when_the_game_is_finished()
    {
        var clock = GameClock.For(GameState(status: Domain.Games.GameStatus.Finished, annaMs: 60_000), Received);

        Assert.Equal(TimeSpan.FromSeconds(60), clock.Remaining(Anna, Received.AddMinutes(5)));
    }

    [Fact]
    public void Players_without_a_clock_have_no_remaining_time()
    {
        var players = new List<PlayerDto> { new(Anna, "anna", 0, 7, false, null) };

        Assert.Null(new GameClock(players, Anna, Received).Remaining(Anna, Received));
    }

    [Theory]
    [InlineData(300_000, "5:00")]
    [InlineData(299_001, "5:00")]
    [InlineData(299_000, "4:59")]
    [InlineData(61_000, "1:01")]
    [InlineData(10_000, "0:10")]
    [InlineData(9_999, "0:09.9")]
    [InlineData(4_250, "0:04.2")]
    [InlineData(0, "0:00.0")]
    [InlineData(-500, "0:00.0")]
    public void Format_shows_minutes_and_seconds_and_tenths_at_the_end(long milliseconds, string expected)
    {
        Assert.Equal(expected, GameClock.Format(TimeSpan.FromMilliseconds(milliseconds)));
    }
}
