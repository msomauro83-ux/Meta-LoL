using System.Reflection;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MetaLol.Desktop;
using Xunit;

namespace MetaLol.Tests;

public sealed class DraftWindowTests
{
    [Fact]
    public void Live_draft_displays_unknown_and_off_role_picks_and_supports_jungle_confirmation()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var app = new App(); app.InitializeComponent();
                var window = new MainWindow();
                Assert.True(DesktopWindowBehavior.GetEnabled(window));
                DesktopWindowBehavior.FitToWorkArea(window, new Rect(0, 0, 1024, 728));
                Assert.Equal(1008, window.Width); Assert.Equal(712, window.Height);
                Assert.Equal(8, window.Left); Assert.Equal(8, window.Top);
                Assert.True(window.MinWidth <= 1008 && window.MinHeight <= 712);
                VerifyDragSurfaces();
                VerifyImportFits();
                var ids = JungleChampionPool.Selected.Concat(new[] { "Ahri", "Ashe", "Lux", "Garen", "Zed", "Leona", "Jinx", "Sona" }).ToArray();
                var catalog = ids.Select((id, i) => new Champion(id, (i + 1).ToString(), id, "", "",
                    RoleCatalog.GetTacticalTags(id, id is "Garen" or "Leona" ? ["Tank"] : id is "Ashe" or "Jinx" ? ["Marksman"] : ["Fighter"]),
                    RoleCatalog.GetRoles(id, ["Fighter"]))).Prepend(Champion.Empty).ToList();
                Set(window, "_catalog", new CatalogSnapshot("16.20.1", catalog, true, DateTimeOffset.UtcNow));
                Set(window, "_isReady", true);
                Call(window, "RebuildAnalyzer", new object?[] { null });
                Call(window, "ConfigureChampionSelectors");
                LeagueClientPick Pick(string id, string role, int cell, bool local = false) =>
                    new(int.Parse(catalog.Single(c => c.Id == id).Key), role, cell, local) { IsLocked = true };
                var draft = new LeagueClientPollResult("ChampSelect", true,
                    [Pick("Jax", "jungle", 1, true), Pick("Garen", "top", 2), Pick("Zed", "middle", 3), Pick("Jinx", "bottom", 4), Pick("Sona", "utility", 5)],
                    [Pick("Ahri", "top", 6), Pick("XinZhao", "", 7), Pick("Ashe", "bottom", 8), Pick("Lux", "middle", 9), Pick("Leona", "utility", 10)])
                    { SessionId = "test-draft" };
                Call(window, "ApplyClientDraft", [draft]);
                Assert.Equal("Ahri", ((Champion)((ComboBox)window.FindName("TopBox")).SelectedItem).Id);
                Assert.Contains("XinZhao", ((TextBlock)window.FindName("LiveEnemyText")).Text);
                Assert.Contains("Jax", ((TextBlock)window.FindName("LiveAllyText")).Text);
                var jungle = (ComboBox)window.FindName("DetectedJungleBox");
                Assert.True(((Champion)jungle.SelectedItem).IsEmpty);
                jungle.SelectedItem = catalog.Single(c => c.Id == "XinZhao");
                var recommendations = (ItemsControl)window.FindName("RecommendationsList");
                Assert.Contains(recommendations.Items.Cast<Recommendation>(), r => r.Id == "Jax" && r.MatchupReason.Contains("E esquiva"));
                Assert.DoesNotContain(recommendations.Items.Cast<Recommendation>(), r => r.Id == "XinZhao");
                Call(window, "ApplyClientDraft", [draft]);
                Assert.Equal("XinZhao", ((Champion)jungle.SelectedItem).Id);
                ((TextBlock)window.FindName("PersonalStatusText")).Text = "VISTA DE PRUEBA · datos simulados · pool de 15 junglas";
                var root = (FrameworkElement)window.Content;
                window.Content = null;
                foreach (var (width, height) in new[] { (1300, 900), (1024, 728), (1008, 712), (803, 566), (640, 480), (1300, 900) })
                {
                    root.Measure(new Size(width, height)); root.Arrange(new Rect(0, 0, width, height)); root.UpdateLayout();
                    var resultsPanel = (FrameworkElement)window.FindName("RecommendationsPanel");
                    var workspace = (ScrollViewer)window.FindName("WorkspaceScroll");
                    Assert.Equal(width < 900 ? 1 : 0, Grid.GetRow(resultsPanel));
                    Assert.Equal(width < 900 ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled, workspace.VerticalScrollBarVisibility);
                    foreach (var name in new[] { "WindowControls", "HeroGrid", "FilterPanel", "FooterGrid", "WorkspaceScroll" })
                    {
                        var control = (FrameworkElement)window.FindName(name);
                        var bounds = control.TransformToAncestor(root).TransformBounds(new Rect(control.RenderSize));
                        Assert.True(bounds.Left >= -1 && bounds.Right <= width + 1, $"{name} escaped the {width}px viewport: {bounds}");
                        Assert.True(bounds.Top >= -1 && bounds.Bottom <= height + 1, $"{name} escaped the {height}px viewport: {bounds}");
                    }
                    Assert.True(resultsPanel.ActualWidth > 400, "Recommendation text must retain enough width to read");
                    var resultsScroll = (ScrollViewer)window.FindName("RecommendationsScroll");
                    if (width < 900)
                    {
                        Assert.True(workspace.ScrollableHeight > 0);
                        workspace.ScrollToEnd(); root.UpdateLayout();
                        Assert.InRange(workspace.VerticalOffset, workspace.ScrollableHeight - 1, workspace.ScrollableHeight + 1);
                        workspace.ScrollToTop(); root.UpdateLayout();
                    }
                    else
                    {
                        Assert.True(resultsScroll.ViewportHeight > 180, "Recommendations must have a useful viewport");
                        Assert.True(resultsScroll.ScrollableHeight > 0);
                    }
                    var image = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                    image.Render(root);
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
                    var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../artifacts"));
                    Directory.CreateDirectory(directory);
                    using var output = File.Create(Path.Combine(directory, $"MetaLoL-2.13-preview-{width}x{height}.png")); encoder.Save(output);
                }
                Call(window, "ApplyClientDraft", [draft with { SessionId = "new-draft" }]);
                Assert.True(((Champion)jungle.SelectedItem).IsEmpty);
                window.Close(); app.Shutdown();
            }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "WPF draft test timed out");
        if (error is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }

    private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    private static void Call(object target, string method, object?[]? args = null) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);

    private static void VerifyDragSurfaces()
    {
        Assert.True(DesktopWindowBehavior.CanDragFrom(new Grid()));
        Assert.True(DesktopWindowBehavior.CanDragFrom(new Border()));
        Assert.True(DesktopWindowBehavior.CanDragFrom(new TextBlock()));
        Assert.False(DesktopWindowBehavior.CanDragFrom(new TextBlock { Text = "Texto" }));
        Assert.False(DesktopWindowBehavior.CanDragFrom(new ComboBox()));
        Assert.False(DesktopWindowBehavior.CanDragFrom(new TextBox()));
        Assert.False(DesktopWindowBehavior.CanDragFrom(new PasswordBox()));
        Assert.False(DesktopWindowBehavior.CanDragFrom(new ScrollBar()));
        Assert.False(DesktopWindowBehavior.CanDragFrom(new Thumb()));
        var buttonContent = new Border();
        var button = new Button { Content = buttonContent };
        Assert.False(DesktopWindowBehavior.CanDragFrom(buttonContent));
        var run = new Run("Texto inline");
        var label = new TextBlock(run) { Width = 240, Height = 40 };
        label.Measure(new Size(240, 40)); label.Arrange(new Rect(0, 0, 240, 40));
        Assert.False(DesktopWindowBehavior.CanDragFrom(run));
        Assert.True(DesktopWindowBehavior.CanDragFrom(label,
            text => text.GetPositionFromPoint(new Point(220, 10), false) is not null));
        Assert.False(DesktopWindowBehavior.CanDragFrom(label,
            text => text.GetPositionFromPoint(new Point(5, 8), false) is not null));
        var legacySize = new Window { Width = 1300, Height = 900, MinWidth = 1100, MinHeight = 760 };
        DesktopWindowBehavior.FitToWorkArea(legacySize, new Rect(0, 0, 1024 / 1.25, 728 / 1.25));
        Assert.True(legacySize.Left >= 0 && legacySize.Top >= 0);
        Assert.True(legacySize.Left + legacySize.Width <= 1024 / 1.25);
        Assert.True(legacySize.Top + legacySize.Height <= 728 / 1.25);
        legacySize.Close();
    }

    private static void VerifyImportFits()
    {
        var import = new DataImportWindow(new DatabaseService(), "LAS", "ALL");
        Assert.True(DesktopWindowBehavior.GetEnabled(import));
        DesktopWindowBehavior.FitToWorkArea(import, new Rect(0, 0, 1024 / 1.25, 728 / 1.25));
        var root = (FrameworkElement)import.Content; import.Content = null;
        root.Measure(new Size(import.Width, import.Height));
        root.Arrange(new Rect(0, 0, import.Width, import.Height)); root.UpdateLayout();
        var button = (Button)import.FindName("ImportButton");
        var buttonBounds = button.TransformToAncestor(root).TransformBounds(new Rect(button.RenderSize));
        var input = (TextBox)import.FindName("MatchIdsBox");
        var inputBounds = input.TransformToAncestor(root).TransformBounds(new Rect(input.RenderSize));
        Assert.True(buttonBounds.Bottom <= import.Height);
        Assert.True(inputBounds.Bottom <= buttonBounds.Top, "Match ID input must not overlap the import button at 125% scale");
        Assert.True(input.ViewportHeight > 0);
        import.Close();
    }
}
