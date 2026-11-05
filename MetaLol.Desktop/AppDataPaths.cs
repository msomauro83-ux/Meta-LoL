using System.IO;

namespace MetaLol.Desktop;

public static class AppDataPaths
{
    public static string Root { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MetaLol");
    public static string Cache { get; } = Path.Combine(Root, "cache");
    public static string Database { get; } = Path.Combine(Root, "meta-lol.db");
    public static string ChampionCache { get; } = Path.Combine(Cache, "champions-es_ES.json");
    public static string ItemCache { get; } = Path.Combine(Cache, "items-es_ES.json");
    public static string RuneCache { get; } = Path.Combine(Cache, "runes-es_ES.json");
    public static string VersionCache { get; } = Path.Combine(Cache, "version.txt");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Cache);
    }
}
