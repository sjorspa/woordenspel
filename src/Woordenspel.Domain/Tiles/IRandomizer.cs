namespace Woordenspel.Domain.Tiles;

/// <summary>Bron van toeval, geabstraheerd zodat het schudden van de letterzak testbaar is.</summary>
public interface IRandomizer
{
    /// <summary>Geeft een getal in [0, <paramref name="maxExclusive"/>).</summary>
    int Next(int maxExclusive);
}
