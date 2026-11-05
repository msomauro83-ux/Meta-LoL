using MetaLol.Desktop;
using MetaLol.Mobile;
using Xunit;

namespace MetaLol.Standard.Mobile.Tests;

public sealed class ManualDraftTests
{
    private static readonly CatalogSnapshot Catalog = CatalogParser.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "champions-es_ES.json")), true);
    private static Champion C(string id) => Catalog.Champions.Single(c => c.Id == id);

    [Theory]
    [InlineData("Top", "Darius")][InlineData("Jungle", "Kayn")][InlineData("Mid", "Ahri")]
    [InlineData("ADC", "Caitlyn")][InlineData("Support", "Leona")]
    public void All_five_roles_recommend_from_catalog_without_a_fixed_pool(string role, string opponent)
    {
        var draft = new ManualDraft(); draft.ChangeRole(role);
        Assert.True(draft.TrySetPick(role, true, C(opponent)));
        Assert.Equal(opponent, draft.Opponent!.Id);
        var result = draft.Analyze(new DraftAnalyzer(Catalog.Champions));
        Assert.Equal(5, result.Recommendations.Count);
        Assert.Equal(result.Recommendations.OrderByDescending(r => r.Score).ThenBy(r => r.Name, StringComparer.Ordinal), result.Recommendations);
        Assert.All(result.Recommendations, pick =>
        {
            Assert.Contains(role, C(pick.Id).Roles);
            Assert.NotEqual(opponent, pick.Id);
            Assert.False(pick.HasRealStats);
            Assert.Equal(0, pick.HistoryGames);
            Assert.False(string.IsNullOrWhiteSpace(pick.MatchupReason));
            Assert.False(string.IsNullOrWhiteSpace(pick.Plan));
        });
    }

    [Fact]
    public void Switching_role_clears_our_slot_and_selects_the_correct_opponent()
    {
        var draft = new ManualDraft();
        draft.TrySetPick("Top", false, C("Darius"));
        draft.TrySetPick("Top", true, C("Garen"));
        draft.TrySetPick("Mid", true, C("Ahri"));
        draft.ChangeRole("Top");
        Assert.Empty(draft.Allies);
        Assert.True(draft.GetPick("Top", false).IsEmpty);
        Assert.Equal("Garen", draft.Opponent!.Id);
        Assert.False(draft.TrySetPick("Top", false, C("Jax")));
        draft.ChangeRole("Support");
        Assert.Null(draft.Opponent);
    }

    [Fact]
    public void Duplicate_champions_are_rejected_across_both_teams_and_can_be_freed()
    {
        var draft = new ManualDraft();
        Assert.True(draft.TrySetPick("Top", true, C("Jax")));
        Assert.True(draft.IsAvailable(C("Jax"), "Top", true));
        Assert.False(draft.TrySetPick("Jungle", true, C("Jax")));
        Assert.False(draft.TrySetPick("Support", false, C("Jax")));
        Assert.True(draft.TrySetPick("Top", true, Champion.Empty));
        Assert.True(draft.TrySetPick("Support", false, C("Jax")));
    }

    [Fact]
    public void Off_role_manual_picks_are_accepted_and_excluded_from_recommendations()
    {
        var draft = new ManualDraft(); draft.ChangeRole("Jungle");
        Assert.True(draft.TrySetPick("Support", false, C("Kayn")));
        Assert.True(draft.TrySetPick("Jungle", true, C("Vi")));
        var result = draft.Analyze(new DraftAnalyzer(Catalog.Champions));
        Assert.DoesNotContain(result.Recommendations, r => r.Id is "Kayn" or "Vi");
    }

    [Theory]
    [InlineData("kai sa", "Kaisa")][InlineData("chogath", "Chogath")]
    [InlineData("wukong", "MonkeyKing")][InlineData("LEBLANC", "Leblanc")]
    public void Full_catalog_search_handles_names_ids_and_punctuation(string query, string id) =>
        Assert.Contains(ManualDraft.Search(Catalog.Champions, query), c => c.Id == id);

    [Fact]
    public void Search_ignores_accents_and_retains_clear_selection()
    {
        var accented = new Champion("Test", "1", "Tést", "", "", [], ["Mid"]);
        var results = ManualDraft.Search(new[] { Champion.Empty, accented }, "test");
        Assert.Contains(accented, results); Assert.Contains(Champion.Empty, results);
    }

    [Fact]
    public void Bundled_catalog_has_all_champions_and_no_pool_filter()
    {
        Assert.True(Catalog.Champions.Count > 170);
        Assert.True(Catalog.IsOffline);
        Assert.Contains(Catalog.Champions, c => c.Id == "Jax");
        Assert.Contains(Catalog.Champions, c => c.Id == "Mel");
        Assert.All(Catalog.Champions.Where(c => !c.IsEmpty), c =>
        {
            Assert.NotEmpty(c.Roles);
            Assert.Contains(Catalog.Version, c.ImageUrl);
            Assert.StartsWith("https://", c.ImageUrl);
        });
    }

    [Fact]
    public void Incomplete_draft_works_without_guessing_the_lane_opponent()
    {
        var draft = new ManualDraft();
        draft.TrySetPick("Jungle", true, C("Vi"));
        Assert.Null(draft.Opponent);
        var result = draft.Analyze(new DraftAnalyzer(Catalog.Champions));
        Assert.Equal(5, result.Recommendations.Count);
        Assert.Contains("Matchup pendiente", result.Summary);
    }

    [Fact]
    public void Clear_removes_every_pick_but_keeps_the_manual_role()
    {
        var draft = new ManualDraft(); draft.ChangeRole("ADC");
        draft.TrySetPick("Top", false, C("Garen")); draft.TrySetPick("Top", true, C("Darius"));
        draft.Clear();
        Assert.Empty(draft.Enemies); Assert.Empty(draft.Allies); Assert.Null(draft.Opponent);
        Assert.Equal("ADC", draft.Role);
        Assert.Empty(draft.Analyze(new DraftAnalyzer(Catalog.Champions)).Recommendations);
    }
}
