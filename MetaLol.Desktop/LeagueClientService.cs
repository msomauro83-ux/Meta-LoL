using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Management;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MetaLol.Desktop;

public sealed class LeagueClientService : IDisposable
{
    private HttpClient? _httpClient;
    private string? _connectionSignature;
    public LeagueClientService(HttpClient? httpClient = null) => _httpClient = httpClient;

    public async Task<LeagueClientPollResult?> PollAsync(CancellationToken cancellationToken = default)
    {
        // Reuse the authenticated loopback connection while healthy; avoid a WMI process scan every two seconds.
        if (_httpClient is not null && _connectionSignature is not null)
        {
            try { return await ReadGameStateAsync(cancellationToken); }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { }
        }
        (int Port, string Token)? processConnection;
        try { processConnection = await Task.Run(FindProcessConnection, cancellationToken); }
        catch (Exception ex) when (ex is ManagementException or UnauthorizedAccessException or Win32Exception)
        { processConnection = null; }
        if (processConnection is not null)
        {
            ConfigureClient(processConnection.Value.Port, processConnection.Value.Token);
            return await ReadGameStateAsync(cancellationToken);
        }

        var lockfile = await ReadLockfileAsync(cancellationToken);
        if (lockfile is null) return null;
        ConfigureClient(lockfile.Value.Port, lockfile.Value.Token);
        return await ReadGameStateAsync(cancellationToken);
    }

    private async Task<LeagueClientPollResult> ReadGameStateAsync(CancellationToken cancellationToken)
    {
        var phaseJson = await _httpClient!.GetStringAsync("lol-gameflow/v1/gameflow-phase", cancellationToken);
        var phase = JsonSerializer.Deserialize<string>(phaseJson)
            ?? throw new InvalidDataException("El cliente de LoL devolvió una fase de juego vacía.");
        if (!string.Equals(phase, "ChampSelect", StringComparison.OrdinalIgnoreCase))
            return new(phase, false, Array.Empty<LeagueClientPick>(), Array.Empty<LeagueClientPick>());

        var sessionJson = await _httpClient.GetStringAsync("lol-champ-select/v1/session", cancellationToken);
        using var session = JsonDocument.Parse(sessionJson);
        var root = session.RootElement;
        int? localPlayerCellId = root.TryGetProperty("localPlayerCellId", out var localCellElement) &&
                                 localCellElement.TryGetInt32(out var localCell)
            ? localCell
            : null;
        var banned = new HashSet<int>();
        if (root.TryGetProperty("bans", out var bans))
            foreach (var name in new[] { "myTeamBans", "theirTeamBans" })
                if (bans.TryGetProperty(name, out var ids) && ids.ValueKind == JsonValueKind.Array)
                    foreach (var id in ids.EnumerateArray()) if (id.TryGetInt32(out var n) && n > 0) banned.Add(n);
        return new(phase, true, ReadTeam(root, "myTeam", localPlayerCellId), ReadTeam(root, "theirTeam", null))
        {
            BannedChampionIds = banned,
            SessionId = root.TryGetProperty("gameId", out var gameId) ? gameId.ToString() : "active"
        };
    }

    public async Task<PlayerSnapshot?> ReadPlayerSnapshotAsync(IReadOnlyList<Champion> catalog,
        IReadOnlySet<string> knownGameIds, CancellationToken cancellationToken = default)
    {
        if (_httpClient is null) return null;
        var currentJson = await OptionalGetAsync("lol-summoner/v1/current-summoner", cancellationToken);
        if (currentJson is null) return null;
        using var current = JsonDocument.Parse(currentJson);
        var puuid = LocalPlayerDataParser.Text(current.RootElement, "puuid");
        var summonerId = LocalPlayerDataParser.Number(current.RootElement, "summonerId");
        if (string.IsNullOrWhiteSpace(puuid)) return null;
        var sameAccount = string.Equals(_historyPlayerId, puuid, StringComparison.Ordinal);
        if (!sameAccount) _inspectedGames.Clear();
        var champions = catalog.Where(x => !x.IsEmpty && int.TryParse(x.Key, out _))
            .ToDictionary(x => int.Parse(x.Key, System.Globalization.CultureInfo.InvariantCulture), x => x.Id);
        // Independent optional endpoints: one unavailable source must not suppress the other.
        var historyTask = OptionalGetAsync($"lol-match-history/v1/products/lol/{Uri.EscapeDataString(puuid)}/matches?begIndex=0&endIndex=99", cancellationToken);
        var masteryTask = OptionalGetAsync("lol-champion-mastery/v1/local-player/champion-mastery", cancellationToken);
        await Task.WhenAll(historyTask, masteryTask);
        var historyPayload = await historyTask ?? await OptionalGetAsync(
            "lol-match-history/v1/products/lol/current-summoner/matches?begIndex=0&endIndex=99", cancellationToken);
        var matches = new List<PersonalMatch>();
        var historyAvailable = false;
        if (historyPayload is { } historyJson)
        {
            using var history = JsonDocument.Parse(historyJson);
            historyAvailable = history.RootElement.ValueKind == JsonValueKind.Object && history.RootElement.TryGetProperty("games", out _);
            var detailRequests = 0;
            foreach (var game in LocalPlayerDataParser.Games(history.RootElement))
            {
                var id = LocalPlayerDataParser.Number(game, "gameId");
                if (id <= 0) continue;
                // Game IDs are skipped only for this account. Account changes must backfill independently.
                var key = id.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (sameAccount && (knownGameIds.Contains(key) || _inspectedGames.Contains(key))) continue;
                var parsed = LocalPlayerDataParser.ParseMatch(game, puuid, summonerId, champions);
                var needsDetail = !game.TryGetProperty("participants", out var players) || players.ValueKind != JsonValueKind.Array || players.GetArrayLength() < 10;
                if (needsDetail && detailRequests++ < 12)
                {
                    var json = await OptionalGetAsync($"lol-match-history/v1/games/{id}", cancellationToken);
                    if (json is not null)
                    {
                        using var detail = JsonDocument.Parse(json);
                        parsed = LocalPlayerDataParser.ParseMatch(detail.RootElement, puuid, summonerId, champions) ?? parsed;
                        if (detail.RootElement.TryGetProperty("participants", out var detailPlayers) &&
                            detailPlayers.ValueKind == JsonValueKind.Array && detailPlayers.GetArrayLength() >= 10) _inspectedGames.Add(key);
                    }
                }
                else if (!needsDetail) _inspectedGames.Add(key);
                if (parsed is not null) matches.Add(parsed);
            }
        }
        var masteries = new List<ChampionMastery>();
        var masteryAvailable = false;
        if (await masteryTask is { } masteryJson)
        {
            using var document = JsonDocument.Parse(masteryJson);
            masteryAvailable = document.RootElement.ValueKind == JsonValueKind.Array;
            if (masteryAvailable)
                foreach (var entry in document.RootElement.EnumerateArray())
                    if (champions.TryGetValue((int)LocalPlayerDataParser.Number(entry, "championId"), out var champion))
                        masteries.Add(new(champion, (int)LocalPlayerDataParser.Number(entry, "championLevel"),
                            LocalPlayerDataParser.Number(entry, "championPoints")));
        }
        // Do not attach local-player mastery to a previous account if a login changed during sync.
        var identityJson = await OptionalGetAsync("lol-summoner/v1/current-summoner", cancellationToken);
        if (identityJson is null) return null;
        using var identity = JsonDocument.Parse(identityJson);
        if (LocalPlayerDataParser.Text(identity.RootElement, "puuid") != puuid) return null;
        _historyPlayerId = puuid;
        return new(puuid, matches, masteries, historyAvailable, masteryAvailable);
    }

    private string? _historyPlayerId;
    private readonly HashSet<string> _inspectedGames = [];
    private async Task<string?> OptionalGetAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient!.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var parsed = JsonDocument.Parse(json);
            return json;
        }
        catch (HttpRequestException) { return null; }
        catch (JsonException) { return null; }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { return null; }
    }

    private static (int Port, string Token)? FindProcessConnection()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT CommandLine FROM Win32_Process WHERE Name = 'LeagueClientUx.exe'");
        using var processes = searcher.Get();

        foreach (ManagementBaseObject process in processes)
        {
            using (process)
            {
                var commandLine = process["CommandLine"] as string;
                if (commandLine is null) continue;

                var portMatch = Regex.Match(commandLine, @"--app-port(?:=|\s+)(\d+)");
                var tokenMatch = Regex.Match(commandLine, @"--remoting-auth-token(?:=|\s+)(?:""([^""]+)""|([^\s""]+))");
                if (!portMatch.Success ||
                    !int.TryParse(portMatch.Groups[1].Value, out var port) ||
                    port is < 1 or > 65535 ||
                    !tokenMatch.Success)
                {
                    continue;
                }

                var token = tokenMatch.Groups[1].Success
                    ? tokenMatch.Groups[1].Value
                    : tokenMatch.Groups[2].Value;
                return (port, token);
            }
        }

        return null;
    }

    private static async Task<(int Port, string Token)?> ReadLockfileAsync(CancellationToken cancellationToken)
    {
        UnauthorizedAccessException? accessError = null;
        IOException? sharingError = null;
        foreach (var path in FindLockfiles())
        {
            if (!File.Exists(path)) continue;
            try
            {
                var contents = await File.ReadAllTextAsync(path, cancellationToken);
                var fields = contents.Trim().Split(':');
                if (fields.Length >= 5 &&
                    int.TryParse(fields[2], out var port) &&
                    port is >= 1 and <= 65535 &&
                    !string.IsNullOrWhiteSpace(fields[3]))
                {
                    return (port, fields[3]);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                accessError = ex;
            }
            catch (IOException ex)
            {
                if (!File.Exists(path))
                    continue;
                sharingError = ex;
            }
        }

        if (sharingError is not null)
            throw new IOException(
                "El lockfile está bloqueado y no fue posible leer los argumentos del proceso de LoL.",
                sharingError);
        if (accessError is not null)
            throw new UnauthorizedAccessException(
                "Windows denegó el acceso al lockfile del cliente. Abre Meta LoL con los mismos permisos que el cliente de LoL.",
                accessError);
        return null;
    }

    private void ConfigureClient(int port, string token)
    {
        var signature = $"{port}\0{token}";
        if (string.Equals(signature, _connectionSignature, StringComparison.Ordinal)) return;

        if (port is < 1 or > 65535 || string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidDataException("Los datos de conexión del cliente de LoL no son válidos.");
        }

        var handler = new HttpClientHandler
        {
            UseProxy = false,
            ServerCertificateCustomValidationCallback =
                (request, _, _, _) => request?.RequestUri?.IsLoopback == true
        };
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri($"https://127.0.0.1:{port}/"),
            Timeout = TimeSpan.FromSeconds(4)
        };
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"riot:{token}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        _httpClient?.Dispose();
        _httpClient = client;
        _connectionSignature = signature;
        _historyPlayerId = null;
    }

    public static IReadOnlyList<LeagueClientPick> ReadTeam(JsonElement root, string teamName, int? localPlayerCellId)
    {
        if (!root.TryGetProperty(teamName, out var team) || team.ValueKind != JsonValueKind.Array)
            return Array.Empty<LeagueClientPick>();

        var picks = new List<LeagueClientPick>();
        foreach (var player in team.EnumerateArray())
        {
            var championId = (int)LocalPlayerDataParser.Number(player, "championId");
            var isIntent = championId <= 0;
            if (isIntent) championId = (int)LocalPlayerDataParser.Number(player, "championPickIntent");
            if (championId <= 0) continue;

            var position = player.TryGetProperty("assignedPosition", out var positionElement)
                ? positionElement.GetString() ?? string.Empty
                : string.Empty;
            var cellId = player.TryGetProperty("cellId", out var cellElement) &&
                         cellElement.TryGetInt32(out var parsedCellId)
                ? parsedCellId
                : -1;
            var locked = false;
            if (root.TryGetProperty("actions", out var actions) && actions.ValueKind == JsonValueKind.Array)
                foreach (var group in actions.EnumerateArray())
                    foreach (var action in group.EnumerateArray())
                        if (LocalPlayerDataParser.Number(action, "actorCellId") == cellId &&
                            LocalPlayerDataParser.Text(action, "type") == "pick" &&
                            action.TryGetProperty("completed", out var completed) && completed.ValueKind == JsonValueKind.True)
                            locked = true;
            picks.Add(new(championId, position, cellId, localPlayerCellId == cellId) { IsLocked = locked && !isIntent });
        }

        return picks;
    }

    private static IEnumerable<string> FindLockfiles()
    {
        var candidates = new List<string>();
        foreach (var process in Process.GetProcessesByName("LeagueClientUx"))
        {
            using (process)
            {
                try
                {
                    var executable = process.MainModule?.FileName;
                    var directory = executable is null ? null : Path.GetDirectoryName(executable);
                    for (var depth = 0; directory is not null && depth < 4; depth++)
                    {
                        candidates.Add(Path.Combine(directory, "lockfile"));
                        directory = Directory.GetParent(directory)?.FullName;
                    }
                }
                catch (Win32Exception)
                {
                    // Try the standard install locations if Windows denies process inspection.
                }
                catch (UnauthorizedAccessException)
                {
                    // Try the standard install locations if Windows denies process inspection.
                }
                catch (InvalidOperationException)
                {
                    // The client may have exited while its process information was being read.
                }
            }
        }

        var driveRoot = Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\";
        candidates.Add(Path.Combine(driveRoot, "Riot Games", "League of Legends", "lockfile"));
        foreach (var programFiles in new[]
                 {
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
                 }.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            candidates.Add(Path.Combine(programFiles, "Riot Games", "League of Legends", "lockfile"));
        }

        return candidates.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public void Dispose() => _httpClient?.Dispose();
}

public sealed record LeagueClientPick(int ChampionId, string Position, int CellId, bool IsLocalPlayer)
{
    public bool IsLocked { get; init; }
}

public sealed record LeagueClientPollResult(
    string Phase,
    bool IsChampionSelect,
    IReadOnlyList<LeagueClientPick> AllyPicks,
    IReadOnlyList<LeagueClientPick> EnemyPicks)
{
    public IReadOnlySet<int> BannedChampionIds { get; init; } = new HashSet<int>();
    public string SessionId { get; init; } = "active";
}
