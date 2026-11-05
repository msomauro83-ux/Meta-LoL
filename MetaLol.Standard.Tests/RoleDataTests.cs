using System.IO;
using System.Text.Json;
using MetaLol.Desktop;
using Xunit;

namespace MetaLol.Standard.Tests;

public sealed class RoleDataTests
{
    [Theory]
    [InlineData("top", "Top")][InlineData("jungle", "Jungle")][InlineData("middle", "Mid")]
    [InlineData("bottom", "ADC")][InlineData("utility", "Support")]
    public void Local_role_is_detected_before_a_champion_has_been_selected(string position, string expected)
    {
        var json = JsonSerializer.Serialize(new { localPlayerCellId = 7, myTeam = new[] {
            new { cellId = 1, championId = 24, assignedPosition = "jungle" },
            new { cellId = 7, championId = 0, assignedPosition = position } }, theirTeam = Array.Empty<object>() });
        using var doc = JsonDocument.Parse(json);
        var draft = LeagueClientService.ParseDraftSession(doc.RootElement);
        Assert.Equal(expected, draft.LocalPosition);
        Assert.DoesNotContain(draft.AllyPicks, p => p.IsLocalPlayer);
    }

    [Fact]
    public void Missing_local_position_does_not_use_another_players_role()
    {
        using var doc = JsonDocument.Parse("""{"localPlayerCellId":7,"myTeam":[{"cellId":1,"championId":24,"assignedPosition":"jungle"},{"cellId":7,"championId":0,"assignedPosition":""}]}""");
        Assert.Equal("", LeagueClientService.ParseDraftSession(doc.RootElement).LocalPosition);
    }

    [Theory]
    [InlineData("TOP", "Top")][InlineData("JUNGLE", "Jungle")][InlineData("MIDDLE", "Mid")]
    [InlineData("BOTTOM", "ADC")][InlineData("UTILITY", "Support")]
    public void History_records_the_actual_role_and_the_opponent_in_that_role(string position, string expected)
    {
        var game = new { gameId = 123, queueId = 420, gameDuration = 1800, gameCreation = 999,
            participantIdentities = new[] { new { participantId = 1, player = new { puuid = "me", summonerId = 42 } } },
            participants = new[] {
                new { participantId = 1, championId = 103, teamId = 100, teamPosition = position, stats = new { win = true } },
                new { participantId = 2, championId = 7, teamId = 200, teamPosition = position, stats = new { win = false } },
                new { participantId = 3, championId = 64, teamId = 200, teamPosition = position == "JUNGLE" ? "MIDDLE" : "JUNGLE", stats = new { win = false } } } };
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(game));
        var match = LocalPlayerDataParser.ParseMatch(doc.RootElement, "me", 42, new Dictionary<int, string> { [103] = "Ahri", [7] = "Leblanc", [64] = "LeeSin" });
        Assert.NotNull(match); Assert.Equal(expected, match.Role); Assert.Equal("Leblanc", match.OpponentId);
    }

    [Fact]
    public async Task History_is_saved_without_duplicates_and_filtered_by_account_and_role()
    {
        var path = Path.Combine(Path.GetTempPath(), $"standard-{Guid.NewGuid():N}.db");
        try
        {
            var db = new DatabaseService(path); await db.InitializeAsync();
            var snapshot = new PlayerSnapshot("friend", [new("1", "Ahri", "Leblanc", true, 10, 420, "Mid"),
                new("2", "Ahri", "Caitlyn", false, 20, 420, "ADC")], [new("Ahri", 12, 150000)], true, true);
            await db.SavePlayerSnapshotAsync(snapshot); await db.SavePlayerSnapshotAsync(snapshot);
            var mid = Assert.Single(await db.LoadPersonalStatsAsync("friend", "Leblanc", "Mid"));
            var adc = Assert.Single(await db.LoadPersonalStatsAsync("friend", role: "ADC"));
            Assert.Equal(1, mid.Games); Assert.Equal(1, mid.Wins); Assert.Equal(1, mid.MatchupWins);
            Assert.Equal(1, adc.Games); Assert.Equal(0, adc.Wins);
            Assert.Equal(2, (await db.LoadPersonalMatchesAsync("friend")).Count);
            Assert.Empty(await db.LoadPersonalStatsAsync("other", role: "Mid"));
            Assert.Equal(12, Assert.Single(await db.LoadMasteriesAsync("friend")).Level);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            foreach (var file in new[] { path, path + "-wal", path + "-shm" }) if (File.Exists(file)) File.Delete(file);
        }
    }
}
