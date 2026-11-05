using System.Text.Json;
using System.Net.Http;
using System.IO;

namespace MetaLol.Desktop;

public sealed class DataDragonService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(12) };
    private const string VersionsUrl = "https://ddragon.leagueoflegends.com/api/versions.json";

    public async Task<CatalogSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        AppDataPaths.EnsureCreated();
        string json;
        string version;
        var offline = false;

        try
        {
            var versionsJson = await Http.GetStringAsync(VersionsUrl, cancellationToken);
            version = JsonSerializer.Deserialize<string[]>(versionsJson)?.FirstOrDefault()
                ?? throw new InvalidOperationException("Data Dragon no devolvió una versión válida.");
            var catalogUrl = $"https://ddragon.leagueoflegends.com/cdn/{version}/data/es_ES/champion.json";
            json = await Http.GetStringAsync(catalogUrl, cancellationToken);
            await File.WriteAllTextAsync(AppDataPaths.ChampionCache, json, cancellationToken);
            await File.WriteAllTextAsync(AppDataPaths.VersionCache, version, cancellationToken);
        }
        catch when (File.Exists(AppDataPaths.ChampionCache))
        {
            json = await File.ReadAllTextAsync(AppDataPaths.ChampionCache, cancellationToken);
            version = File.Exists(AppDataPaths.VersionCache)
                ? (await File.ReadAllTextAsync(AppDataPaths.VersionCache, cancellationToken)).Trim()
                : ReadVersion(json);
            offline = true;
        }

        var champions = Parse(json, version);
        _ = WarmIconCacheAsync(champions, version, CancellationToken.None);
        return new(version, champions, offline, DateTimeOffset.Now);
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
            var roles = RoleCatalog.GetRoles(id, baseTags);
            var tacticalTags = RoleCatalog.GetTacticalTags(id, baseTags);
            var remoteIcon = $"https://ddragon.leagueoflegends.com/cdn/{version}/img/champion/{id}.png";
            var localIcon = Path.Combine(AppDataPaths.Cache, "icons", version, $"{id}.png");
            var image = File.Exists(localIcon) ? new Uri(localIcon).AbsoluteUri : remoteIcon;

            champions.Add(new Champion(
                id,
                value.GetProperty("key").GetString() ?? "0",
                value.GetProperty("name").GetString() ?? id,
                value.GetProperty("title").GetString() ?? string.Empty,
                image,
                tacticalTags,
                roles));
        }

        return champions.OrderBy(x => x.IsEmpty ? string.Empty : x.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static async Task WarmIconCacheAsync(IReadOnlyList<Champion> champions, string version, CancellationToken cancellationToken)
    {
        var iconDirectory = Path.Combine(AppDataPaths.Cache, "icons", version);
        Directory.CreateDirectory(iconDirectory);
        using var gate = new SemaphoreSlim(6);
        var tasks = champions.Where(x => !x.IsEmpty).Select(async champion =>
        {
            var path = Path.Combine(iconDirectory, $"{champion.Id}.png");
            if (File.Exists(path)) return;
            await gate.WaitAsync(cancellationToken);
            try
            {
                var bytes = await Http.GetByteArrayAsync(champion.ImageUrl, cancellationToken);
                await File.WriteAllBytesAsync(path, bytes, cancellationToken);
            }
            catch { /* El icono remoto seguirá disponible; el caché se reintentará después. */ }
            finally { gate.Release(); }
        });
        await Task.WhenAll(tasks);
    }

    private static string ReadVersion(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("version").GetString() ?? "unknown";
    }
}
