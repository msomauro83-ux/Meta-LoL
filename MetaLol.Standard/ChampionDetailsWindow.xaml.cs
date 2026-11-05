using System.Windows;
using System.Windows.Media.Imaging;

namespace MetaLol.Desktop;

public partial class ChampionDetailsWindow : Window
{
    public ChampionDetailsWindow(Champion champion, Recommendation recommendation, BuildSuggestion build)
    {
        InitializeComponent();
        ChampionName.Text = champion.Name;
        ChampionTitle.Text = champion.Title;
        RecommendationReason.Text = $"{recommendation.Reason}\n\n{recommendation.MatchupReason}\n\nRiesgos: {recommendation.Risk}\n\nPlan: {recommendation.Plan}";
        ScoreText.Text = $"{recommendation.Score:F2}/100";
        StatText.Text = $"{recommendation.StatLine}\n{recommendation.PersonalLine}\n{recommendation.Breakdown}";
        BuildNote.Text = build.Note;
        ItemsList.ItemsSource = build.Items;
        RunePagePanel.DataContext = build.RunePage;
        if (Uri.TryCreate(champion.ImageUrl, UriKind.Absolute, out var uri)) ChampionImage.Source = new BitmapImage(uri);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
