using Woordenspel.Domain.Tiles;

namespace Woordenspel.Application;

/// <summary>Bedenkt namen voor gasten, zoals "VrolijkeVos42".</summary>
public sealed class GuestNameGenerator(IRandomizer randomizer)
{
    public static IReadOnlyList<string> Adjectives { get; } =
    [
        "Blije", "Dappere", "Gekke", "Gouden", "Grappige", "Groene", "Handige", "Kleine", "Koene", "Lieve",
        "Luie", "Rappe", "Rode", "Slimme", "Snelle", "Stille", "Stoere", "Vrolijke", "Wijze", "Zonnige",
    ];

    public static IReadOnlyList<string> Animals { get; } =
    [
        "Beer", "Bever", "Das", "Dolfijn", "Egel", "Eend", "Ekster", "Haas", "Kikker", "Konijn",
        "Merel", "Mol", "Mus", "Otter", "Panda", "Pinguin", "Uil", "Valk", "Vos", "Zeehond",
    ];

    public string Next() =>
        $"{Adjectives[randomizer.Next(Adjectives.Count)]}{Animals[randomizer.Next(Animals.Count)]}{randomizer.Next(100):00}";
}
