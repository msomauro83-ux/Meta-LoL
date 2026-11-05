using MetaLol.Desktop;
using Xunit;

namespace MetaLol.Tests;

public sealed class PersonalRankingTests
{
    private static Champion C(string id, params string[] tags) => new(id, "1", id, "", "", tags, ["Jungle"]);

    [Fact]
    public void Jax_is_in_the_personal_pool_and_has_dueling_tools()
    {
        Assert.Equal(15, JungleChampionPool.Selected.Count);
        Assert.Contains("Jax", JungleChampionPool.Selected);
        var tags = RoleCatalog.GetTacticalTags("Jax", ["Fighter", "Assassin"]);
        Assert.Contains("Duelist", tags);
        Assert.Contains("AutoAttack", tags);
        Assert.Contains("CC", tags);
        Assert.Contains("Jungle", RoleCatalog.GetRoles("Jax", ["Fighter"]));
    }

    [Fact]
    public void Equal_scores_use_personal_wr_before_mastery_or_name()
    {
        var first = C("ZExperienced", "Engage");
        var second = C("AOther", "Engage");
        var enemy = C("Enemy", "AP");
        var analyzer = new DraftAnalyzer([first, second, enemy], history:
            [new(first.Id, 8, 6, 8, 6), new(second.Id, 18, 12, 16, 11)],
            mastery: [new(second.Id, 100, 1000000)]);
        var result = analyzer.Analyze([enemy], [], null).Recommendations;
        Assert.Equal(result[0].Score, result[1].Score);
        Assert.Equal(first.Id, result[0].Id);
        Assert.True(result[0].HistoricalWinRate > result[1].HistoricalWinRate);
    }

    [Fact]
    public void Equal_score_and_wr_use_mastery_level_then_points()
    {
        var a = C("A", "Engage"); var b = C("B", "Engage"); var c = C("C", "Engage"); var enemy = C("Enemy", "AP");
        var analyzer = new DraftAnalyzer([a, b, c, enemy], history: [new("A", 20, 10), new("B", 20, 10), new("C", 20, 10)],
            mastery: [new("A", 5, 100000), new("B", 6, 2000), new("C", 6, 3000)]);
        Assert.Equal(new[] { "C", "B", "A" }, analyzer.Analyze([enemy], [], null).Recommendations.Select(x => x.Id));
    }

    [Fact]
    public void One_personal_win_cannot_erase_a_clear_jungle_counter()
    {
        var counter = C("Poppy", "AntiDive", "Tank", "CC");
        var comfort = C("Comfort", "Scaling");
        var enemy = C("Enemy", "Dash", "Dive", "Early");
        var analyzer = new DraftAnalyzer([counter, comfort, enemy], history: [new("Comfort", 1, 1, 1, 1)]);
        var picks = analyzer.Analyze([enemy], [], enemy).Recommendations;
        Assert.Equal("Poppy", picks[0].Id);
        Assert.Contains("W", picks[0].MatchupReason);
        Assert.Contains("pocas partidas", picks.Single(x => x.Id == "Comfort").Risk);
    }

    [Fact]
    public void Own_matchup_history_changes_score_only_for_the_matching_opponent()
    {
        var c = C("Jax", "Duelist"); var enemy = C("Enemy", "AP");
        var neutral = new DraftAnalyzer([c, enemy], history: [new("Jax", 30, 15)]).Analyze([enemy], [], enemy).Recommendations.Single();
        var personal = new DraftAnalyzer([c, enemy], history: [new("Jax", 30, 15, MatchupGames: 10, MatchupWins: 9)])
            .Analyze([enemy], [], enemy).Recommendations.Single();
        Assert.True(personal.Score > neutral.Score);
        Assert.Contains("9/10", personal.MatchupReason);
    }

    [Fact]
    public void Picks_and_bans_are_removed_but_low_score_risks_remain_visible()
    {
        var enemy = C("Enemy", "Early", "CC"); var ally = C("Ally", "AD");
        var banned = C("Banned"); var weak = C("Weak", "Scaling", "Reset");
        var analyzer = new DraftAnalyzer([enemy, ally, banned, weak]);
        var pick = Assert.Single(analyzer.Analyze([enemy], [ally], enemy, new HashSet<string> { "Banned" }).Recommendations);
        Assert.Equal("Weak", pick.Id);
        Assert.True(pick.Score < 51);
        Assert.Equal("ARRIESGADO", pick.Verdict);
        Assert.NotEmpty(pick.Plan);
    }

    [Fact]
    public void High_fit_drafts_do_not_collapse_to_a_shared_score_of_100()
    {
        var a = C("Tank", "AP", "Tank", "Frontline", "CC", "Engage", "AntiAD", "Peel");
        var b = C("Other", "AP", "Tank", "CC", "Engage");
        var enemies = Enumerable.Range(1, 5).Select(i => C($"Enemy{i}", "AD", "Dive", "Engage")).ToArray();
        var allies = Enumerable.Range(1, 4).Select(i => C($"Ally{i}", "AD", "Range")).ToArray();
        var picks = new DraftAnalyzer([a, b]).Analyze(enemies, allies, null).Recommendations;
        Assert.NotEqual(picks[0].Score, picks[1].Score);
        Assert.All(picks, x => Assert.InRange(x.Score, 0, 99.99));
    }

    [Fact]
    public void Unknown_jungle_does_not_consume_an_imported_matchup_or_claim_a_counter()
    {
        var c = C("Jax"); var enemy = C("Enemy");
        var picks = new DraftAnalyzer([c, enemy], [new("16.20", "LAS", "ALL", "Jax", "Enemy", 1000, 800)])
            .Analyze([enemy], [], null).Recommendations;
        Assert.False(picks.Single().HasRealStats);
        Assert.Contains("pendiente", picks.Single().MatchupReason);
    }
}
