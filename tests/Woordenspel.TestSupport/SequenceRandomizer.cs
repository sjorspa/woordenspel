using Woordenspel.Domain.Tiles;

namespace Woordenspel.TestSupport;

/// <summary>Geeft de opgegeven getallen achter elkaar terug, en begint daarna opnieuw.</summary>
public sealed class SequenceRandomizer(params int[] values) : IRandomizer
{
    private int _index;

    public int Next(int maxExclusive) => values[_index++ % values.Length] % maxExclusive;
}
