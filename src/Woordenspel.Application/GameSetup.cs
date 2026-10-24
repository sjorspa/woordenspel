using Woordenspel.Domain.Boards;
using Woordenspel.Domain.Games;
using Woordenspel.Domain.Tiles;

namespace Woordenspel.Application;

/// <summary>Alles wat bepaalt hoe een nieuw spel eruitziet.</summary>
public sealed record GameSetup(GameRules Rules, BoardLayout Layout, LetterSet LetterSet)
{
    public static GameSetup Wordfeud { get; } = new(GameRules.Default, BoardLayout.Standard, LetterSet.Dutch);
}
