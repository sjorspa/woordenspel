using Woordenspel.Domain.Tiles;

namespace Woordenspel.Infrastructure;

public sealed class SystemRandomizer : IRandomizer
{
    public int Next(int maxExclusive) => Random.Shared.Next(maxExclusive);
}
