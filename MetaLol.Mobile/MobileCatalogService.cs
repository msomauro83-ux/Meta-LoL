using System.Text.Json;
using MetaLol.Desktop;

namespace MetaLol.Mobile;

public sealed class MobileCatalogService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private const string VersionsUrl = "https://ddragon.leagueoflegends.com/api/versions.json";

    private static string CacheDirectory => Path.Combine(FileSystem.AppDataDirectory, "cache");
    private static string ChampionCache => Path.Combine(CacheDirectory, "champions-es_ES.json");
    private static string VersionCache => Path.Combine(CacheDirectory, "version.txt");

    public async Task<CatalogSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(CacheDirectory);
        string json;
        string version;
        var offline = false;

        try
        {
            var versionsJson = await Http.GetStringAsync(VersionsUrl, cancellationToken);
            version = JsonSerializer.Deserialize<string[]>(versionsJson)?.FirstOrDefault()
                ?? throw new InvalidOperationException("Data Dragon no devolvió una versión válida.");
            json = await Http.GetStringAsync(
                $"https://ddragon.leagueoflegends.com/cdn/{version}/data/es_ES/champion.json",
                cancellationToken);
            await File.WriteAllTextAsync(ChampionCache, json, cancellationToken);
            await File.WriteAllTextAsync(VersionCache, version, cancellationToken);
        }
        catch when (File.Exists(ChampionCache))
        {
            json = await File.ReadAllTextAsync(ChampionCache, cancellationToken);
            version = File.Exists(VersionCache)
                ? (await File.ReadAllTextAsync(VersionCache, cancellationToken)).Trim()
                : ReadVersion(json);
            offline = true;
        }

        return new CatalogSnapshot(version, Parse(json, version), offline, DateTimeOffset.Now);
    }

    private static IReadOnlyList<Champion> Parse(string json, string version)
    {
        using var document = JsonDocument.Parse(json);
        var champions = new List<Champion> { Champion.Empty };
        foreach (var property in document.RootElement.GetProperty("data").EnumerateObject())
        {
            var value = property.Value;
            var id = value.GetProperty("id").GetString() ?? property.Name;
            var baseTags = value.GetProperty("tags").EnumerateArray()
                .Select(x => x.GetString()).Where(x => x is not null).Cast<string>().ToList();
            champions.Add(new Champion(
                id,
                value.GetProperty("key").GetString() ?? "0",
                value.GetProperty("name").GetString() ?? id,
                value.GetProperty("title").GetString() ?? string.Empty,
                $"https://ddragon.leagueoflegends.com/cdn/{version}/img/champion/{id}.png",
                RoleCatalog.GetTacticalTags(id, baseTags),
                RoleCatalog.GetRoles(id, baseTags)));
        }

        return champions.OrderBy(x => x.IsEmpty ? string.Empty : x.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static string ReadVersion(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("version").GetString() ?? "unknown";
    }
}
