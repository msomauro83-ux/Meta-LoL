using MetaLol.Desktop;
using Xunit;

namespace MetaLol.Tests;

public sealed class DraftAnalyzerTests
{
    [Fact]
    public void Role_filter_never_leaks_another_lane()
    {
        var analyzer = new DraftAnalyzer([
            Champion.Empty,
            C("Darius", ["Top"], "AD", "Fighter"),
            C("Ahri", ["Mid"], "AP", "Mage"),
            C("Vi", ["Jungle"], "AD", "Fighter")]);

        var top = analyzer.ForRole("Top");

        Assert.Contains(top, x => x.IsEmpty);
        Assert.Contains(top, x => x.Id == "Darius");
        Assert.DoesNotContain(top, x => x.Id is "Ahri" or "Vi");
    }

    [Fact]
    public void Anti_ad_jungler_rises_against_physical_composition()
    {
        var rammus = C("Rammus", ["Jungle"], "AP", "Tank", "AntiAD", "CC");
        var vi = C("Vi", ["Jungle"], "AD", "Fighter", "Engage");
        var analyzer = new DraftAnalyzer([Champion.Empty, rammus, vi]);
        var enemies = new[] { C("Darius", ["Top"], "AD"), C("Zed", ["Mid"], "AD"), C("Draven", ["ADC"], "AD") };

        var result = analyzer.Analyze(enemies, [], null);

        Assert.Equal("Rammus", result.Recommendations[0].Name);
        Assert.Contains(result.Priorities, x => x.Contains("físico", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Real_matchup_sample_is_marked_and_used()
    {
        var vi = C("Vi", ["Jungle"], "AD", "Fighter");
        var amumu = C("Amumu", ["Jungle"], "AP", "Tank");
        var enemy = C("LeeSin", ["Jungle"], "AD", "Fighter");
        var stats = new[] { new MatchupStat("16.20", "LAS", "ALL", "Vi", "LeeSin", 500, 300) };
        var analyzer = new DraftAnalyzer([Champion.Empty, vi, amumu, enemy], stats);

        var result = analyzer.Analyze([enemy], [], enemy);

        Assert.True(result.UsesRealStats);
        var recommendation = Assert.Single(result.Recommendations, x => x.Id == "Vi");
        Assert.True(recommendation.HasRealStats);
        Assert.Contains("60,0", recommendation.StatLine.Replace('.', ','));
    }

    [Fact]
    public void Returns_every_jungler_at_or_above_51_without_a_top_seven_limit()
    {
        var enemy = C("EnemyMage", ["Mid"], "AP", "Mage");
        var junglers = Enumerable.Range(1, 12)
            .Select(index => C($"Jungler{index:00}", ["Jungle"], "AD", "Assassin", "Pick"))
            .ToList();
        var catalog = new List<Champion> { Champion.Empty, enemy };
        catalog.AddRange(junglers);
        var analyzer = new DraftAnalyzer(catalog);

        var result = analyzer.Analyze([enemy], [], null);

        Assert.Equal(12, result.Recommendations.Count);
        Assert.All(result.Recommendations, recommendation => Assert.True(recommendation.Score >= 51));
        Assert.Equal(
            result.Recommendations.OrderByDescending(recommendation => recommendation.Score).ThenBy(recommendation => recommendation.Name),
            result.Recommendations);
    }

    [Fact]
    public void Recommendations_are_limited_to_the_configured_champion_pool()
    {
        var enemy = C("EnemyCarry", ["ADC"], "AD", "Marksman");
        var vi = C("Vi", ["Jungle"], "AD", "Fighter", "Engage");
        var rammus = C("Rammus", ["Jungle"], "AP", "Tank", "Engage");
        var analyzer = new DraftAnalyzer(
            [Champion.Empty, enemy, vi, rammus],
            junglePool: new HashSet<string>(["Vi"], StringComparer.OrdinalIgnoreCase));

        var result = analyzer.Analyze([enemy], [], null);

        var recommendation = Assert.Single(result.Recommendations);
        Assert.Equal("Vi", recommendation.Id);
    }

    [Fact]
    public void Allied_composition_changes_the_recommended_jungler()
    {
        var enemy = C("EnemyTank", ["Top"], "AP", "Tank", "CC");
        var volibear = C("Volibear", ["Jungle"], "AP", "Tank", "Frontline", "Engage", "CC");
        var masterYi = C("MasterYi", ["Jungle"], "AD", "Assassin", "Dive", "Scaling");
        var analyzer = new DraftAnalyzer([Champion.Empty, enemy, volibear, masterYi]);

        var physicalAllies = new[]
        {
            C("AllyAdc", ["ADC"], "AD", "Marksman", "Range"),
            C("AllyMid", ["Mid"], "AD", "Assassin"),
            C("AllyTop", ["Top"], "AD", "Fighter")
        };
        var magicalAlliesWithFrontline = new[]
        {
            C("AllyTank", ["Top"], "AP", "Tank", "Frontline", "Engage", "CC"),
            C("AllyMage", ["Mid"], "AP", "Mage"),
            C("AllySupport", ["Support"], "AP", "Mage", "Peel")
        };

        var needsApAndFrontline = analyzer.Analyze([enemy], physicalAllies, null);
        var needsAdCarry = analyzer.Analyze([enemy], magicalAlliesWithFrontline, null);

        Assert.Equal("Volibear", needsApAndFrontline.Recommendations[0].Id);
        Assert.Contains("daño aliado con AP", needsApAndFrontline.Recommendations[0].Reason);
        Assert.Equal("MasterYi", needsAdCarry.Recommendations[0].Id);
        Assert.Contains("daño aliado con AD", needsAdCarry.Recommendations[0].Reason);
    }

    [Theory]
    [InlineData("16.20.1", "16.20")]
    [InlineData("15.9", "15.9")]
    [InlineData("unknown", "unknown")]
    public void Patch_normalization_is_stable(string input, string expected) =>
        Assert.Equal(expected, DatabaseService.NormalizePatch(input));

    [Theory]
    [InlineData("1629605898", "LAS", "LA2_1629605898")]
    [InlineData("1629605898", "LAN", "LA1_1629605898")]
    [InlineData("1629605898", "NA", "NA1_1629605898")]
    [InlineData("la2_1629605898", "LAS", "LA2_1629605898")]
    public void Match_id_is_normalized_for_the_selected_region(string input, string region, string expected) =>
        Assert.Equal(expected, RiotMatchImporter.NormalizeMatchId(input, region));

    [Fact]
    public void Numeric_match_id_requires_a_specific_region() =>
        Assert.Throws<ArgumentException>(() => RiotMatchImporter.NormalizeMatchId("1629605898", "GLOBAL"));

    private static Champion C(string id, string[] roles, params string[] tags) =>
        new(id, "1", id, string.Empty, string.Empty, tags, roles);
}
