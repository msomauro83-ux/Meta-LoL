using System.Globalization;
using MetaLol.Desktop;
using Microsoft.Maui.Controls.Shapes;

namespace MetaLol.Mobile;

public sealed class MainPage : ContentPage
{
    private static readonly Color Ink = Color.FromArgb("#F5F8FF");
    private static readonly Color Muted = Color.FromArgb("#9AAFC2");
    private static readonly Color Cyan = Color.FromArgb("#72E7FF");
    private readonly MobileCatalogService _catalogService = new();
    private readonly ManualDraft _draft = new();
    private readonly Dictionary<(string Role, bool Enemy), Button> _pickButtons = new();
    private readonly Label _catalogStatus = Text("Abriendo catálogo…", 12, Muted);
    private readonly Label _draftStatus = Text("Selecciona al menos un rival.", 13, Muted);
    private readonly Label _matchup = Text("", 14, Cyan);
    private readonly Label _summary = Text("", 14, Ink);
    private readonly Label _resultCount = Text("", 14, Cyan, true);
    private readonly VerticalStackLayout _priorities = new() { Spacing = 8 };
    private readonly VerticalStackLayout _cards = new() { Spacing = 12 };
    private readonly VerticalStackLayout _results = new() { Spacing = 14 };
    private readonly Button _refresh = ActionButton("Actualizar catálogo");
    private readonly Button _jump = ActionButton("Ver recomendaciones");
    private readonly ScrollView _scroll;
    private CatalogSnapshot? _catalog;
    private DraftAnalyzer? _analyzer;
    private bool _loading;
    private bool _refreshing;
    private bool _selectorOpen;

    public MainPage()
    {
        Title = "Meta LoL Estándar";
        SafeAreaEdges = new Microsoft.Maui.SafeAreaEdges(Microsoft.Maui.SafeAreaRegions.Container);
        BackgroundColor = Color.FromArgb("#07111F");
        var body = new VerticalStackLayout { Padding = new Thickness(16, 22, 16, 36), Spacing = 16, MaximumWidthRequest = 820, HorizontalOptions = LayoutOptions.Fill };
        body.Add(Text("META LOL · ANDROID", 12, Cyan, true));
        body.Add(Text("Estándar", 32, Ink, true));
        body.Add(Text("Draft manual para cualquier línea. Catálogo completo, sin champion pool predefinida.", 14, Muted));
        body.Add(_catalogStatus);
        _refresh.Clicked += async (_, _) => await RefreshCatalogAsync();
        body.Add(_refresh);

        var rolePicker = new Picker
        {
            Title = "Tu línea", ItemsSource = EditionProfile.Roles.Select(EditionProfile.RoleLabel).ToList(), SelectedIndex = 2,
            TextColor = Ink, TitleColor = Muted, BackgroundColor = Color.FromArgb("#101F33"), MinimumHeightRequest = 52,
            AutomationId = "RolePicker"
        };
        rolePicker.SelectedIndexChanged += (_, _) =>
        {
            if (rolePicker.SelectedIndex < 0) return;
            _draft.ChangeRole(EditionProfile.Roles[rolePicker.SelectedIndex]);
            UpdatePickButtons(); Analyze();
        };
        body.Add(Card(new VerticalStackLayout { Spacing = 10, Children = { Text("Tu línea", 20, Ink, true), rolePicker, _matchup } }));
        body.Add(BuildTeam(true));
        body.Add(BuildTeam(false));
        body.Add(_draftStatus);
        _jump.IsEnabled = false;
        _jump.Clicked += async (_, _) => await _scroll!.ScrollToAsync(_results, ScrollToPosition.Start, true);
        body.Add(_jump);
        var clear = ActionButton("Limpiar draft");
        clear.Clicked += (_, _) => { _draft.Clear(); UpdatePickButtons(); Analyze(); };
        body.Add(clear);

        _results.Add(Text("Las mejores opciones", 23, Ink, true));
        _results.Add(_resultCount);
        _results.Add(Text("Score de encaje sobre 100; no es un porcentaje de victoria. Análisis por reglas de kit y composición, sin historial personal ni maestría en esta edición manual.", 13, Muted));
        _results.Add(_summary); _results.Add(_priorities); _results.Add(_cards);
        _results.IsVisible = false;
        body.Add(_results);
        body.Add(Text("Selecciona los picks según su línea real para identificar el matchup. En bot, completa ADC y soporte de ambos equipos para valorar el 2v2.", 13, Muted));
        _scroll = new ScrollView { Content = body };
        Content = _scroll;
        UpdatePickButtons();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_catalog is not null || _loading) return;
        _loading = true;
        try
        {
            ApplyCatalog(await _catalogService.LoadLocalAsync());
            await RefreshCatalogAsync();
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException or InvalidDataException or KeyNotFoundException)
        {
            _catalogStatus.Text = "No se pudo abrir el catálogo. Pulsa Actualizar catálogo para recuperarlo con internet.";
        }
        finally { _loading = false; }
    }

    private async Task RefreshCatalogAsync()
    {
        if (_refreshing) return;
        _refreshing = true;
        _refresh.IsEnabled = false;
        _refresh.Text = "Buscando actualización…";
        try { ApplyCatalog(await _catalogService.RefreshAsync()); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or System.Text.Json.JsonException or InvalidDataException or KeyNotFoundException)
        {
            _catalogStatus.Text = _catalog is null ? "Sin conexión. Reintenta para descargar el catálogo." : $"Catálogo {_catalog.Version} · disponible sin conexión";
        }
        finally { _refreshing = false; _refresh.IsEnabled = true; _refresh.Text = "Actualizar catálogo"; }
    }

    private void ApplyCatalog(CatalogSnapshot catalog)
    {
        _catalog = catalog;
        _analyzer = new DraftAnalyzer(catalog.Champions);
        _catalogStatus.Text = $"Catálogo {catalog.Version} · {catalog.Champions.Count - 1} campeones · {(catalog.IsOffline ? "copia local" : "actualizado desde Riot")}";
        UpdatePickButtons(); Analyze();
    }

    private Border BuildTeam(bool enemy)
    {
        var rows = new VerticalStackLayout { Spacing = 10 };
        rows.Add(Text(enemy ? "Equipo rival" : "Tu equipo", 21, Ink, true));
        rows.Add(Text(enemy ? "Elige los rivales que ya conoces." : "Elige tus cuatro aliados. Tu línea queda libre para la recomendación.", 13, Muted));
        foreach (var role in EditionProfile.Roles)
        {
            var row = new Grid { ColumnDefinitions = { new(new GridLength(66)), new(GridLength.Star) }, ColumnSpacing = 8 };
            row.Add(new Label { Text = EditionProfile.RoleLabel(role), TextColor = Muted, FontSize = 12, VerticalTextAlignment = TextAlignment.Center });
            var button = ActionButton("Sin seleccionar");
            button.FontSize = 14;
            button.HorizontalOptions = LayoutOptions.Fill;
            button.AutomationId = $"{(enemy ? "Enemy" : "Ally")}{role}";
            button.Clicked += async (_, _) => await OpenChampionSearchAsync(role, enemy);
            row.Add(button, 1, 0);
            _pickButtons[(role, enemy)] = button;
            rows.Add(row);
        }
        return Card(rows);
    }

    private async Task OpenChampionSearchAsync(string role, bool enemy)
    {
        if (_catalog is null || _selectorOpen || (!enemy && role == _draft.Role)) return;
        _selectorOpen = true;
        try
        {
            // All champions are selectable in every slot, including off-role picks.
            var available = _catalog.Champions.Where(c => _draft.IsAvailable(c, role, enemy)).ToList();
            var page = new ChampionSearchPage($"{(enemy ? "Rival" : "Aliado")} · {EditionProfile.RoleLabel(role)}", available, champion =>
            {
                if (_draft.TrySetPick(role, enemy, champion)) { UpdatePickButtons(); Analyze(); }
            });
            page.Disappearing += (_, _) => _selectorOpen = false;
            await Navigation.PushModalAsync(page);
        }
        catch { _selectorOpen = false; throw; }
    }

    private void UpdatePickButtons()
    {
        foreach (var (slot, button) in _pickButtons)
        {
            var own = !slot.Enemy && slot.Role == _draft.Role;
            var champion = _draft.GetPick(slot.Role, slot.Enemy);
            button.Text = own ? "Tu línea · por elegir" : champion.IsEmpty ? "Elegir campeón…" : champion.Name;
            button.IsEnabled = !own && _catalog is not null;
            button.TextColor = own ? Muted : champion.IsEmpty ? Ink : Cyan;
        }
        _matchup.Text = $"{EditionProfile.RoleLabel(_draft.Role)} · Rival: {_draft.Opponent?.Name ?? "sin confirmar"}";
    }

    private void Analyze()
    {
        _jump.IsEnabled = _analyzer is not null && _draft.Enemies.Count > 0;
        _results.IsVisible = _jump.IsEnabled;
        _draftStatus.Text = $"{_draft.Enemies.Count}/5 rivales · {_draft.Allies.Count}/4 aliados · rol: {EditionProfile.RoleLabel(_draft.Role)}";
        if (!_results.IsVisible || _analyzer is null) return;
        var result = _draft.Analyze(_analyzer);
        _summary.Text = result.Summary;
        _resultCount.Text = $"{result.Recommendations.Count} opciones · {EditionProfile.RoleLabel(_draft.Role)}";
        _priorities.Clear();
        foreach (var priority in result.Priorities.Where(p => !p.Contains("Empates:"))) _priorities.Add(Text("• " + priority, 13, Muted));
        _cards.Clear();
        var position = 1;
        foreach (var pick in result.Recommendations) _cards.Add(RecommendationCard(pick, position++));
        if (position == 1) _cards.Add(Text("No quedan campeones disponibles para esta línea.", 15, Muted));
    }

    private static Border RecommendationCard(Recommendation pick, int position)
    {
        var content = new VerticalStackLayout { Spacing = 10 };
        var heading = new Grid { ColumnDefinitions = { new(new GridLength(46)), new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 10 };
        heading.Add(new Image { Source = pick.ImageUrl, WidthRequest = 46, HeightRequest = 46, Aspect = Aspect.AspectFill });
        var names = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
        names.Add(Text($"{position}. {pick.Name}", 18, Ink, true));
        names.Add(Text(pick.Verdict, 11, pick.Score >= 51 ? Cyan : Color.FromArgb("#FFBC80"), true));
        heading.Add(names, 1, 0);
        var score = new VerticalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center };
        score.Add(Text(pick.Score.ToString("F2", CultureInfo.GetCultureInfo("es-CL")), 22, Cyan, true));
        score.Add(Text("/ 100", 11, Muted));
        heading.Add(score, 2, 0);
        content.Add(heading);
        content.Add(Text(pick.Profile, 12, Muted));
        AddTactical("Por qué elegirlo", pick.Reason, Ink);
        AddTactical("Matchup", pick.MatchupReason, Ink);
        AddTactical("Riesgo", pick.Risk, Color.FromArgb("#FFBC80"));
        AddTactical("Plan de partida", pick.Plan, Ink);
        var breakdown = Text(pick.Breakdown, 12, Muted);
        breakdown.IsVisible = false;
        var toggle = ActionButton("Ver desglose del score");
        toggle.Clicked += (_, _) => { breakdown.IsVisible = !breakdown.IsVisible; toggle.Text = breakdown.IsVisible ? "Ocultar desglose" : "Ver desglose del score"; };
        content.Add(toggle); content.Add(breakdown);
        return Card(content);

        void AddTactical(string title, string value, Color color)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            content.Add(Text(title, 12, Cyan, true)); content.Add(Text(value, 14, color));
        }
    }

    private static Label Text(string value, double size, Color color, bool bold = false) => new()
    {
        Text = value, FontSize = size, TextColor = color, LineBreakMode = LineBreakMode.WordWrap,
        FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None
    };

    private static Button ActionButton(string text) => new()
    {
        Text = text, TextColor = Cyan, BackgroundColor = Color.FromArgb("#142B40"), FontSize = 14,
        CornerRadius = 12, MinimumHeightRequest = 48, Padding = new Thickness(12, 10)
    };

    private static Border Card(View content) => new()
    {
        Content = content, BackgroundColor = Color.FromArgb("#0D1A2C"), Stroke = Color.FromArgb("#24425C"),
        StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = 14
    };
}
