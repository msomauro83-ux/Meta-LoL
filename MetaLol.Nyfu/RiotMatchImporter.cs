using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MetaLol.Desktop;

public sealed class RiotMatchImporter(DatabaseService database)
{
    public async Task<(int Imported, int Skipped)> ImportMatchIdsAsync(
        IEnumerable<string> matchIds,
        string apiKey,
        string route,
        string region,
        string tier,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("Se necesita una Riot API key.", nameof(apiKey));
        if (!apiKey.Trim().StartsWith("RGAPI-", StringComparison.Ordinal))
            throw new ArgumentException("La API key no tiene el formato RGAPI esperado. Cópiala nuevamente desde Riot Developer Portal.", nameof(apiKey));

        var normalizedIds = matchIds
            .Select(matchId => NormalizeMatchId(matchId, region))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        using var http = new HttpClient { BaseAddress = new Uri($"https://{route}.api.riotgames.com/") };
        http.DefaultRequestHeaders.Add("X-Riot-Token", apiKey.Trim());
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MetaLol", "0.2"));
        var imported = 0;
        var skipped = 0;

        foreach (var matchId in normalizedIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report($"Importando {matchId}...");

            using var response = await http.GetAsync(
                $"lol/match/v5/matches/{Uri.EscapeDataString(matchId)}",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                skipped++;
                progress?.Report($"{matchId}: partida no encontrada; revisa región y número.");
                continue;
            }

            if (!response.IsSuccessStatusCode)
                throw CreateApiException(response.StatusCode);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (await ImportMatchJsonAsync(json, region, tier)) imported++; else skipped++;
            await Task.Delay(70, cancellationToken);
        }

        return (imported, skipped);
    }

    public static string NormalizeMatchId(string matchId, string region)
    {
        var value = matchId.Trim();
        if (value.Length == 0) throw new ArgumentException("El Match ID está vacío.", nameof(matchId));

        if (Regex.IsMatch(value, @"^\d+$"))
        {
            var platform = region.ToUpperInvariant() switch
            {
                "LAS" => "LA2",
                "LAN" => "LA1",
                "NA" => "NA1",
                "EUW" => "EUW1",
                "KR" => "KR",
                _ => throw new ArgumentException(
                    $"No se puede deducir el servidor para la región {region}. Usa el ID completo, por ejemplo LA2_1629605898.",
                    nameof(matchId))
            };
            return $"{platform}_{value}";
        }

        if (!Regex.IsMatch(value, @"^[A-Za-z0-9]+_\d+$"))
            throw new ArgumentException(
                $"Match ID inválido: {value}. Usa solo el número o el formato completo, por ejemplo LA2_1629605898.",
                nameof(matchId));

        var separator = value.IndexOf('_');
        return $"{value[..separator].ToUpperInvariant()}{value[separator..]}";
    }

    private static Exception CreateApiException(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => new InvalidOperationException(
            "Riot rechazó la autenticación (401). Genera una API key nueva en developer.riotgames.com, cópiala completa y vuelve a intentarlo."),
        HttpStatusCode.Forbidden => new InvalidOperationException(
            "Riot rechazó la API key (403). La clave está vencida, revocada o no tiene acceso; las claves de desarrollo expiran cada 24 horas."),
        HttpStatusCode.BadRequest => new InvalidOperationException(
            "Riot rechazó el Match ID (400). Revisa el servidor y usa un ID como LA2_1629605898."),
        HttpStatusCode.TooManyRequests => new InvalidOperationException(
            "Riot alcanzó el límite de peticiones (429). Espera un momento antes de volver a importar."),
        _ => new HttpRequestException(
            $"Riot API respondió {(int)statusCode} ({statusCode}). Intenta nuevamente más tarde.",
            null,
            statusCode)
    };

    public async Task<bool> ImportMatchJsonAsync(string json, string region, string tier)
    {
        using var document = JsonDocument.Parse(json);
        var info = document.RootElement.GetProperty("info");
        var patch = info.GetProperty("gameVersion").GetString() ?? "unknown";
        var junglers = info.GetProperty("participants").EnumerateArray()
            .Where(x => string.Equals(x.GetProperty("teamPosition").GetString(), "JUNGLE", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(x.GetProperty("individualPosition").GetString(), "JUNGLE", StringComparison.OrdinalIgnoreCase))
            .Select(x => new
            {
                Champion = x.GetProperty("championName").GetString() ?? string.Empty,
                Won = x.GetProperty("win").GetBoolean(),
                Team = x.GetProperty("teamId").GetInt32()
            })
            .GroupBy(x => x.Team).Select(x => x.First()).Take(2).ToList();

        if (junglers.Count != 2 || junglers.Any(x => string.IsNullOrWhiteSpace(x.Champion))) return false;
        await database.RecordMatchAsync(patch, region, tier, junglers[0].Champion, junglers[0].Won, junglers[1].Champion);
        return true;
    }
}
