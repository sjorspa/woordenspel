using Woordenspel.Domain.Words;

namespace Woordenspel.TestSupport;

public sealed class FakeWordDictionary(params string[] words) : IWordDictionary
{
    private readonly HashSet<string> _words = new(words, StringComparer.OrdinalIgnoreCase);

    public bool Contains(string word) => _words.Contains(word);
}
