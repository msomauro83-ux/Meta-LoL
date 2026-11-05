using System.Globalization;
using System.Text;
using MetaLol.Desktop;

namespace MetaLol.Mobile;

// Contains no Android dependencies so manual draft behavior can be tested directly.
public sealed class ManualDraft
{
    public string Role { get; private set; } = "Mid";
    private readonly Dictionary<string, Champion> _enemies = EditionProfile.Roles.ToDictionary(r => r, _ => Champion.Empty);
    private readonly Dictionary<string, Champion> _allies = EditionProfile.Roles.ToDictionary(r => r, _ => Champion.Empty);
    public Champion GetPick(string role, bool enemy) => (enemy ? _enemies : _allies)[role];
    public IReadOnlyList<Champion> Enemies => _enemies.Values.Where(c => !c.IsEmpty).ToList();
    public IReadOnlyList<Champion> Allies => _allies.Where(p => p.Key != Role).Select(p => p.Value).Where(c => !c.IsEmpty).ToList();
    public Champion? Opponent => _enemies[Role].IsEmpty ? null : _enemies[Role];

    public void ChangeRole(string role)
    {
        if (!EditionProfile.Roles.Contains(role)) throw new ArgumentException("Rol no reconocido.", nameof(role));
        Role = role;
        // A selection in our new slot no longer represents a teammate.
        _allies[role] = Champion.Empty;
    }

    public bool IsAvailable(Champion champion, string role, bool enemy) => champion.IsEmpty ||
        !_enemies.Any(p => !(enemy && p.Key == role) && p.Value.Id == champion.Id) &&
        !_allies.Any(p => !(!enemy && p.Key == role) && p.Value.Id == champion.Id);

    public bool TrySetPick(string role, bool enemy, Champion champion)
    {
        if (!EditionProfile.Roles.Contains(role)) throw new ArgumentException("Rol no reconocido.", nameof(role));
        if ((!enemy && role == Role) || !IsAvailable(champion, role, enemy)) return false;
        (enemy ? _enemies : _allies)[role] = champion;
        return true;
    }

    public void Clear()
    {
        foreach (var role in EditionProfile.Roles) { _enemies[role] = Champion.Empty; _allies[role] = Champion.Empty; }
    }

    public AnalysisResult Analyze(DraftAnalyzer analyzer) => analyzer.Analyze(Enemies, Allies, Opponent, role: Role);

    public static IReadOnlyList<Champion> Search(IEnumerable<Champion> catalog, string? query)
    {
        var term = Normalize(query ?? "");
        return catalog.Where(c => c.IsEmpty || Normalize(c.Name).Contains(term) || Normalize(c.Id).Contains(term)).ToList();
    }

    private static string Normalize(string text) => new string(text.Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c))
        .Select(char.ToLowerInvariant).ToArray());
}
