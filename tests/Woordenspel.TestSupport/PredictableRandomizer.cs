using Woordenspel.Domain.Tiles;

namespace Woordenspel.TestSupport;

/// <summary>
/// Geeft altijd 0: de letterzak blijft in de opgegeven volgorde en de eerste speler begint.
/// Zo weten tests precies welke stenen een speler krijgt.
/// </summary>
public sealed class PredictableRandomizer : IRandomizer
{
    public int Next(int maxExclusive) => 0;
}
