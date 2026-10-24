using Woordenspel.Infrastructure.Words;

namespace Woordenspel.Infrastructure.Tests;

public class WordListTests
{
    [Fact]
    public void Contains_is_case_insensitive_and_ignores_whitespace_in_the_source()
    {
        var words = new WordList(["  kat ", "HOND", ""]);

        Assert.Equal(2, words.Count);
        Assert.True(words.Contains("KAT"));
        Assert.True(words.Contains("hond"));
        Assert.False(words.Contains("muis"));
    }

    [Fact]
    public void FromFile_reads_one_word_per_line()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(path, ["appel", "peer"]);

            var words = WordList.FromFile(path);

            Assert.True(words.Contains("APPEL"));
            Assert.True(words.Contains("PEER"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Bundled_dutch_word_list_is_usable()
    {
        var words = WordList.FromFile(new WordListOptions().ResolvePath());

        Assert.True(words.Count > 100_000);
        Assert.All(["HUIS", "KAT", "WOORDENSPEL", "ZO", "IJS", "QUIZ", "HM"], w => Assert.True(words.Contains(w), w));
        Assert.False(words.Contains("XQZWV"));
    }

    [Theory]
    [InlineData("GEEERD")]   // geëerd: accenten weg
    [InlineData("TAXIS")]    // taxi's: apostrof weg
    [InlineData("ZEEEGEL")]  // zee-egel: streepje weg
    public void Bundled_word_list_contains_words_written_without_accents_and_punctuation(string word)
    {
        Assert.True(BundledWords.Value.Contains(word));
    }

    [Theory]
    [InlineData("BTW")]        // afkorting zonder klinker
    [InlineData("CG")]         // symbool
    [InlineData("OV")]         // afkorting uit data/afkortingen.txt
    [InlineData("TVCAMERAS")]  // tv-camera's: TV is geen woord
    [InlineData("JANSEN")]     // eigennaam
    public void Bundled_word_list_excludes_abbreviations_and_names(string word)
    {
        Assert.False(BundledWords.Value.Contains(word));
    }

    private static readonly Lazy<WordList> BundledWords = new(() => WordList.FromFile(new WordListOptions().ResolvePath()));

    [Fact]
    public void ResolvePath_keeps_absolute_paths()
    {
        var absolute = Path.Combine(Path.GetTempPath(), "woorden.txt");

        Assert.Equal(absolute, new WordListOptions { Path = absolute }.ResolvePath());
    }
}
