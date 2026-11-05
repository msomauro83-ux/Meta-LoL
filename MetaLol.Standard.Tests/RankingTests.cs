using MetaLol.Desktop;
using Xunit;

namespace MetaLol.Standard.Tests;

public sealed class RankingTests
{
    private static Champion C(string id, string role = "Mid", params string[] tags) => new(id, "1", id, "", "", tags, [role]);

    [Fact]
    public void Standard_edition_has_no_fixed_player_or_champion_pool()
    {
        Assert.Equal("Meta LoL Estándar", EditionProfile.DisplayName);
        Assert.DoesNotContain("#", EditionProfile.Title);
        Assert.Null(typeof(EditionProfile).GetProperty("ChampionPool"));
        Assert.Null(typeof(Champion).GetProperty("IsInChampionPool"));
        Assert.Null(typeof(Recommendation).GetProperty("IsInChampionPool"));
        Assert.Null(typeof(Recommendation).GetProperty("PoolLabel"));
        Assert.EndsWith("MetaLol.Standard", AppDataPaths.Root);
    }

    [Theory]
    [InlineData("Top")][InlineData("Jungle")][InlineData("Mid")][InlineData("ADC")][InlineData("Support")]
    public void Every_role_ranks_the_whole_role_catalog_and_returns_only_five(string role)
    {
        var candidates = Enumerable.Range(1, 9).Select(i => C($"Outside{i}", role, "AD", "Fighter")).ToList();
        var enemy = C("Enemy", role, "AP");
        var analyzer = new DraftAnalyzer(candidates.Append(C("WrongRole", role == "Mid" ? "ADC" : "Mid")).ToList());
        var result = analyzer.Analyze([enemy], [], enemy, role: role);
        Assert.Equal(5, result.Recommendations.Count);
        Assert.All(result.Recommendations, r => Assert.StartsWith("Outside", r.Id));
        Assert.Equal(result.Recommendations.OrderByDescending(r => r.Score).ThenByDescending(r => r.HistoricalWinRate)
            .ThenByDescending(r => r.MasteryLevel).ThenByDescending(r => r.MasteryPoints).ThenBy(r => r.Name, StringComparer.Ordinal), result.Recommendations);
    }

    [Fact]
    public void Ranking_uses_personal_results_without_a_predefined_champion_allowlist()
    {
        var outsiders = Enumerable.Range(1, 6).Select(i => C($"Outside{i}")).ToList();
        var ahri = C("Ahri"); var enemy = C("Enemy");
        var history = outsiders.Select(c => new PersonalChampionStat(c.Id, 100, 90, 20, 18)).Append(new("Ahri", 100, 10, 20, 2));
        var result = new DraftAnalyzer(outsiders.Append(ahri).ToList(), history: history).Analyze([enemy], [], enemy, role: "Mid");
        Assert.Equal(5, result.Recommendations.Count);
        Assert.DoesNotContain(result.Recommendations, r => r.Id == "Ahri");
    }

    [Fact]
    public void Equal_scores_use_personal_wr_before_mastery()
    {
        var enemy = C("Enemy");
        var analyzer = new DraftAnalyzer([C("Ahri"), C("Outside")],
            history: [new("Ahri", 8, 6, 8, 6), new("Outside", 18, 12, 16, 11)], mastery: [new("Outside", 50, 1000000)]);
        var picks = analyzer.Analyze([enemy], [], null).Recommendations;
        Assert.Equal(picks[0].Score, picks[1].Score); Assert.Equal("Ahri", picks[0].Id);
    }

    [Fact]
    public void Occupied_and_banned_champions_are_not_suggested()
    {
        var enemy = C("Ahri"); var ally = C("Ekko");
        var result = new DraftAnalyzer([enemy, ally, C("Diana"), C("Leblanc")])
            .Analyze([enemy], [ally], enemy, new HashSet<string> { "Diana" });
        Assert.Equal("Leblanc", Assert.Single(result.Recommendations).Id);
    }

    [Fact]
    public void Imported_jungle_samples_do_not_become_mid_lane_statistics()
    {
        var c = C("Diana", "Mid", "AP"); var enemy = C("Yasuo", "Mid", "AD");
        var baseline = new DraftAnalyzer([c]).Analyze([enemy], [], enemy).Recommendations.Single();
        var imported = new DraftAnalyzer([c], [new("16.20", "LAS", "ALL", "Diana", "Yasuo", 1000, 900)])
            .Analyze([enemy], [], enemy).Recommendations.Single();
        Assert.Equal(baseline.Score, imported.Score); Assert.False(imported.HasRealStats);
    }

    [Fact]
    public void Ranged_lane_matchup_and_bot_pair_context_are_explained()
    {
        var c = C("Caitlyn", "ADC", "Range", "LongRange", "AD", "Marksman");
        var enemy = C("Lucian", "ADC", "Range", "ShortRange", "AD", "Marksman");
        var pick = new DraftAnalyzer([c]).Analyze([enemy], [], enemy, role: "ADC").Recommendations.Single();
        Assert.Contains("rango", pick.MatchupReason); Assert.Contains("2v2", pick.MatchupReason);
        Assert.Contains("trampas", pick.Plan); Assert.True(pick.Score > 50);
    }
}
