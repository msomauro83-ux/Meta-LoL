using System.Text.Json;
using System.IO;
using MetaLol.Desktop;
using Xunit;

namespace MetaLol.Tests;

public sealed class PersonalDataTests
{
    [Fact]
    public async Task Database_migration_preserves_imports_and_personal_sync_is_idempotent_and_account_scoped()
    {
        var path = Path.Combine(Path.GetTempPath(), $"metalol-tests-{Guid.NewGuid():N}.db");
        try
        {
            var db = new DatabaseService(path);
            await db.InitializeAsync();
            await db.RecordMatchAsync("16.20", "LAS", "ALL", "Vi", true, "Jax");
            await db.InitializeAsync();
            Assert.Equal(2, await db.GetMatchupCountAsync());
            var matches = Enumerable.Range(1, 25).Select(i => new PersonalMatch(i.ToString(), "Jax", i % 2 == 0 ? "Vi" : "LeeSin", i % 2 == 0, i, 420)).ToList();
            var snapshot = new PlayerSnapshot("account-a", matches, [new("Jax", 12, 200000)], true, true);
            await db.SavePlayerSnapshotAsync(snapshot);
            await db.SavePlayerSnapshotAsync(snapshot);
            var stats = Assert.Single(await db.LoadPersonalStatsAsync("account-a", "Vi"));
            Assert.Equal(25, stats.Games); Assert.Equal(12, stats.Wins);
            Assert.Equal(20, stats.RecentGames); Assert.Equal(10, stats.RecentWins);
            Assert.Equal(12, stats.MatchupGames); Assert.Equal(12, stats.MatchupWins);
            await db.SavePlayerSnapshotAsync(new("account-b", [new("1", "Jax", "Vi", false, 99, 420)], [], true, false));
            Assert.Equal(1, Assert.Single(await db.LoadPersonalStatsAsync("account-b")).Games);
            Assert.Equal(25, Assert.Single(await db.LoadPersonalStatsAsync("account-a")).Games);
            Assert.Empty(await db.LoadMasteriesAsync("account-b"));
            Assert.Equal(12, Assert.Single(await db.LoadMasteriesAsync("account-a")).Level);
            Assert.Equal("account-b", await db.GetActivePlayerAsync());
            await db.SavePlayerSnapshotAsync(new("account-a", [], [], false, false));
            Assert.Single(await db.LoadMasteriesAsync("account-a"));
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            foreach (var file in new[] { path, path + "-wal", path + "-shm" }) if (File.Exists(file)) File.Delete(file);
        }
    }

    private const string Game = """
        {"gameId":123,"queueId":420,"gameDuration":1800,"gameCreation":999,
         "participantIdentities":[{"participantId":1,"player":{"puuid":"me","summonerId":42}}],
         "participants":[
            {"participantId":1,"championId":24,"teamId":100,"spell1Id":11,"timeline":{"lane":"JUNGLE"},"stats":{"win":true}},
            {"participantId":2,"championId":64,"teamId":200,"spell1Id":11,"timeline":{"lane":"JUNGLE"},"stats":{"win":false}}]}
        """;

    [Fact]
    public void Match_parser_links_the_local_identity_and_opponent()
    {
        using var doc = JsonDocument.Parse(Game);
        var match = LocalPlayerDataParser.ParseMatch(doc.RootElement, "me", 42, new Dictionary<int, string> { [24] = "Jax", [64] = "LeeSin" });
        Assert.NotNull(match); Assert.Equal("Jax", match.ChampionId); Assert.Equal("LeeSin", match.OpponentId); Assert.True(match.Won);
        Assert.Null(LocalPlayerDataParser.ParseMatch(doc.RootElement, "someone-else", 0, new Dictionary<int, string> { [24] = "Jax" }));
    }

    [Theory]
    [InlineData("\"queueId\":420", "\"queueId\":450")]
    [InlineData("\"gameDuration\":1800", "\"gameDuration\":120")]
    [InlineData("\"lane\":\"JUNGLE\"", "\"lane\":\"TOP\"")]
    public void Aram_remakes_and_non_jungle_games_do_not_contaminate_personal_wr(string before, string after)
    {
        using var doc = JsonDocument.Parse(Game.Replace(before, after));
        Assert.Null(LocalPlayerDataParser.ParseMatch(doc.RootElement, "me", 42, new Dictionary<int, string> { [24] = "Jax" }));
    }

    [Fact]
    public void Champion_intents_appear_as_provisional_before_lock_in()
    {
        using var doc = JsonDocument.Parse("""{"myTeam":[{"cellId":1,"championId":0,"championPickIntent":24,"assignedPosition":"jungle"}]}""");
        var pick = Assert.Single(LeagueClientService.ReadTeam(doc.RootElement, "myTeam", 1));
        Assert.Equal(24, pick.ChampionId); Assert.False(pick.IsLocked);
    }

    [Fact]
    public void Live_draft_preserves_unknown_roles_and_marks_provisional_picks()
    {
        using var doc = JsonDocument.Parse("""
            {"localPlayerCellId":1,"myTeam":[{"cellId":1,"championId":24,"assignedPosition":"jungle"},
              {"cellId":2,"championId":64,"assignedPosition":""},{"cellId":3,"championId":0}],
             "actions":[[{"actorCellId":1,"type":"pick","completed":true}]]}
            """);
        var picks = LeagueClientService.ReadTeam(doc.RootElement, "myTeam", 1);
        Assert.Equal(2, picks.Count); Assert.True(picks[0].IsLocalPlayer); Assert.True(picks[0].IsLocked);
        Assert.Equal("", picks[1].Position); Assert.False(picks[1].IsLocked);
    }
}
