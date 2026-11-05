using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MetaLol.Desktop;
using Xunit;

namespace MetaLol.Nyfu.Tests;

public sealed class WindowTests
{
    [Fact]
    public void Full_catalog_top_five_pool_colors_and_automatic_matchup_work_at_1024_by_768()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var app = new App(); app.InitializeComponent();
                var window = new MainWindow();
                var catalog = Catalog();
                Set(window, "_catalog", new CatalogSnapshot("16.20.1", catalog, true, DateTimeOffset.UtcNow));
                Set(window, "_isReady", true);
                Call(window, "RebuildAnalyzer", [null]); Call(window, "ConfigureChampionSelectors");
                Assert.Contains(EditionProfile.RiotId, window.Title);
                var playerRole = (ComboBox)window.FindName("PlayerRoleBox");
                Assert.Equal("Mid", playerRole.SelectedValue);
                Assert.True(playerRole.IsEnabled);
                foreach (var name in new[] { "TopBox", "JungleBox", "MidBox", "AdcBox", "SupportBox", "AllyTopBox", "AllyJungleBox", "AllyMidBox", "AllyAdcBox", "AllySupportBox" })
                {
                    var box = (ComboBox)window.FindName(name);
                    Assert.Equal(catalog.Count, box.Items.Count);
                    Assert.Contains(box.Items.Cast<Champion>(), c => c.Id == "Caitlyn");
                    Assert.Contains(box.Items.Cast<Champion>(), c => c.Id == "Darius");
                }

                var root = (FrameworkElement)window.Content; window.Content = null;
                var cases = new[] { ("Top", "TopBox", "Darius"), ("Jungle", "JungleBox", "Kayn"), ("Mid", "MidBox", "Yasuo"), ("ADC", "AdcBox", "Caitlyn"), ("Support", "SupportBox", "Leona") };
                foreach (var (role, boxName, enemy) in cases)
                {
                    playerRole.SelectedValue = role;
                    ((ComboBox)window.FindName(boxName)).SelectedItem = catalog.Single(c => c.Id == enemy);
                    Call(window, "AnalyzeDraft");
                    var picks = ((ItemsControl)window.FindName("RecommendationsList")).Items.Cast<Recommendation>().ToList();
                    Assert.Equal(5, picks.Count);
                    Assert.All(picks, r => Assert.Contains(role, catalog.Single(c => c.Id == r.Id).Roles));
                    Assert.Contains(enemy, ((TextBlock)window.FindName("MatchupStatusText")).Text);
                }

                playerRole.SelectedValue = "Mid";
                Render(root, window, "manual-mid", 1008, 712);
                var manualPicks = ((ItemsControl)window.FindName("RecommendationsList")).Items.Cast<Recommendation>().ToList();
                Assert.Contains(manualPicks, p => p.IsInChampionPool);
                foreach (var card in Descendants<Border>(root).Where(b => b.DataContext is Recommendation && b.BorderThickness.Left == 1 && b.CornerRadius.TopLeft == 9))
                {
                    var recommendation = (Recommendation)card.DataContext;
                    Assert.Equal(recommendation.IsInChampionPool ? Color.FromRgb(0x71, 0xF5, 0xB2) : Color.FromRgb(0x1D, 0x39, 0x52), ((SolidColorBrush)card.BorderBrush).Color);
                }

                LeagueClientPick Pick(string id, string role, int cell) => new(int.Parse(catalog.Single(c => c.Id == id).Key), role, cell, false) { IsLocked = true };
                var draft = new LeagueClientPollResult("ChampSelect", true,
                    [Pick("Garen", "top", 1), Pick("Amumu", "jungle", 2), Pick("Ahri", "middle", 3), Pick("Lulu", "utility", 5)],
                    [Pick("Darius", "top", 6), Pick("Kayn", "jungle", 7), Pick("Yasuo", "middle", 8), Pick("Caitlyn", "bottom", 9), Pick("Leona", "utility", 10)])
                    { SessionId = "nyfu-auto-role", LocalPosition = "ADC" };
                Call(window, "ApplyClientDraft", [draft]);
                Assert.Equal("ADC", playerRole.SelectedValue); Assert.False(playerRole.IsEnabled);
                Assert.Contains("AUTOMÁTICA", ((TextBlock)window.FindName("RoleStatusText")).Text);
                Assert.Contains("ADC", ((TextBlock)window.FindName("MatchupStatusText")).Text);
                Assert.Contains("Caitlyn", ((TextBlock)window.FindName("MatchupStatusText")).Text);
                Assert.DoesNotContain("vs Kayn", ((TextBlock)window.FindName("MatchupStatusText")).Text);
                Assert.Equal(5, ((ItemsControl)window.FindName("RecommendationsList")).Items.Count);
                Render(root, window, "auto-adc", 1008, 712);
                Render(root, window, "auto-adc-small", 803, 566);

                // A role swap with unchanged champion picks still updates the displayed lane and opponent.
                Call(window, "ApplyClientDraft", [draft with { LocalPosition = "Mid" }]);
                Assert.Equal("Mid", playerRole.SelectedValue);
                Assert.Contains("vs Yasuo", ((TextBlock)window.FindName("MatchupStatusText")).Text);
                Call(window, "DeactivateClientDraft");
                Assert.True(playerRole.IsEnabled); Assert.Equal("Mid", playerRole.SelectedValue);
                Assert.Contains("MANUAL", ((TextBlock)window.FindName("RoleStatusText")).Text);
                window.Close(); app.Shutdown();
            }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "WPF test timed out");
        if (error is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }

    private static void Render(FrameworkElement root, MainWindow window, string label, int width, int height)
    {
        root.Measure(new Size(width, height)); root.Arrange(new Rect(0, 0, width, height)); root.UpdateLayout();
        foreach (var name in new[] { "WindowControls", "HeroGrid", "FilterPanel", "FooterGrid", "WorkspaceScroll" })
        {
            var element = (FrameworkElement)window.FindName(name);
            var bounds = element.TransformToAncestor(root).TransformBounds(new Rect(element.RenderSize));
            Assert.True(bounds.Left >= -1 && bounds.Right <= width + 1, $"{name} overflowed {width}px: {bounds}");
            Assert.True(bounds.Top >= -1 && bounds.Bottom <= height + 1, $"{name} overflowed {height}px: {bounds}");
        }
        var image = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); image.Render(root);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../artifacts/nyfu"));
        Directory.CreateDirectory(directory);
        using var output = File.Create(Path.Combine(directory, $"preview-{label}.png")); encoder.Save(output);
    }

    private static IReadOnlyList<Champion> Catalog()
    {
        var ids = EditionProfile.ChampionPool.Concat(new[] { "Yasuo", "Syndra", "Orianna", "Viktor", "Annie", "Lux", "Garen", "Malphite", "Jax", "Gwen", "Ornn", "Renekton", "LeeSin", "Warwick", "Vi", "Jinx", "Ashe", "Ezreal", "Leona", "Lulu", "Rakan", "Morgana", "Nautilus", "Sona" }).Distinct().ToList();
        return ids.Select((id, i) =>
        {
            string[] tags = id is "Caitlyn" or "Xayah" or "Lucian" or "MissFortune" or "Vayne" or "Jinx" or "Ashe" or "Ezreal" ? ["Marksman"]
                : id is "Ahri" or "Leblanc" ? ["Mage", "Assassin"]
                : id is "Diana" or "Ekko" or "Kayn" ? ["Assassin", "Fighter"]
                : id is "Amumu" or "Malphite" or "Ornn" or "Leona" or "Nautilus" ? ["Tank"]
                : id is "Lulu" or "Rakan" or "Sona" ? ["Support"]
                : id is "Syndra" or "Orianna" or "Viktor" or "Annie" or "Lux" or "Morgana" ? ["Mage"] : ["Fighter"];
            return new Champion(id, (i + 1).ToString(), id, "", "", RoleCatalog.GetTacticalTags(id, tags), RoleCatalog.GetRoles(id, tags));
        }).Prepend(Champion.Empty).ToList();
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    private static void Call(object target, string name, object?[]? args = null) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);
}
