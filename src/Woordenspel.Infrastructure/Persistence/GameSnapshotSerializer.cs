using System.Text.Json;
using System.Text.Json.Serialization;
using Woordenspel.Domain.Games;
using Woordenspel.Domain.Tiles;

namespace Woordenspel.Infrastructure.Persistence;

public sealed class GameSnapshotSerializer(IRandomizer randomizer)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public string Serialize(Game game) => JsonSerializer.Serialize(game.ToSnapshot(), Options);

    public Game Deserialize(string json)
    {
        var snapshot = JsonSerializer.Deserialize<GameSnapshot>(json, Options)
            ?? throw new InvalidOperationException("Ongeldige spelgegevens in de database.");
        return Game.Restore(snapshot, randomizer);
    }
}
