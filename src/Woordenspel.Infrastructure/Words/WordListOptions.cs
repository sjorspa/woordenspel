namespace Woordenspel.Infrastructure.Words;

public sealed class WordListOptions
{
    public const string SectionName = "WordList";

    /// <summary>Pad naar de woordenlijst; relatief ten opzichte van de applicatiemap.</summary>
    public string Path { get; set; } = System.IO.Path.Combine("Data", "woorden.txt");

    public string ResolvePath() =>
        System.IO.Path.IsPathRooted(Path) ? Path : System.IO.Path.Combine(AppContext.BaseDirectory, Path);
}
