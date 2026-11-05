namespace MetaLol.Desktop;

public static class EditionProfile
{
    public const string DisplayName = "Meta LoL Estándar";
    public const string Title = "Meta LoL Estándar · Multilínea";
    public const int RecommendationLimit = 5;
    public static IReadOnlyList<string> Roles { get; } = ["Top", "Jungle", "Mid", "ADC", "Support"];
    public static string? NormalizeRole(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "top" => "Top", "jungle" => "Jungle", "mid" or "middle" => "Mid",
        "adc" or "bot" or "bottom" => "ADC", "support" or "utility" => "Support", _ => null
    };
    public static string RoleLabel(string role) => role switch
    {
        "Jungle" => "Jungla", "Mid" => "Mid", "ADC" => "ADC", "Support" => "Soporte", "Top" => "Top", _ => "Sin confirmar"
    };
}
