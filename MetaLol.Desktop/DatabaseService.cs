using System.Text.Json;
using System.IO;
using Microsoft.Data.Sqlite;

namespace MetaLol.Desktop;

public sealed class DatabaseService(string? databasePath = null)
{
    private static bool _sqliteInitialized;
    private string ConnectionString => new SqliteConnectionStringBuilder { DataSource = databasePath ?? AppDataPaths.Database }.ToString();

    public async Task InitializeAsync()
    {
        if (!_sqliteInitialized)
        {
            SQLitePCL.Batteries_V2.Init();
            _sqliteInitialized = true;
        }
        if (databasePath is null) AppDataPaths.EnsureCreated();
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS champions (
                id TEXT PRIMARY KEY, champion_key TEXT NOT NULL, name TEXT NOT NULL, title TEXT NOT NULL,
                image_url TEXT NOT NULL, tags_json TEXT NOT NULL, roles_json TEXT NOT NULL, updated_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS matchup_stats (
                patch TEXT NOT NULL, region TEXT NOT NULL, tier TEXT NOT NULL,
                jungler_id TEXT NOT NULL, opponent_id TEXT NOT NULL,
                games INTEGER NOT NULL DEFAULT 0, wins INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (patch, region, tier, jungler_id, opponent_id)
            );
            CREATE TABLE IF NOT EXISTS build_stats (
                patch TEXT NOT NULL, region TEXT NOT NULL, tier TEXT NOT NULL, jungler_id TEXT NOT NULL,
                item_ids TEXT NOT NULL, rune_ids TEXT NOT NULL, games INTEGER NOT NULL DEFAULT 0,
                wins INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (patch, region, tier, jungler_id, item_ids, rune_ids)
            );
            CREATE TABLE IF NOT EXISTS settings (key TEXT PRIMARY KEY, value TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS personal_matches (
                player_id TEXT NOT NULL, game_id TEXT NOT NULL, champion_id TEXT NOT NULL,
                opponent_id TEXT NOT NULL, won INTEGER NOT NULL CHECK(won IN (0,1)),
                played_at INTEGER NOT NULL, queue_id INTEGER NOT NULL,
                PRIMARY KEY(player_id,game_id)
            );
            CREATE TABLE IF NOT EXISTS champion_mastery (
                player_id TEXT NOT NULL, champion_id TEXT NOT NULL, level INTEGER NOT NULL,
                points INTEGER NOT NULL, PRIMARY KEY(player_id,champion_id)
            );
            CREATE INDEX IF NOT EXISTS ix_personal_champion ON personal_matches(player_id,champion_id,played_at DESC);
            CREATE INDEX IF NOT EXISTS ix_matchup_lookup ON matchup_stats(patch, region, tier, opponent_id);
            """;
        await command.ExecuteNonQueryAsync();
    }

    public async Task SaveChampionsAsync(IEnumerable<Champion> champions)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        foreach (var champion in champions.Where(x => !x.IsEmpty))
        {
            var command = connection.CreateCommand();
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText = """
                INSERT INTO champions(id, champion_key, name, title, image_url, tags_json, roles_json, updated_at)
                VALUES($id,$key,$name,$title,$image,$tags,$roles,$updated)
                ON CONFLICT(id) DO UPDATE SET champion_key=excluded.champion_key, name=excluded.name,
                    title=excluded.title, image_url=excluded.image_url, tags_json=excluded.tags_json,
                    roles_json=excluded.roles_json, updated_at=excluded.updated_at;
                """;
            command.Parameters.AddWithValue("$id", champion.Id);
            command.Parameters.AddWithValue("$key", champion.Key);
            command.Parameters.AddWithValue("$name", champion.Name);
            command.Parameters.AddWithValue("$title", champion.Title);
            command.Parameters.AddWithValue("$image", champion.ImageUrl);
            command.Parameters.AddWithValue("$tags", JsonSerializer.Serialize(champion.Tags));
            command.Parameters.AddWithValue("$roles", JsonSerializer.Serialize(champion.Roles));
            command.Parameters.AddWithValue("$updated", DateTimeOffset.UtcNow.ToString("O"));
            await command.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
    }

    public async Task<IReadOnlyList<MatchupStat>> LoadMatchupsAsync(string patch, string region, string tier)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT patch, region, tier, jungler_id, opponent_id, games, wins
            FROM matchup_stats WHERE patch=$patch AND region=$region AND tier=$tier;
            """;
        command.Parameters.AddWithValue("$patch", NormalizePatch(patch));
        command.Parameters.AddWithValue("$region", region);
        command.Parameters.AddWithValue("$tier", tier);
        var result = new List<MatchupStat>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetInt32(5), reader.GetInt32(6)));
        return result;
    }

    public async Task<int> GetMatchupCountAsync()
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(SUM(games),0) FROM matchup_stats";
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task RecordMatchAsync(string patch, string region, string tier, string firstJungler, bool firstWon, string secondJungler)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await UpsertMatchupAsync(connection, (SqliteTransaction)transaction, NormalizePatch(patch), region, tier, firstJungler, secondJungler, firstWon);
        await UpsertMatchupAsync(connection, (SqliteTransaction)transaction, NormalizePatch(patch), region, tier, secondJungler, firstJungler, !firstWon);
        await transaction.CommitAsync();
    }

    public async Task<int> ImportCountersCsvAsync(string path)
    {
        var imported = 0;
        foreach (var line in (await File.ReadAllLinesAsync(path)).Skip(1))
        {
            var cells = line.Split(',').Select(x => x.Trim()).ToArray();
            if (cells.Length < 7 || !int.TryParse(cells[5], out var games) || !int.TryParse(cells[6], out var wins) || games <= 0 || wins < 0 || wins > games) continue;
            await using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO matchup_stats(patch,region,tier,jungler_id,opponent_id,games,wins)
                VALUES($patch,$region,$tier,$jungler,$opponent,$games,$wins)
                ON CONFLICT(patch,region,tier,jungler_id,opponent_id)
                DO UPDATE SET games=excluded.games,wins=excluded.wins;
                """;
            command.Parameters.AddWithValue("$patch", NormalizePatch(cells[0]));
            command.Parameters.AddWithValue("$region", cells[1]);
            command.Parameters.AddWithValue("$tier", cells[2]);
            command.Parameters.AddWithValue("$jungler", cells[3]);
            command.Parameters.AddWithValue("$opponent", cells[4]);
            command.Parameters.AddWithValue("$games", games);
            command.Parameters.AddWithValue("$wins", wins);
            imported += await command.ExecuteNonQueryAsync() > 0 ? 1 : 0;
        }
        return imported;
    }

    private static async Task UpsertMatchupAsync(SqliteConnection connection, SqliteTransaction transaction, string patch, string region, string tier, string jungler, string opponent, bool won)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO matchup_stats(patch,region,tier,jungler_id,opponent_id,games,wins)
            VALUES($patch,$region,$tier,$jungler,$opponent,1,$won)
            ON CONFLICT(patch,region,tier,jungler_id,opponent_id)
            DO UPDATE SET games=games+1,wins=wins+$won;
            """;
        command.Parameters.AddWithValue("$patch", patch);
        command.Parameters.AddWithValue("$region", region);
        command.Parameters.AddWithValue("$tier", tier);
        command.Parameters.AddWithValue("$jungler", jungler);
        command.Parameters.AddWithValue("$opponent", opponent);
        command.Parameters.AddWithValue("$won", won ? 1 : 0);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<string?> GetActivePlayerAsync()
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM settings WHERE key='active_player'";
        return await command.ExecuteScalarAsync() as string;
    }

    public async Task SavePlayerSnapshotAsync(PlayerSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.PlayerId)) return;
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();
        async Task Execute(string sql, params (string Key, object Value)[] parameters)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$player", snapshot.PlayerId);
            foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Key, parameter.Value);
            await command.ExecuteNonQueryAsync();
        }
        foreach (var match in snapshot.Matches)
            await Execute("""
                INSERT INTO personal_matches(player_id,game_id,champion_id,opponent_id,won,played_at,queue_id)
                VALUES($player,$game,$champion,$opponent,$won,$time,$queue)
                ON CONFLICT(player_id,game_id) DO UPDATE SET opponent_id=excluded.opponent_id
                WHERE personal_matches.opponent_id='' AND excluded.opponent_id<>'';
                """, ("$game", match.GameId), ("$champion", match.ChampionId), ("$opponent", match.OpponentId),
                ("$won", match.Won ? 1 : 0), ("$time", match.PlayedAt), ("$queue", match.QueueId));
        if (snapshot.MasteryAvailable)
        {
            await Execute("DELETE FROM champion_mastery WHERE player_id=$player");
            foreach (var mastery in snapshot.Masteries)
                await Execute("INSERT INTO champion_mastery(player_id,champion_id,level,points) VALUES($player,$champion,$level,$points)",
                    ("$champion", mastery.ChampionId), ("$level", mastery.Level), ("$points", mastery.Points));
        }
        await Execute("INSERT INTO settings(key,value) VALUES('active_player',$player) ON CONFLICT(key) DO UPDATE SET value=excluded.value");
        await transaction.CommitAsync();
    }

    public async Task<IReadOnlyList<PersonalChampionStat>> LoadPersonalStatsAsync(string? playerId, string? opponentId = null)
    {
        if (playerId is null) return [];
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = """
            WITH ranked AS (
                SELECT *, ROW_NUMBER() OVER(PARTITION BY champion_id ORDER BY played_at DESC,game_id DESC) AS recent_rank
                FROM personal_matches WHERE player_id=$player
            )
            SELECT champion_id, COUNT(*), SUM(won),
                SUM(CASE WHEN recent_rank<=20 THEN 1 ELSE 0 END),
                SUM(CASE WHEN recent_rank<=20 THEN won ELSE 0 END),
                SUM(CASE WHEN opponent_id=$opponent THEN 1 ELSE 0 END),
                SUM(CASE WHEN opponent_id=$opponent THEN won ELSE 0 END)
            FROM ranked GROUP BY champion_id;
            """;
        command.Parameters.AddWithValue("$player", playerId);
        command.Parameters.AddWithValue("$opponent", opponentId ?? "");
        var result = new List<PersonalChampionStat>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(new(reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2),
            reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5), reader.GetInt32(6)));
        return result;
    }

    public async Task<IReadOnlyList<ChampionMastery>> LoadMasteriesAsync(string? playerId)
    {
        if (playerId is null) return [];
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT champion_id,level,points FROM champion_mastery WHERE player_id=$player";
        command.Parameters.AddWithValue("$player", playerId);
        var result = new List<ChampionMastery>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(new(reader.GetString(0), reader.GetInt32(1), reader.GetInt64(2)));
        return result;
    }

    public async Task<IReadOnlyList<PersonalMatch>> LoadPersonalMatchesAsync(string? playerId)
    {
        if (playerId is null) return [];
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT game_id,champion_id,opponent_id,won,played_at,queue_id FROM personal_matches WHERE player_id=$player";
        command.Parameters.AddWithValue("$player", playerId);
        var result = new List<PersonalMatch>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2),
            reader.GetInt32(3) != 0, reader.GetInt64(4), reader.GetInt32(5)));
        return result;
    }

    public static string NormalizePatch(string version)
    {
        var parts = version.Split('.');
        return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : version;
    }
}
