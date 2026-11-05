using System.Net;
using System.Net.Http;
using MetaLol.Desktop;
using Xunit;

namespace MetaLol.Tests;

public sealed class LeagueClientSyncTests
{
    private static readonly Champion[] Champions =
    [new("Jax", "24", "Jax", "", "", [], ["Jungle"]), new("LeeSin", "64", "Lee Sin", "", "", [], ["Jungle"])];

    [Fact]
    public async Task Unavailable_history_does_not_discard_available_mastery()
    {
        using var http = Client(path => path.Contains("current-summoner") && !path.Contains("matches")
            ? Ok("""{"puuid":"me","summonerId":42}""")
            : path.Contains("champion-mastery") ? Ok("""[{"championId":24,"championLevel":12,"championPoints":100000}]""")
            : new(HttpStatusCode.NotFound));
        using var service = new LeagueClientService(http);
        var snapshot = await service.ReadPlayerSnapshotAsync(Champions, new HashSet<string>());
        Assert.NotNull(snapshot); Assert.False(snapshot.HistoryAvailable); Assert.True(snapshot.MasteryAvailable);
        Assert.Equal(12, Assert.Single(snapshot.Masteries).Level); Assert.Empty(snapshot.Matches);
    }

    [Fact]
    public async Task Summary_backfill_fetches_opponent_and_deduplicates_only_within_the_same_account()
    {
        var player = "me"; var details = 0;
        using var http = Client(path =>
        {
            if (path.Contains("current-summoner") && !path.Contains("matches")) return Ok($$"""{"puuid":"{{player}}","summonerId":42}""");
            if (path.Contains("champion-mastery")) return Ok("[]");
            if (path.Contains("/games/")) { details++; return Ok(Game(player)); }
            return Ok("""{"games":{"games":[{"gameId":123}]}}""");
        });
        using var service = new LeagueClientService(http);
        var first = await service.ReadPlayerSnapshotAsync(Champions, new HashSet<string>());
        Assert.Equal("LeeSin", Assert.Single(first!.Matches).OpponentId); Assert.Equal(1, details);
        var second = await service.ReadPlayerSnapshotAsync(Champions, new HashSet<string> { "123" });
        Assert.Empty(second!.Matches); Assert.Equal(1, details);
        player = "second-account";
        var switched = await service.ReadPlayerSnapshotAsync(Champions, new HashSet<string> { "123" });
        Assert.Equal(player, switched!.PlayerId); Assert.Single(switched.Matches); Assert.Equal(2, details);
    }

    [Fact]
    public async Task A_login_change_during_sync_does_not_attribute_mastery_to_the_previous_account()
    {
        var identities = 0;
        using var http = Client(path => path.Contains("current-summoner") && !path.Contains("matches")
            ? Ok(++identities == 1 ? """{"puuid":"old","summonerId":42}""" : """{"puuid":"new","summonerId":99}""")
            : path.Contains("champion-mastery") ? Ok("[]") : Ok("""{"games":{"games":[]}}"""));
        using var service = new LeagueClientService(http);
        Assert.Null(await service.ReadPlayerSnapshotAsync(Champions, new HashSet<string>()));
    }

    private static string Game(string player) => $$$"""
        {"gameId":123,"queueId":420,"gameDuration":1800,"gameCreation":999,
         "participantIdentities":[{"participantId":1,"player":{"puuid":"{{{player}}}","summonerId":42}}],
         "participants":[
            {"participantId":1,"championId":24,"teamId":100,"timeline":{"lane":"JUNGLE"},"stats":{"win":true}},
            {"participantId":2,"championId":64,"teamId":200,"timeline":{"lane":"JUNGLE"},"stats":{"win":false}}]}
        """;
    private static HttpResponseMessage Ok(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json) };
    private static HttpClient Client(Func<string, HttpResponseMessage> response) => new(new Handler(response)) { BaseAddress = new Uri("https://127.0.0.1:12345/") };
    private sealed class Handler(Func<string, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response(request.RequestUri!.PathAndQuery));
    }
}
