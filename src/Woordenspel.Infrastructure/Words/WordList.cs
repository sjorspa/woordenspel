using Woordenspel.Domain.Words;

namespace Woordenspel.Infrastructure.Words;

/// <summary>Woordenboek in het geheugen, geladen uit een tekstbestand met één woord per regel.</summary>
public sealed class WordList : IWordDictionary
{
    private readonly HashSet<string> _words;

    public WordList(IEnumerable<string> words)
    {
        _words = new HashSet<string>(
            words.Select(w => w.Trim()).Where(w => w.Length > 0),
            StringComparer.OrdinalIgnoreCase);
    }

    public int Count => _words.Count;

    public bool Contains(string word) => _words.Contains(word);

    public static WordList FromFile(string path) => new(File.ReadLines(path));
}
