using System.Text.Json;
using MetaLol.Desktop;

namespace MetaLol.Mobile;

public sealed class MobileCatalogService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };
    private static string CachePath => Path.Combine(FileSystem.AppDataDirectory, "champions-es_ES.json");

    public async Task<CatalogSnapshot> LoadLocalAsync()
    {
        if (File.Exists(CachePath))
        {
            try { return CatalogParser.Parse(await File.ReadAllTextAsync(CachePath), true); }
            catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or KeyNotFoundException) { }
        }
        // A complete bundled catalog also makes the very first launch work offline.
        using var stream = await FileSystem.OpenAppPackageFileAsync("champions-es_ES.json");
        using var reader = new StreamReader(stream);
        return CatalogParser.Parse(await reader.ReadToEndAsync(), true);
    }

    public async Task<CatalogSnapshot> RefreshAsync()
    {
        var versions = JsonSerializer.Deserialize<string[]>(await Http.GetStringAsync("https://ddragon.leagueoflegends.com/api/versions.json"));
        var version = versions?.FirstOrDefault() ?? throw new InvalidDataException("No se recibió un parche válido.");
        var json = await Http.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{version}/data/es_ES/champion.json");
        var catalog = CatalogParser.Parse(json, false);
        // Validate before replacing the cache; the JSON carries its own matching version.
        var temporary = CachePath + ".tmp";
        await File.WriteAllTextAsync(temporary, json);
        File.Move(temporary, CachePath, true);
        return catalog;
    }
}
