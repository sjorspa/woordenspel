namespace Woordenspel.TestSupport;

public sealed class FakeTimeProvider : TimeProvider
{
    public static readonly DateTimeOffset Start = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public DateTimeOffset Now { get; set; } = Start;

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan duration) => Now += duration;
}
