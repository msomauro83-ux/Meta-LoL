using System.Text.Json;
using MetaLol.Desktop;

namespace MetaLol.Mobile;

public static class CatalogParser
{
    public static CatalogSnapshot Parse(string json, bool offline)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var version = root.GetProperty("version").GetString() ?? throw new InvalidDataException("Catálogo sin versión.");
        var champions = new List<Champion> { Champion.Empty };
        foreach (var property in root.GetProperty("data").EnumerateObject())
        {
            var value = property.Value;
            var id = value.GetProperty("id").GetString() ?? property.Name;
            var tags = value.GetProperty("tags").EnumerateArray().Select(x => x.GetString()!).ToList();
            champions.Add(new(id, value.GetProperty("key").GetString() ?? "0",
                value.GetProperty("name").GetString() ?? id, value.GetProperty("title").GetString() ?? "",
                $"https://ddragon.leagueoflegends.com/cdn/{version}/img/champion/{id}.png",
                RoleCatalog.GetTacticalTags(id, tags), RoleCatalog.GetRoles(id, tags)));
        }
        if (champions.Count < 2) throw new InvalidDataException("Catálogo vacío.");
        return new(version, champions.OrderBy(c => c.IsEmpty ? "" : c.Name, StringComparer.CurrentCultureIgnoreCase).ToList(), offline, DateTimeOffset.Now);
    }
}
