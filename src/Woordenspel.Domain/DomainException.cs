namespace Woordenspel.Domain;

/// <summary>
/// Wordt gegooid wanneer een actie in strijd is met de spelregels.
/// <see cref="Code"/> is stabiel en bedoeld voor clients; de message is voor mensen.
/// </summary>
public class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class InvalidWordsException(IReadOnlyList<string> invalidWords)
    : DomainException(DomainErrors.InvalidWords, $"Ongeldige woorden: {string.Join(", ", invalidWords)}.")
{
    public IReadOnlyList<string> InvalidWords { get; } = invalidWords;
}

public static class DomainErrors
{
    public const string InvalidLetter = "InvalidLetter";
    public const string InvalidPlayerCount = "InvalidPlayerCount";
    public const string InvalidPlayerName = "InvalidPlayerName";
    public const string DuplicatePlayer = "DuplicatePlayer";
    public const string SeekNotForYou = "SeekNotForYou";
    public const string InvalidUsername = "InvalidUsername";
    public const string InvalidPassword = "InvalidPassword";
    public const string PlayerNotInGame = "PlayerNotInGame";
    public const string NotYourTurn = "NotYourTurn";
    public const string GameFinished = "GameFinished";

    public const string NoTiles = "NoTiles";
    public const string TilesNotOnRack = "TilesNotOnRack";
    public const string BlankLetterRequired = "BlankLetterRequired";
    public const string OutsideBoard = "OutsideBoard";
    public const string SquareOccupied = "SquareOccupied";
    public const string DuplicatePosition = "DuplicatePosition";
    public const string NotInLine = "NotInLine";
    public const string NotContiguous = "NotContiguous";
    public const string MustCoverCenter = "MustCoverCenter";
    public const string NotConnected = "NotConnected";
    public const string WordTooShort = "WordTooShort";
    public const string InvalidWords = "InvalidWords";
    public const string NotEnoughTilesToSwap = "NotEnoughTilesToSwap";
}
