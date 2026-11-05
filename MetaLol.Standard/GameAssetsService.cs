using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MetaLol.Desktop;

public sealed class GameAssetsService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(12) };
    private IReadOnlyDictionary<string, GameItem> _items = new Dictionary<string, GameItem>();
    private IReadOnlyDictionary<int, RuneAsset> _runes = new Dictionary<int, RuneAsset>();
    private IReadOnlyDictionary<int, RuneStyleAsset> _runeStyles = new Dictionary<int, RuneStyleAsset>();

    public async Task LoadAsync(string version, CancellationToken cancellationToken = default)
    {
        var itemJson = await LoadOrCacheAsync(
            $"https://ddragon.leagueoflegends.com/cdn/{version}/data/es_ES/item.json",
            AppDataPaths.ItemCache, cancellationToken);
        var runeJson = await LoadOrCacheAsync(
            $"https://ddragon.leagueoflegends.com/cdn/{version}/data/es_ES/runesReforged.json",
            AppDataPaths.RuneCache, cancellationToken);
        _items = ParseItems(itemJson, version);
        _runes = ParseRunes(runeJson);
        _runeStyles = ParseRuneStyles(runeJson);
    }

    public BuildSuggestion Suggest(Champion champion)
    {
        var ids = champion.Has("Tank")
            ? new[] { "3068", "3075", "3143", "2504", "3111", "3047" }
            : champion.Has("Mage") || champion.Has("AP")
                ? new[] { "4633", "3152", "3116", "3089", "3135", "3020" }
                : champion.Has("Assassin")
                    ? new[] { "6692", "3142", "3814", "6694", "3111", "3047" }
                    : champion.Has("Marksman")
                        ? new[] { "6672", "3031", "3094", "3072", "3006", "3111" }
                        : new[] { "3071", "3053", "6333", "3748", "3111", "3047" };
        var items = ids.Where(_items.ContainsKey).Select(id => _items[id]).DistinctBy(x => x.Id).Take(6).ToList();

        var note = "Build adaptativa basada en el perfil del campeón y el catálogo del parche. Ajusta botas y defensas al daño rival; no representa win rate hasta importar partidas.";
        return new(items, SuggestRunePage(champion), note);
    }

    private RunePageSuggestion SuggestRunePage(Champion champion)
    {
        if (champion.Has("Tank"))
        {
            return Page(
                8400, [8439, 8463, 8429, 8451],
                8000, [9111, 9104],
                "Velocidad de habilidades", "StatModsCDRScalingIcon.png",
                "Vida progresiva", "StatModsHealthScalingIcon.png",
                "Vida progresiva", "StatModsHealthScalingIcon.png");
        }

        if (champion.Has("Peel"))
        {
            return Page(
                8200, [8214, 8275, 8210, 8232],
                8400, [8463, 8453],
                "Velocidad de habilidades", "StatModsCDRScalingIcon.png",
                "Fuerza adaptable", "StatModsAdaptiveForceIcon.png",
                "Vida progresiva", "StatModsHealthScalingIcon.png");
        }

        if (champion.Has("Assassin"))
        {
            return Page(
                8100, [8128, 8143, 8140, 8105],
                8000, [9111, 8014],
                "Fuerza adaptable", "StatModsAdaptiveForceIcon.png",
                "Fuerza adaptable", "StatModsAdaptiveForceIcon.png",
                "Vida progresiva", "StatModsHealthScalingIcon.png");
        }

        if (champion.Has("Marksman"))
        {
            return Page(
                8000, [8005, 9111, 9104, 8014],
                8100, [8143, 8135],
                "Velocidad de ataque", "StatModsAttackSpeedIcon.png",
                "Fuerza adaptable", "StatModsAdaptiveForceIcon.png",
                "Vida progresiva", "StatModsHealthScalingIcon.png");
        }

        if (champion.Has("Mage") || champion.Has("AP"))
        {
            return Page(
                8100, [8128, 8126, 8140, 8106],
                8200, [8210, 8232],
                "Fuerza adaptable", "StatModsAdaptiveForceIcon.png",
                "Fuerza adaptable", "StatModsAdaptiveForceIcon.png",
                "Vida progresiva", "StatModsHealthScalingIcon.png");
        }

        return Page(
            8000, [8010, 9111, 9104, 8299],
            8400, [8473, 8453],
            "Velocidad de ataque", "StatModsAttackSpeedIcon.png",
            "Fuerza adaptable", "StatModsAdaptiveForceIcon.png",
            "Vida progresiva", "StatModsHealthScalingIcon.png");
    }

    private RunePageSuggestion Page(
        int primaryStyle,
        int[] primaryRunes,
        int secondaryStyle,
        int[] secondaryRunes,
        string offenseName,
        string offenseIcon,
        string flexName,
        string flexIcon,
        string defenseName,
        string defenseIcon) =>
        new(
            Path(primaryStyle, primaryRunes, true),
            Path(secondaryStyle, secondaryRunes, false),
            [
                Shard("OFENSIVA", offenseName, offenseIcon),
                Shard("FLEXIBLE", flexName, flexIcon),
                Shard("DEFENSA", defenseName, defenseIcon)
            ]);

    private RunePathSuggestion Path(int styleId, IReadOnlyList<int> runeIds, bool primary)
    {
        _runeStyles.TryGetValue(styleId, out var style);
        var presentation = StylePresentation(styleId);
        var selections = runeIds
            .Where(_runes.ContainsKey)
            .Select((id, index) => new RuneSelection(
                primary && index == 0 ? "RUNA CLAVE" : $"ELECCIÓN {(primary ? index : index + 1)}",
                _runes[id]))
            .ToList();

        return new(
            styleId,
            style?.Name ?? presentation.FallbackName,
            presentation.ColorName,
            presentation.Accent,
            style?.ImageUrl ?? string.Empty,
            selections);
    }

    private static StatShard Shard(string step, string name, string icon) =>
        new(step, name, $"https://ddragon.leagueoflegends.com/cdn/img/perk-images/StatMods/{icon}");

    private static (string FallbackName, string ColorName, string Accent) StylePresentation(int styleId) => styleId switch
    {
        8100 => ("Dominación", "ROJAS", "#E05A5A"),
        8300 => ("Inspiración", "CELESTES", "#65C7D0"),
        8000 => ("Precisión", "AMARILLAS", "#D8B653"),
        8400 => ("Valor", "VERDES", "#63BE78"),
        8200 => ("Brujería", "AZULES", "#8796FF"),
        _ => ("Runas", "NEUTRAS", "#62E5FF")
    };

    private static async Task<string> LoadOrCacheAsync(string url, string cachePath, CancellationToken cancellationToken)
    {
        try
        {
            var json = await Http.GetStringAsync(url, cancellationToken);
            await File.WriteAllTextAsync(cachePath, json, cancellationToken);
            return json;
        }
        catch when (File.Exists(cachePath))
        {
            return await File.ReadAllTextAsync(cachePath, cancellationToken);
        }
    }

    private static IReadOnlyDictionary<string, GameItem> ParseItems(string json, string version)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("data").EnumerateObject().ToDictionary(
            property => property.Name,
            property =>
            {
                var item = property.Value;
                var description = item.TryGetProperty("description", out var raw) ? StripHtml(raw.GetString() ?? string.Empty) : string.Empty;
                return new GameItem(property.Name, item.GetProperty("name").GetString() ?? property.Name,
                    $"https://ddragon.leagueoflegends.com/cdn/{version}/img/item/{property.Name}.png", description);
            });
    }

    private static IReadOnlyDictionary<int, RuneAsset> ParseRunes(string json)
    {
        using var document = JsonDocument.Parse(json);
        var result = new Dictionary<int, RuneAsset>();
        foreach (var style in document.RootElement.EnumerateArray())
        foreach (var slot in style.GetProperty("slots").EnumerateArray())
        foreach (var rune in slot.GetProperty("runes").EnumerateArray())
        {
            var id = rune.GetProperty("id").GetInt32();
            var icon = rune.GetProperty("icon").GetString() ?? string.Empty;
            result[id] = new(id, rune.GetProperty("name").GetString() ?? id.ToString(), $"https://ddragon.leagueoflegends.com/cdn/img/{icon}");
        }
        return result;
    }

    private static IReadOnlyDictionary<int, RuneStyleAsset> ParseRuneStyles(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateArray().ToDictionary(
            style => style.GetProperty("id").GetInt32(),
            style =>
            {
                var id = style.GetProperty("id").GetInt32();
                var icon = style.GetProperty("icon").GetString() ?? string.Empty;
                return new RuneStyleAsset(
                    id,
                    style.GetProperty("name").GetString() ?? id.ToString(),
                    $"https://ddragon.leagueoflegends.com/cdn/img/{icon}");
            });
    }

    private static string StripHtml(string value) => Regex.Replace(value, "<.*?>", string.Empty).Replace("&nbsp;", " ").Trim();

    private sealed record RuneStyleAsset(int Id, string Name, string ImageUrl);
}
