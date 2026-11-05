using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Net.Http;
using System.Text.Json;

namespace MetaLol.Desktop;

public partial class MainWindow : Window
{
    private readonly DataDragonService _dataDragon = new();
    private readonly DatabaseService _database = new();
    private readonly GameAssetsService _assets = new();
    private readonly LeagueClientService _leagueClient = new();
    private DraftAnalyzer? _analyzer;
    private CatalogSnapshot? _catalog;
    private bool _isClearing;
    private bool _isReady;
    private bool _isPollingClient;
    private bool _isClientDraftActive;
    private Champion? _clientOpponent;
    private string? _activePlayer;
    private IReadOnlyList<PersonalMatch> _personalMatches = [];
    private IReadOnlyList<ChampionMastery> _masteries = [];
    private IReadOnlyList<MatchupStat> _matchups = [];
    private IReadOnlySet<string> _bannedChampions = new HashSet<string>();
    private DateTimeOffset _nextPersonalSync = DateTimeOffset.MinValue;
    private string _lastClientPhase = "";
    private string? _opponentOverrideId;
    private string? _draftIdentity;
    private string? _lastDraftSignature;
    private string _manualRole = "Mid";
    private string? _detectedRole;
    private LeagueClientPollResult? _latestClientDraft;
    private string SelectedRole => _detectedRole ?? _manualRole;
    private bool _isSyncingPersonal;
    private readonly CancellationTokenSource _lifetime = new();
    private IReadOnlyList<Champion> _clientUnassignedEnemies = Array.Empty<Champion>();
    private IReadOnlyList<Champion> _clientUnassignedAllies = Array.Empty<Champion>();
    private readonly DispatcherTimer _updateTimer = new() { Interval = TimeSpan.FromMinutes(30) };
    private readonly DispatcherTimer _clientTimer = new() { Interval = TimeSpan.FromSeconds(2) };

    public MainWindow()
    {
        InitializeComponent();
        Title = EditionProfile.Title;
        ConfigureFilters();
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
        _updateTimer.Tick += UpdateTimer_Tick;
        _clientTimer.Tick += ClientTimer_Tick;
    }

    private void ResponsiveRoot_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (WorkspaceGrid is null) return;
        var compact = e.NewSize.Width < 1180;
        var stacked = e.NewSize.Width < 900;
        HeroGrid.Margin = compact ? new Thickness(16, 10, 16, 12) : new Thickness(30, 16, 30, 16);
        HeroTitle.FontSize = compact ? 24 : 30;
        HeroTitle.Margin = compact ? new Thickness(0) : new Thickness(0, 5, 0, 0);
        HeroEyebrow.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        HeroSubtitle.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        Grid.SetColumnSpan(HeroCopy, compact ? 2 : 1);
        Grid.SetRow(FilterPanel, compact ? 1 : 0);
        Grid.SetColumn(FilterPanel, compact ? 0 : 1);
        Grid.SetColumnSpan(FilterPanel, compact ? 2 : 1);
        FilterPanel.Margin = compact ? new Thickness(0, 7, 0, 0) : new Thickness(0);
        FilterPanel.HorizontalAlignment = compact ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        EngineBadge.Visibility = stacked ? Visibility.Collapsed : Visibility.Visible;
        WorkspaceScroll.Margin = compact ? new Thickness(16, 0, 16, 10) : new Thickness(30, 0, 30, 12);
        WorkspaceScroll.VerticalScrollBarVisibility = stacked ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
        WorkspaceGrid.VerticalAlignment = stacked ? VerticalAlignment.Top : VerticalAlignment.Stretch;
        DraftColumn.Width = stacked ? new GridLength(1, GridUnitType.Star) : new GridLength(compact ? 280 : 350);
        WorkspaceGap.Width = new GridLength(stacked ? 0 : compact ? 12 : 18);
        ResultsColumn.Width = stacked ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        DraftRow.Height = stacked ? GridLength.Auto : new GridLength(1, GridUnitType.Star);
        Grid.SetRow(RecommendationsPanel, stacked ? 1 : 0);
        Grid.SetColumn(RecommendationsPanel, stacked ? 0 : 2);
        RecommendationsPanel.Margin = stacked ? new Thickness(0, 12, 0, 0) : new Thickness(0);
        RecommendationsPanel.MinHeight = stacked ? 360 : 0;
        DraftScroll.MaxHeight = stacked ? Math.Clamp(e.NewSize.Height * .45, 220, 360) : double.PositiveInfinity;
        RecommendationsScroll.VerticalScrollBarVisibility = stacked ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            SystemStatusText.Text = "SYNCING DATA";
            CompositionSummary.Text = "Sincronizando catálogo oficial de Riot y base local...";
            await _database.InitializeAsync();
            _activePlayer = await _database.GetActivePlayerAsync();
            _personalMatches = await _database.LoadPersonalMatchesAsync(_activePlayer);
            _masteries = await _database.LoadMasteriesAsync(_activePlayer);
            UpdatePersonalStatus("caché local");
            _catalog = await _dataDragon.LoadAsync();
            await Task.WhenAll(
                _database.SaveChampionsAsync(_catalog.Champions),
                _assets.LoadAsync(_catalog.Version));
            await ReloadAnalyzerAsync();
            ConfigureChampionSelectors();
            foreach (var box in AllRoleBoxes()) box.SelectionChanged += RoleBox_SelectionChanged;
            RegionBox.SelectionChanged += Filter_SelectionChanged;
            RankBox.SelectionChanged += Filter_SelectionChanged;
            _isReady = true;
            ClearButton.IsEnabled = true;
            PatchText.Text = _catalog.Version;
            var totalSamples = await _database.GetMatchupCountAsync();
            SystemStatusText.Text = _catalog.IsOffline ? "OFFLINE CACHE" : "SYSTEM READY";
            FooterDataText.Text = totalSamples > 0
                ? $"DATA: {totalSamples:N0} MATCHUPS · PATCH {DatabaseService.NormalizePatch(_catalog.Version)}"
                : $"DATA: RIOT CATALOG · PATCH {_catalog.Version} · COUNTERS PENDING";
            ResetAnalysis();
            _updateTimer.Start();
            await PollLeagueClientAsync();
            _clientTimer.Start();
        }
        catch (Exception ex)
        {
            SystemStatusText.Text = "DATA ERROR";
            FooterDataText.Text = "DATA: NO DISPONIBLE";
            CompositionSummary.Text = $"No se pudo inicializar el catálogo: {ex.Message}";
        }
    }

    private async void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isReady || _catalog is null) return;
        try
        {
            var refreshed = await _dataDragon.LoadAsync();
            if (refreshed.Version == _catalog.Version) return;
            SystemStatusText.Text = "NEW PATCH";
            var boxes = AllRoleBoxes().ToList();
            var selectedIds = boxes.Select(box => (box.SelectedItem as Champion)?.Id).ToList();
            _isClearing = true;
            _catalog = refreshed;
            await Task.WhenAll(_database.SaveChampionsAsync(refreshed.Champions), _assets.LoadAsync(refreshed.Version));
            await ReloadAnalyzerAsync();
            ConfigureChampionSelectors();
            for (var index = 0; index < boxes.Count; index++)
            {
                var selected = boxes[index].Items.Cast<Champion>().FirstOrDefault(x => x.Id == selectedIds[index]);
                if (selected is not null) boxes[index].SelectedItem = selected;
            }
            _isClearing = false;
            PatchText.Text = refreshed.Version;
            SystemStatusText.Text = "PATCH UPDATED";
            AnalyzeDraft();
        }
        catch
        {
            SystemStatusText.Text = "OFFLINE CACHE";
            _isClearing = false;
        }
    }

    private void ConfigureFilters()
    {
        RegionBox.DisplayMemberPath = nameof(FilterOption.Label);
        RegionBox.SelectedValuePath = nameof(FilterOption.Value);
        RegionBox.ItemsSource = new[]
        {
            new FilterOption("LAS", "LAS"), new FilterOption("LAN", "LAN"), new FilterOption("NA", "NA"),
            new FilterOption("EUW", "EUW"), new FilterOption("KR", "KR"), new FilterOption("GLOBAL", "GLOBAL")
        };
        RegionBox.SelectedIndex = 0;

        RankBox.DisplayMemberPath = nameof(FilterOption.Label);
        RankBox.SelectedValuePath = nameof(FilterOption.Value);
        RankBox.ItemsSource = new[]
        {
            new FilterOption("Todos", "ALL"), new FilterOption("Oro+", "GOLD_PLUS"),
            new FilterOption("Platino+", "PLATINUM_PLUS"), new FilterOption("Esmeralda+", "EMERALD_PLUS"),
            new FilterOption("Diamante+", "DIAMOND_PLUS")
        };
        RankBox.SelectedIndex = 0;
        PlayerRoleBox.DisplayMemberPath = nameof(FilterOption.Label);
        PlayerRoleBox.SelectedValuePath = nameof(FilterOption.Value);
        PlayerRoleBox.ItemsSource = EditionProfile.Roles.Select(role => new FilterOption(EditionProfile.RoleLabel(role), role)).ToList();
        PlayerRoleBox.SelectedValue = _manualRole;
        UpdateRolePresentation();
    }

    private void PlayerRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isClearing) return;
        if (_detectedRole is null && PlayerRoleBox.SelectedValue is string role) _manualRole = role;
        _opponentOverrideId = null;
        UpdateRolePresentation();
        if (!_isReady) return;
        if (_latestClientDraft is not null)
        {
            _lastDraftSignature = null;
            ApplyClientDraft(_latestClientDraft);
        }
        else AnalyzeDraft();
    }

    private void UpdateRolePresentation()
    {
        var label = EditionProfile.RoleLabel(SelectedRole);
        RoleStatusText.Text = $"TU LÍNEA: {label} · {(_detectedRole is null ? "MANUAL" : "AUTOMÁTICA · LCU")}";
        RecommendationTitle.Text = $"Mejores 5 · {label}";
        MatchupStatusText.Text = $"{EditionProfile.RiotId} · {label} · rival por confirmar";
        DetectedOpponentLabel.Text = $"RIVAL DE {label.ToUpperInvariant()} · confirma si falta su línea";
        PlayerRoleBox.IsEnabled = _detectedRole is null;
        foreach (var role in EditionProfile.Roles)
        {
            var box = RoleBoxFor(role, allies: true);
            if (box is not null) box.IsEnabled = !_isClientDraftActive && role != SelectedRole;
        }
    }

    private void ConfigureChampionSelectors()
    {
        if (_analyzer is null) return;
        ConfigureRoleBox(TopBox, "Top");
        ConfigureRoleBox(JungleBox, "Jungle");
        ConfigureRoleBox(MidBox, "Mid");
        ConfigureRoleBox(AdcBox, "ADC");
        ConfigureRoleBox(SupportBox, "Support");
        ConfigureRoleBox(AllyTopBox, "Top");
        ConfigureRoleBox(AllyJungleBox, "Jungle");
        ConfigureRoleBox(AllyMidBox, "Mid");
        ConfigureRoleBox(AllyAdcBox, "ADC");
        ConfigureRoleBox(AllySupportBox, "Support");
    }

    private void ConfigureRoleBox(ComboBox box, string role)
    {
        // Picks may be off-role: every selector contains the complete catalog.
        box.ItemsSource = _analyzer!.Champions;
        box.IsTextSearchEnabled = true;
        box.MaxDropDownHeight = 260;
        box.ItemContainerStyle = (Style)FindResource("ChampionOptionStyle");
        box.SelectedIndex = 0;
    }

    private IEnumerable<ComboBox> EnemyRoleBoxes() => [TopBox, JungleBox, MidBox, AdcBox, SupportBox];
    private IEnumerable<ComboBox> AllAllyRoleBoxes() => [AllyTopBox, AllyJungleBox, AllyMidBox, AllyAdcBox, AllySupportBox];
    private IEnumerable<ComboBox> AllyRoleBoxes() => EditionProfile.Roles.Where(role => role != SelectedRole).Select(role => RoleBoxFor(role, true)!);
    private IEnumerable<ComboBox> AllRoleBoxes() => EnemyRoleBoxes().Concat(AllAllyRoleBoxes());

    private async void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isReady || _catalog is null) return;
        SystemStatusText.Text = "LOADING FILTER";
        await ReloadAnalyzerAsync();
        SystemStatusText.Text = "SYSTEM READY";
        AnalyzeDraft();
    }

    private async Task ReloadAnalyzerAsync()
    {
        if (_catalog is null) return;
        var version = _catalog.Version;
        var region = SelectedFilter(RegionBox, "LAS");
        var tier = SelectedFilter(RankBox, "ALL");
        var matchups = await _database.LoadMatchupsAsync(
            _catalog.Version,
            region, tier);
        if (_catalog.Version != version || SelectedFilter(RegionBox, "LAS") != region || SelectedFilter(RankBox, "ALL") != tier) return;
        _matchups = matchups;
        RebuildAnalyzer();
    }

    private void RebuildAnalyzer(Champion? opponent = null)
    {
        if (_catalog is null) return;
        var history = _personalMatches.Where(x => x.Role == SelectedRole).GroupBy(x => x.ChampionId).Select(group =>
        {
            var recent = group.OrderByDescending(x => x.PlayedAt).ThenByDescending(x => x.GameId).Take(20).ToList();
            var counter = group.Where(x => opponent is not null && x.OpponentId == opponent.Id).ToList();
            return new PersonalChampionStat(group.Key, group.Count(), group.Count(x => x.Won),
                recent.Count, recent.Count(x => x.Won), counter.Count, counter.Count(x => x.Won));
        });
        _analyzer = new DraftAnalyzer(_catalog.Champions, _matchups, history: history, mastery: _masteries);
    }

    private void UpdatePersonalStatus(string state) => PersonalStatusText.Text = _activePlayer is null
        ? "HISTORIAL: conecta el cliente para leer tus partidas y maestría."
        : $"TU HISTORIAL: {_personalMatches.Count} partidas · {_masteries.Count} maestrías · {state}";

    private async Task SyncPersonalDataAsync()
    {
        if (_catalog is null || _isSyncingPersonal || DateTimeOffset.UtcNow < _nextPersonalSync) return;
        _isSyncingPersonal = true;
        _nextPersonalSync = DateTimeOffset.UtcNow.AddMinutes(1);
        try
        {
            var known = _personalMatches.Where(x => !string.IsNullOrEmpty(x.OpponentId)).Select(x => x.GameId).ToHashSet();
            var snapshot = await _leagueClient.ReadPlayerSnapshotAsync(_catalog.Champions, known, _lifetime.Token);
            if (snapshot is null)
            {
                UpdatePersonalStatus("cliente sin identidad disponible; caché");
                return;
            }
            await _database.SavePlayerSnapshotAsync(snapshot);
            _activePlayer = snapshot.PlayerId;
            _personalMatches = await _database.LoadPersonalMatchesAsync(_activePlayer);
            _masteries = await _database.LoadMasteriesAsync(_activePlayer);
            UpdatePersonalStatus(snapshot.HistoryAvailable && snapshot.MasteryAvailable ? "sincronizado" :
                $"historial {(snapshot.HistoryAvailable ? "disponible" : "no disponible; caché")} · maestría {(snapshot.MasteryAvailable ? "disponible" : "no disponible; caché")}");
            RebuildAnalyzer();
            AnalyzeDraft();
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException or InvalidOperationException or Microsoft.Data.Sqlite.SqliteException)
        { UpdatePersonalStatus($"sincronización pendiente: {ex.Message}"); }
        finally { _isSyncingPersonal = false; }
    }

    private static string SelectedFilter(ComboBox box, string fallback) =>
        box.SelectedValue as string ?? fallback;

    private void RoleBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isClearing && _isReady) AnalyzeDraft();
    }

    private async void ClientTimer_Tick(object? sender, EventArgs e) => await PollLeagueClientAsync();

    private async Task PollLeagueClientAsync()
    {
        if (!_isReady || _isPollingClient) return;
        _isPollingClient = true;
        try
        {
            var result = await _leagueClient.PollAsync();
            if (result is null)
            {
                DeactivateClientDraft();
                SetClientStatus("LCU: CLIENTE CERRADO · MODO MANUAL");
                UpdatePersonalStatus("cliente cerrado; caché local");
                return;
            }

            if (_lastClientPhase != result.Phase)
            {
                _lastClientPhase = result.Phase;
                if (result.Phase is "EndOfGame" or "PreEndOfGame" or "Lobby") _nextPersonalSync = DateTimeOffset.MinValue;
            }
            if (!result.IsChampionSelect)
            {
                DeactivateClientDraft();
                SetClientStatus($"LCU: CONECTADO · {result.Phase}");
                _ = SyncPersonalDataAsync();
                return;
            }

            ApplyClientDraft(result);
            _ = SyncPersonalDataAsync();
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is TaskCanceledException ||
            ex is IOException ||
            ex is JsonException ||
            ex is InvalidOperationException)
        {
            SetClientStatus($"LCU: ERROR · {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            SetClientStatus($"LCU: ACCESO DENEGADO · {ex.Message}");
        }
        finally
        {
            _isPollingClient = false;
        }
    }

    private void ApplyClientDraft(LeagueClientPollResult result)
    {
        var resultRole = EditionProfile.NormalizeRole(result.LocalPosition) ??
            EditionProfile.NormalizeRole(result.AllyPicks.FirstOrDefault(p => p.IsLocalPlayer)?.Position);
        var signature = result.SessionId + ":" + (resultRole ?? _manualRole) + ":" + string.Join("|", result.EnemyPicks.Concat(result.AllyPicks)
            .Select(p => $"{p.CellId},{p.ChampionId},{p.Position},{p.IsLocalPlayer},{p.IsLocked}")) +
            ":" + string.Join(",", result.BannedChampionIds.Order());
        if (_isClientDraftActive && signature == _lastDraftSignature) return;
        _lastDraftSignature = signature;
        var draftIdentity = result.SessionId;
        if (draftIdentity != _draftIdentity) { _opponentOverrideId = null; _draftIdentity = draftIdentity; }
        var unknownEnemyRolePicks = new List<Champion>();
        var unknownAllyRolePicks = new List<Champion>();
        var unresolvedEnemyPicks = 0;
        _isClearing = true;
        _latestClientDraft = result;
        _detectedRole = resultRole;
        PlayerRoleBox.SelectedValue = SelectedRole;
        foreach (var box in AllRoleBoxes()) box.SelectedIndex = 0;
        _clientOpponent = null;
        _bannedChampions = result.BannedChampionIds.Select(FindChampion).Where(x => x is not null).Select(x => x!.Id).ToHashSet();

        foreach (var pick in result.EnemyPicks)
        {
            var champion = FindChampion(pick.ChampionId);
            if (champion is null)
            {
                unresolvedEnemyPicks++;
                continue;
            }

            var role = NormalizeClientPosition(pick.Position);
            var box = role is null ? null : RoleBoxFor(role, allies: false);
            if (box is null || box.SelectedItem is Champion { IsEmpty: false })
            {
                unknownEnemyRolePicks.Add(champion);
                continue;
            }

            SelectClientChampion(box, champion);
            if (role == SelectedRole) _clientOpponent = champion;
        }

        foreach (var pick in result.AllyPicks.Where(pick => !pick.IsLocalPlayer))
        {
            var champion = FindChampion(pick.ChampionId);
            var role = NormalizeClientPosition(pick.Position);
            var box = role is null ? null : RoleBoxFor(role, allies: true);
            if (champion is null) continue;
            if (box is not null && box.SelectedItem is not Champion { IsEmpty: false })
                SelectClientChampion(box, champion);
            else
                unknownAllyRolePicks.Add(champion);
        }

        _clientUnassignedEnemies = unknownEnemyRolePicks;
        _clientUnassignedAllies = unknownAllyRolePicks;
        _isClientDraftActive = true;
        UpdateRolePresentation();
        var detected = result.EnemyPicks.Select(p => FindChampion(p.ChampionId)).Where(x => x is not null).Cast<Champion>().DistinctBy(x => x.Id).ToList();
        DetectedOpponentBox.ItemsSource = new[] { Champion.Empty }.Concat(detected).ToList();
        var selectedJungle = detected.FirstOrDefault(x => x.Id == _opponentOverrideId) ?? _clientOpponent;
        DetectedOpponentBox.SelectedItem = selectedJungle ?? Champion.Empty;
        _clientOpponent = selectedJungle;
        DetectedOpponentPanel.Visibility = Visibility.Visible;
        LiveEnemyText.Text = "RIVAL: " + DescribePicks(result.EnemyPicks);
        LiveAllyText.Text = "ALIADOS: " + DescribePicks(result.AllyPicks);
        _isClearing = false;
        SetDraftInputsEnabled(false);
        var detectedCount = result.EnemyPicks.Count - unresolvedEnemyPicks;
        var allyDetectedCount = result.AllyPicks.Count(pick => !pick.IsLocalPlayer);
        var status = $"LCU: {EditionProfile.RoleLabel(SelectedRole).ToUpperInvariant()} {(_detectedRole is null ? "MANUAL" : "AUTO")} · {detectedCount}/5 RIVALES · {allyDetectedCount}/4 ALIADOS";
        if (unknownEnemyRolePicks.Count > 0)
            status += $" · {unknownEnemyRolePicks.Count} SIN LÍNEA";
        if (unresolvedEnemyPicks > 0)
            status += $" · {unresolvedEnemyPicks} ID NO RECONOCIDO";
        SetClientStatus(status);
        AnalyzeDraft();
    }

    private static void SelectClientChampion(ComboBox box, Champion champion)
    {
        // A live off-role pick must remain visible even when absent from the static role catalog.
        if (!box.Items.Contains(champion)) box.ItemsSource = box.Items.Cast<Champion>().Append(champion).ToList();
        box.SelectedItem = champion;
    }

    private string DescribePicks(IEnumerable<LeagueClientPick> picks) => string.Join(" · ", picks.Select(p =>
        $"{FindChampion(p.ChampionId)?.Name ?? p.ChampionId.ToString()} ({NormalizeClientPosition(p.Position) ?? "línea por confirmar"}{(p.IsLocked ? "" : ", provisional")}{(p.IsLocalPlayer ? ", tú" : "")})"));

    private void DetectedOpponent_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isClearing || !_isClientDraftActive) return;
        _clientOpponent = DetectedOpponentBox.SelectedItem as Champion;
        _opponentOverrideId = _clientOpponent is { IsEmpty: false } ? _clientOpponent.Id : null;
        AnalyzeDraft();
    }

    private void DeactivateClientDraft()
    {
        if (!_isClientDraftActive) return;
        _isClearing = true;
        foreach (var box in AllRoleBoxes()) box.SelectedIndex = 0;
        _clientOpponent = null;
        _clientUnassignedEnemies = Array.Empty<Champion>();
        _clientUnassignedAllies = Array.Empty<Champion>();
        _isClientDraftActive = false;
        _detectedRole = null;
        _latestClientDraft = null;
        PlayerRoleBox.SelectedValue = _manualRole;
        UpdateRolePresentation();
        _bannedChampions = new HashSet<string>();
        _opponentOverrideId = null;
        _draftIdentity = null;
        _lastDraftSignature = null;
        DetectedOpponentPanel.Visibility = Visibility.Collapsed;
        LiveEnemyText.Text = "";
        LiveAllyText.Text = "";
        _isClearing = false;
        SetDraftInputsEnabled(true);
        AnalyzeDraft();
    }

    private void SetDraftInputsEnabled(bool isEnabled)
    {
        foreach (var box in AllRoleBoxes()) box.IsEnabled = isEnabled;
        foreach (var role in EditionProfile.Roles)
            RoleBoxFor(role, true)!.IsEnabled = isEnabled && role != SelectedRole;
        ClearButton.IsEnabled = isEnabled && _isReady;
    }

    private void SetClientStatus(string status)
    {
        ClientStatusText.Text = status;
        ClientStatusText.ToolTip = status;
    }

    private Champion? FindChampion(int championId) =>
        _catalog?.Champions.FirstOrDefault(champion =>
            string.Equals(champion.Key, championId.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal));

    private static string? NormalizeClientPosition(string position) => EditionProfile.NormalizeRole(position);

    private ComboBox? RoleBoxFor(string role, bool allies) =>
        (role, allies) switch
        {
            ("Top", false) => TopBox,
            ("Jungle", false) => JungleBox,
            ("Mid", false) => MidBox,
            ("ADC", false) => AdcBox,
            ("Support", false) => SupportBox,
            ("Top", true) => AllyTopBox,
            ("Jungle", true) => AllyJungleBox,
            ("Mid", true) => AllyMidBox,
            ("ADC", true) => AllyAdcBox,
            ("Support", true) => AllySupportBox,
            _ => null
        };

    private void AnalyzeDraft()
    {
        if (_analyzer is null) return;
        var enemies = SelectedChampions(EnemyRoleBoxes())
            .Concat(_clientUnassignedEnemies)
            .DistinctBy(champion => champion.Id)
            .ToList();
        var allies = SelectedChampions(AllyRoleBoxes())
            .Concat(_clientUnassignedAllies)
            .DistinctBy(champion => champion.Id)
            .ToList();
        if (enemies.Count == 0)
        {
            ResetAnalysis();
            return;
        }

        var enemyJungler = _isClientDraftActive ? _clientOpponent : RoleBoxFor(SelectedRole, false)?.SelectedItem as Champion;
        RebuildAnalyzer(enemyJungler);
        var result = _analyzer!.Analyze(enemies, allies, enemyJungler, _bannedChampions, SelectedRole);
        MatchupStatusText.Text = $"{EditionProfile.RiotId} · {EditionProfile.RoleLabel(SelectedRole)} · " +
            (enemyJungler is { IsEmpty: false } ? $"vs {enemyJungler.Name}" : "rival por confirmar");
        CompositionSummary.Text = result.Summary;
        PrioritiesList.ItemsSource = result.Priorities;
        RecommendationsList.ItemsSource = result.Recommendations;
        EmptyRecommendations.Visibility = result.Recommendations.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        RecommendationCount.Text = result.UsesRealStats
            ? $"TOP {result.Recommendations.Count} · MUESTRA IMPORTADA"
            : $"TOP {result.Recommendations.Count} · VERDE = POOL DE NŸFU";
    }

    private static List<Champion> SelectedChampions(IEnumerable<ComboBox> boxes) => boxes
        .Select(x => x.SelectedItem as Champion)
        .Where(x => x is { IsEmpty: false })
        .Cast<Champion>()
        .DistinctBy(x => x.Id)
        .ToList();

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _isClearing = true;
        foreach (var box in AllRoleBoxes()) box.SelectedIndex = 0;
        _isClearing = false;
        _clientOpponent = null;
        _clientUnassignedEnemies = Array.Empty<Champion>();
        _clientUnassignedAllies = Array.Empty<Champion>();
        ResetAnalysis();
    }

    private void ResetAnalysis()
    {
        MatchupStatusText.Text = $"{EditionProfile.RiotId} · {EditionProfile.RoleLabel(SelectedRole)} · rival por confirmar";
        CompositionSummary.Text = _isReady
            ? "Selecciona un rival para iniciar el análisis."
            : "Inicializando catálogo y base local...";
        PrioritiesList.ItemsSource = null;
        RecommendationsList.ItemsSource = null;
        EmptyRecommendations.Visibility = Visibility.Visible;
        RecommendationCount.Text = _isReady ? "WAITING FOR INPUT" : "INITIALIZING";
    }

    private void EnemyMode_Click(object sender, RoutedEventArgs e)
    {
        EnemyInputs.Visibility = Visibility.Visible;
        AllyInputs.Visibility = Visibility.Collapsed;
        DraftPanelTitle.Text = "Composición rival";
        EnemyModeButton.Style = (Style)FindResource("PrimaryButton");
        AllyModeButton.Style = (Style)FindResource("SecondaryButton");
    }

    private void AllyMode_Click(object sender, RoutedEventArgs e)
    {
        EnemyInputs.Visibility = Visibility.Collapsed;
        AllyInputs.Visibility = Visibility.Visible;
        DraftPanelTitle.Text = "Composición aliada";
        EnemyModeButton.Style = (Style)FindResource("SecondaryButton");
        AllyModeButton.Style = (Style)FindResource("PrimaryButton");
    }

    private void Recommendation_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Recommendation recommendation } || _catalog is null) return;
        var champion = _catalog.Champions.FirstOrDefault(x => x.Id == recommendation.Id);
        if (champion is null) return;
        var details = new ChampionDetailsWindow(champion, recommendation, _assets.Suggest(champion)) { Owner = this };
        details.ShowDialog();
    }

    private async void ImportData_Click(object sender, RoutedEventArgs e)
    {
        if (!_isReady) return;
        var window = new DataImportWindow(_database, SelectedFilter(RegionBox, "LAS"), SelectedFilter(RankBox, "ALL")) { Owner = this };
        if (window.ShowDialog() != true) return;
        await ReloadAnalyzerAsync();
        var totalSamples = await _database.GetMatchupCountAsync();
        FooterDataText.Text = $"DATA: {totalSamples:N0} MATCHUPS · PATCH {DatabaseService.NormalizePatch(_catalog!.Version)}";
        AnalyzeDraft();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _updateTimer.Stop();
        _clientTimer.Stop();
        _leagueClient.Dispose();
        _lifetime.Cancel();
    }

    private sealed record FilterOption(string Label, string Value);
}
