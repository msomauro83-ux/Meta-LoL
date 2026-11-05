using System.Globalization;
using System.Text.Json;

namespace MetaLol.Desktop;

// LCU payloads vary between patches. Missing identity/role/result is never guessed.
public static class LocalPlayerDataParser
{
    public static string Text(JsonElement value, string name) => value.ValueKind == JsonValueKind.Object &&
        value.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : "";
    public static long Number(JsonElement value, string name) => value.ValueKind == JsonValueKind.Object &&
        value.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetInt64(out var n) ? n : 0;

    public static IReadOnlyList<JsonElement> Games(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("games", out var games)) return [];
        if (games.ValueKind == JsonValueKind.Object && games.TryGetProperty("games", out var nested)) games = nested;
        return games.ValueKind == JsonValueKind.Array ? games.EnumerateArray().Select(x => x.Clone()).ToList() : [];
    }

    public static PersonalMatch? ParseMatch(JsonElement game, string puuid, long summonerId,
        IReadOnlyDictionary<int, string> champions)
    {
        var queue = (int)Number(game, "queueId");
        if (queue is not (400 or 420 or 430 or 440 or 490) || Number(game, "gameDuration") < 300 ||
            !game.TryGetProperty("participants", out var participants) || participants.ValueKind != JsonValueKind.Array ||
            !game.TryGetProperty("participantIdentities", out var identities) || identities.ValueKind != JsonValueKind.Array) return null;
        var localId = 0L;
        foreach (var identity in identities.EnumerateArray())
        {
            if (!identity.TryGetProperty("player", out var player)) continue;
            if ((!string.IsNullOrWhiteSpace(puuid) && Text(player, "puuid") == puuid) ||
                (summonerId > 0 && Number(player, "summonerId") == summonerId))
                localId = Number(identity, "participantId");
        }
        if (localId == 0) return null;
        var team = participants.EnumerateArray().ToArray();
        var local = team.FirstOrDefault(x => Number(x, "participantId") == localId);
        var role = local.ValueKind == JsonValueKind.Object ? ReadRole(local, team) : null;
        if (role is null ||
            !champions.TryGetValue((int)Number(local, "championId"), out var champion) ||
            !local.TryGetProperty("stats", out var stats) || !stats.TryGetProperty("win", out var won) ||
            won.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return null;
        var opponent = team.Where(x => Number(x, "teamId") != Number(local, "teamId") && ReadRole(x, team) == role).ToArray();
        var opponentId = opponent.Length == 1 && champions.TryGetValue((int)Number(opponent[0], "championId"), out var id) ? id : "";
        var gameId = Number(game, "gameId");
        if (gameId <= 0) return null;
        var playedAt = Number(game, "gameCreation");
        if (playedAt == 0 && DateTimeOffset.TryParse(Text(game, "gameCreationDate"), CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var created)) playedAt = created.ToUnixTimeMilliseconds();
        return new(gameId.ToString(CultureInfo.InvariantCulture), champion, opponentId, won.GetBoolean(), playedAt, queue, role);
    }

    private static string? ReadRole(JsonElement participant, IReadOnlyList<JsonElement> team)
    {
        var position = Text(participant, "teamPosition");
        var assigned = EditionProfile.NormalizeRole(position);
        if (assigned is not null) return assigned;
        if (participant.TryGetProperty("timeline", out var timeline))
        {
            var lane = Text(timeline, "lane");
            var timelineRole = Text(timeline, "role");
            if (lane == "BOTTOM") return timelineRole switch { "DUO_SUPPORT" => "Support", "DUO_CARRY" => "ADC", _ => null };
            if (EditionProfile.NormalizeRole(lane) is { } normalized) return normalized;
        }
        // Older payloads omit position; accept only the sole Smite carrier on that team.
        return HasSmite(participant) && team.Count(x => Number(x, "teamId") == Number(participant, "teamId") && HasSmite(x)) == 1 ? "Jungle" : null;
    }
    private static bool HasSmite(JsonElement p) => Number(p, "spell1Id") == 11 || Number(p, "spell2Id") == 11;
}
