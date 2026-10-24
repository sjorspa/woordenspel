namespace Woordenspel.Infrastructure;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Map voor de database en andere bestanden die bewaard moeten blijven.</summary>
    public string DataDirectory { get; set; } = "App_Data";

    public string ResolveDataDirectory() => Path.GetFullPath(DataDirectory);

    public string DatabasePath => Path.Combine(ResolveDataDirectory(), "woordenspel.db");
}
