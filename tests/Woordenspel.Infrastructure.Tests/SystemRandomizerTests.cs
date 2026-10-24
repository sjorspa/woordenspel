namespace Woordenspel.Infrastructure.Tests;

public class SystemRandomizerTests
{
    [Fact]
    public void Next_stays_within_bounds()
    {
        var randomizer = new SystemRandomizer();

        Assert.All(Enumerable.Range(0, 1000), _ => Assert.InRange(randomizer.Next(7), 0, 6));
    }
}
