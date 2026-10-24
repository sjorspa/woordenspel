namespace Woordenspel.Domain.Words;

public interface IWordDictionary
{
    /// <summary>Of het woord is toegestaan. Hoofdletterongevoelig.</summary>
    bool Contains(string word);
}
