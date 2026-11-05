namespace MetaLol.Desktop;

public static class JungleChampionPool
{
    public static IReadOnlySet<string> Selected { get; } = new HashSet<string>(
        [
            "XinZhao",
            "Nocturne",
            "Volibear",
            "Warwick",
            "JarvanIV",
            "Jax",
            "Diana",
            "MasterYi",
            "MonkeyKing",
            "Evelynn",
            "Khazix",
            "Ekko",
            "Kayn",
            "Vi",
            "Viego"
        ],
        StringComparer.OrdinalIgnoreCase);
}
