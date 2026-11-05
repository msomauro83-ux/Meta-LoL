namespace MetaLol.Desktop;

public sealed record Champion(
    string Id,
    string Key,
    string Name,
    string Title,
    string ImageUrl,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Roles)
{
    public const string EmptyId = "__empty__";
    public bool IsEmpty => Id == EmptyId;
    public bool Has(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

    public static Champion Empty { get; } = new(
        EmptyId, "0", "— Sin seleccionar —", string.Empty, string.Empty,
        Array.Empty<string>(), ["Top", "Jungle", "Mid", "ADC", "Support"]);
}

public sealed record CatalogSnapshot(string Version, IReadOnlyList<Champion> Champions, bool IsOffline, DateTimeOffset UpdatedAt);

public sealed record MatchupStat(string Patch, string Region, string Tier, string JunglerId, string OpponentId, int Games, int Wins)
{
    public double WinRate => Games == 0 ? 0.5 : (double)Wins / Games;
}

public sealed record Recommendation(
    string Id,
    string Name,
    string ImageUrl,
    double Score,
    string Profile,
    string Reason,
    string StatLine,
    bool HasRealStats)
{
    public double HistoricalWinRate { get; init; } = -1;
    public int HistoryGames { get; init; }
    public int MasteryLevel { get; init; }
    public long MasteryPoints { get; init; }
    public string PersonalLine { get; init; } = "Historial personal pendiente";
    public string Breakdown { get; init; } = string.Empty;
    public string MatchupReason { get; init; } = string.Empty;
    public string Risk { get; init; } = string.Empty;
    public string Plan { get; init; } = string.Empty;
    public string Verdict => Score >= 65 ? "PRIORITARIO" : Score >= 51 ? "VIABLE" : "ARRIESGADO";
}

public sealed record PersonalChampionStat(string ChampionId, int Games, int Wins,
    int RecentGames = 0, int RecentWins = 0, int MatchupGames = 0, int MatchupWins = 0)
{
    public double WinRate => Games > 0 ? (double)Wins / Games : 0.5;
    // A neutral prior prevents one win from outweighing a reliable draft advantage.
    public double AdjustedWinRate => (Wins + 6d) / (Games + 12d);
}

public sealed record ChampionMastery(string ChampionId, int Level, long Points);
public sealed record PersonalMatch(string GameId, string ChampionId, string OpponentId,
    bool Won, long PlayedAt, int QueueId, string Role = "Jungle");
public sealed record PlayerSnapshot(string PlayerId, IReadOnlyList<PersonalMatch> Matches,
    IReadOnlyList<ChampionMastery> Masteries, bool HistoryAvailable, bool MasteryAvailable);

public sealed record AnalysisResult(
    string Summary,
    IReadOnlyList<string> Priorities,
    IReadOnlyList<Recommendation> Recommendations,
    bool UsesRealStats);

public sealed record GameItem(string Id, string Name, string ImageUrl, string Description);
public sealed record RuneAsset(int Id, string Name, string ImageUrl);
public sealed record RuneSelection(string Step, RuneAsset Rune);
public sealed record RunePathSuggestion(
    int Id,
    string Name,
    string ColorName,
    string Accent,
    string ImageUrl,
    IReadOnlyList<RuneSelection> Selections);
public sealed record StatShard(string Step, string Name, string ImageUrl);
public sealed record RunePageSuggestion(
    RunePathSuggestion Primary,
    RunePathSuggestion Secondary,
    IReadOnlyList<StatShard> Shards);
public sealed record BuildSuggestion(
    IReadOnlyList<GameItem> Items,
    RunePageSuggestion RunePage,
    string Note);
