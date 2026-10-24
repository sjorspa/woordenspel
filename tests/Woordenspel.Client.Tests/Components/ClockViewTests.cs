using Microsoft.Extensions.DependencyInjection;
using Woordenspel.Client.Components;
using Woordenspel.Client.Game;
using Woordenspel.TestSupport;
using static Woordenspel.Client.Tests.TestData;

namespace Woordenspel.Client.Tests.Components;

public class ClockViewTests : BunitContext
{
    private readonly FakeTimeProvider _clock = new();

    public ClockViewTests() => Services.AddSingleton<TimeProvider>(_clock);

    [Fact]
    public void Running_clock_counts_down()
    {
        var view = RenderClock(Anna, annaMs: 300_000);
        Assert.Equal("5:00", view.Find(".clock").TextContent);
        Assert.Contains("running", view.Find(".clock").ClassList);

        _clock.Advance(TimeSpan.FromSeconds(75));
        view.Render();

        Assert.Equal("3:45", view.Find(".clock").TextContent);
    }

    [Fact]
    public void Clock_of_the_waiting_player_stands_still()
    {
        var view = RenderClock(Bram, bramMs: 200_000);

        _clock.Advance(TimeSpan.FromSeconds(30));
        view.Render();

        Assert.Equal("3:20", view.Find(".clock").TextContent);
        Assert.DoesNotContain("running", view.Find(".clock").ClassList);
    }

    [Fact]
    public void Clock_turns_red_in_the_last_30_seconds()
    {
        var view = RenderClock(Anna, annaMs: 31_000);
        Assert.DoesNotContain("low", view.Find(".clock").ClassList);

        _clock.Advance(TimeSpan.FromSeconds(2));
        view.Render();

        Assert.Contains("low", view.Find(".clock").ClassList);
        Assert.Equal("0:29", view.Find(".clock").TextContent);
    }

    private IRenderedComponent<ClockView> RenderClock(Guid player, long annaMs = 300_000, long bramMs = 300_000) =>
        Render<ClockView>(p => p
            .Add(c => c.Clock, GameClock.For(GameState(annaMs: annaMs, bramMs: bramMs), _clock.GetUtcNow()))
            .Add(c => c.PlayerId, player));
}
