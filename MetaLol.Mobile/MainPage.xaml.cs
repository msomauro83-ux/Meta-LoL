using MetaLol.Desktop;

namespace MetaLol.Mobile;

public partial class MainPage : ContentPage
{
    private readonly MobileCatalogService _catalogService = new();
    private DraftAnalyzer? _analyzer;
    private bool _loaded;
    private bool _bindingPickers;

    private Picker[] EnemyPickers =>
        [EnemyTopPicker, EnemyJunglePicker, EnemyMidPicker, EnemyAdcPicker, EnemySupportPicker];

    private Picker[] AllyPickers =>
        [AllyTopPicker, AllyMidPicker, AllyAdcPicker, AllySupportPicker];

    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;
        await LoadCatalogAsync();
    }

    private async Task LoadCatalogAsync()
    {
        LoadingOverlay.IsVisible = true;
        LoadingLabel.Text = "Sincronizando campeones…";
        try
        {
            var catalog = await _catalogService.LoadAsync();
            _analyzer = new DraftAnalyzer(catalog.Champions, junglePool: JungleChampionPool.Selected);
            BindPicker(EnemyTopPicker, "Top");
            BindPicker(EnemyJunglePicker, "Jungle");
            BindPicker(EnemyMidPicker, "Mid");
            BindPicker(EnemyAdcPicker, "ADC");
            BindPicker(EnemySupportPicker, "Support");
            BindPicker(AllyTopPicker, "Top");
            BindPicker(AllyMidPicker, "Mid");
            BindPicker(AllyAdcPicker, "ADC");
            BindPicker(AllySupportPicker, "Support");
            PatchLabel.Text = $"PATCH {catalog.Version}{(catalog.IsOffline ? " · OFFLINE" : string.Empty)}";
            AnalyzeDraft();
        }
        catch (Exception ex)
        {
            _loaded = false;
            LoadingLabel.Text = "No se pudo cargar el catálogo";
            await DisplayAlertAsync(
                "Catálogo no disponible",
                $"Conecta el teléfono a internet e inténtalo nuevamente.\n\n{ex.Message}",
                "REINTENTAR");
            _ = LoadCatalogAsync();
            return;
        }
        finally
        {
            if (_analyzer is not null) LoadingOverlay.IsVisible = false;
        }
    }

    private void BindPicker(Picker picker, string role)
    {
        if (_analyzer is null) return;
        _bindingPickers = true;
        picker.ItemsSource = _analyzer.ForRole(role).ToList();
        picker.SelectedIndex = 0;
        _bindingPickers = false;
    }

    private void OnDraftChanged(object? sender, EventArgs e)
    {
        if (!_bindingPickers) AnalyzeDraft();
    }

    private void AnalyzeDraft()
    {
        if (_analyzer is null) return;
        var enemies = EnemyPickers.Select(SelectedChampion).Where(x => !x.IsEmpty).ToList();
        var allies = AllyPickers.Select(SelectedChampion).Where(x => !x.IsEmpty).ToList();
        var enemyJungler = SelectedChampion(EnemyJunglePicker);

        SelectionStatusLabel.Text = $"{enemies.Count}/5 rivales · {allies.Count}/4 aliados";
        if (enemies.Count == 0)
        {
            EmptyState.IsVisible = true;
            AnalysisPanel.IsVisible = false;
            RecommendationsSection.IsVisible = false;
            return;
        }

        var result = _analyzer.Analyze(enemies, allies, enemyJungler);
        EmptyState.IsVisible = false;
        AnalysisPanel.IsVisible = true;
        RecommendationsSection.IsVisible = true;
        SummaryLabel.Text = result.Summary;
        RenderPriorities(result.Priorities);
        RenderRecommendations(result.Recommendations);
    }

    private static Champion SelectedChampion(Picker picker) =>
        picker.SelectedItem as Champion ?? Champion.Empty;

    private void RenderPriorities(IReadOnlyList<string> priorities)
    {
        PrioritiesLayout.Children.Clear();
        foreach (var priority in priorities)
        {
            var row = new Grid
            {
                ColumnDefinitions = [new ColumnDefinition(new GridLength(22)), new ColumnDefinition(GridLength.Star)],
                ColumnSpacing = 8
            };
            row.Add(new Label
            {
                Text = "◆",
                TextColor = Color.FromArgb("#A481FF"),
                FontSize = 10,
                VerticalTextAlignment = TextAlignment.Center
            });
            row.Add(new Label
            {
                Text = priority,
                TextColor = Color.FromArgb("#D9E3ED"),
                FontSize = 13,
                LineBreakMode = LineBreakMode.WordWrap
            }, 1);
            PrioritiesLayout.Children.Add(row);
        }
    }

    private void RenderRecommendations(IReadOnlyList<Recommendation> recommendations)
    {
        RecommendationsLayout.Children.Clear();
        ResultCountLabel.Text = $"{recommendations.Count} OPCIONES";

        foreach (var recommendation in recommendations)
            RecommendationsLayout.Children.Add(CreateRecommendationCard(recommendation));

        if (recommendations.Count == 0)
        {
            RecommendationsLayout.Children.Add(new Label
            {
                Text = "No quedan junglas disponibles en tu pool para este draft.",
                TextColor = Color.FromArgb("#8EA4B9"),
                FontSize = 13,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(10, 16)
            });
        }
    }

    private static Border CreateRecommendationCard(Recommendation recommendation)
    {
        var card = new Border
        {
            BackgroundColor = Color.FromArgb("#0D1A2C"),
            Stroke = recommendation.Score >= 75 ? Color.FromArgb("#39D9FF") : Color.FromArgb("#203D58"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 17 },
            Padding = 13
        };
        var grid = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(new GridLength(58)), new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(54))],
            RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto)],
            ColumnSpacing = 12,
            RowSpacing = 4
        };
        var portrait = new Border
        {
            Stroke = Color.FromArgb("#2BC7EA"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 13 },
            HeightRequest = 56,
            WidthRequest = 56,
            Content = new Image { Source = recommendation.ImageUrl, Aspect = Aspect.AspectFill }
        };
        Grid.SetRowSpan(portrait, 3);
        grid.Add(portrait);
        grid.Add(new Label { Text = recommendation.Name, TextColor = Colors.White, FontSize = 18, FontAttributes = FontAttributes.Bold }, 1, 0);
        grid.Add(new Label { Text = recommendation.Profile.ToUpperInvariant(), TextColor = Color.FromArgb("#789CB3"), FontSize = 10, CharacterSpacing = 1 }, 1, 1);
        grid.Add(new Label { Text = $"{recommendation.Reason}\n{recommendation.MatchupReason}\nRiesgos: {recommendation.Risk}\nPlan: {recommendation.Plan}", TextColor = Color.FromArgb("#B7C7D6"), FontSize = 12, LineBreakMode = LineBreakMode.WordWrap }, 1, 2);

        var score = new Border
        {
            BackgroundColor = recommendation.Score >= 75 ? Color.FromArgb("#12394A") : Color.FromArgb("#17263C"),
            Stroke = Colors.Transparent,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(6, 8),
            VerticalOptions = LayoutOptions.Start,
            Content = new VerticalStackLayout
            {
                Spacing = 0,
                Children =
                {
                    new Label { Text = recommendation.Score.ToString("F2"), TextColor = Color.FromArgb("#72E7FF"), FontSize = 16, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center },
                    new Label { Text = "SCORE", TextColor = Color.FromArgb("#6F879B"), FontSize = 8, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center }
                }
            }
        };
        Grid.SetRowSpan(score, 2);
        grid.Add(score, 2, 0);
        card.Content = grid;
        return card;
    }

    private void OnClearClicked(object? sender, EventArgs e)
    {
        _bindingPickers = true;
        foreach (var picker in EnemyPickers.Concat(AllyPickers)) picker.SelectedIndex = 0;
        _bindingPickers = false;
        AnalyzeDraft();
    }
}
