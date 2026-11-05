namespace MetaLol.Desktop;

public sealed class DraftAnalyzer
{
    private readonly IReadOnlyList<Champion> _champions;
    private readonly IReadOnlyDictionary<(string Jungler, string Opponent), MatchupStat> _matchups;
    private readonly IReadOnlySet<string>? _junglePool;
    private readonly IReadOnlyDictionary<string, PersonalChampionStat> _history;
    private readonly IReadOnlyDictionary<string, ChampionMastery> _mastery;

    public DraftAnalyzer(IReadOnlyList<Champion> champions, IEnumerable<MatchupStat>? matchups = null,
        IReadOnlySet<string>? junglePool = null, IEnumerable<PersonalChampionStat>? history = null,
        IEnumerable<ChampionMastery>? mastery = null)
    {
        _champions = champions;
        _junglePool = junglePool;
        _matchups = (matchups ?? []).Where(x => x.Games > 0 && x.Wins >= 0 && x.Wins <= x.Games)
            .GroupBy(x => (x.JunglerId, x.OpponentId))
            .ToDictionary(x => x.Key, x => x.OrderByDescending(s => s.Games).First());
        _history = (history ?? []).ToDictionary(x => x.ChampionId);
        _mastery = (mastery ?? []).ToDictionary(x => x.ChampionId);
    }

    public IReadOnlyList<Champion> Champions => _champions;
    public IReadOnlyList<Champion> ForRole(string role) => _champions
        .Where(c => c.IsEmpty || c.Roles.Contains(role, StringComparer.OrdinalIgnoreCase)).ToList();

    public AnalysisResult Analyze(IReadOnlyList<Champion> enemies, IReadOnlyList<Champion> allies, Champion? enemyJungler,
        IReadOnlySet<string>? unavailable = null)
    {
        enemies = enemies.Where(x => !x.IsEmpty).DistinctBy(x => x.Id).ToList();
        allies = allies.Where(x => !x.IsEmpty).DistinctBy(x => x.Id).ToList();
        if (enemies.Count == 0) return new("Aún no hay campeones rivales seleccionados.", [], [], false);
        if (enemyJungler is { IsEmpty: true } || !enemies.Any(x => x.Id == enemyJungler?.Id)) enemyJungler = null;
        var enemy = ReadProfile(enemies);
        var ally = ReadProfile(allies);
        var occupied = enemies.Concat(allies).Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var recommendations = ForRole("Jungle")
            .Where(x => !x.IsEmpty && !occupied.Contains(x.Id) && unavailable?.Contains(x.Id) != true &&
                (_junglePool is null || _junglePool.Contains(x.Id)))
            .Select(x => Score(x, enemy, ally, allies.Count, enemyJungler))
            .OrderByDescending(x => x.Score).ThenByDescending(x => x.HistoricalWinRate)
            .ThenByDescending(x => x.MasteryLevel).ThenByDescending(x => x.MasteryPoints)
            .ThenBy(x => x.Name, StringComparer.Ordinal).ToList();
        var priorities = new List<string>
        {
            enemyJungler is null
                ? "Jungla rival sin confirmar: no se inventa un counter. Asigna su línea cuando sea conocida."
                : $"Prioridad: matchup contra {enemyJungler.Name}; después resiliencia aliada y composición rival.",
            "Score de encaje sobre 100, no probabilidad de victoria. Empates: tu WR histórico y luego maestría."
        };
        if (allies.Count > 0 && ally.Frontline == 0) priorities.Add("Tu equipo no tiene frontline: un jungla resistente mejora la estructura.");
        if (enemy.Ad >= 3) priorities.Add("Mucho daño físico rival: armadura y anti-AD ganan valor.");
        if (enemy.Dive >= 2) priorities.Add("Varias entradas rivales: protege a tus carries y conserva el control para el contraataque.");
        if (enemy.Frontline >= 2) priorities.Add("Varias frontline: valora daño sostenido y evita gastar todo sobre el primer tanque.");
        var summary = $"{enemies.Count}/5 rivales · {allies.Count}/4 aliados. Rival: {enemy.Ad} AD, {enemy.Ap} AP, {enemy.Frontline} frontline. " +
            (enemyJungler is null ? "Matchup directo pendiente." : $"Jungla confirmado: {enemyJungler.Name}.");
        return new(summary, priorities.Take(4).ToList(), recommendations, recommendations.Any(x => x.HasRealStats));
    }

    private Recommendation Score(Champion c, TeamProfile enemy, TeamProfile ally, int allyCount, Champion? opponent)
    {
        var reasons = new List<string>();
        var risks = new List<string>();
        double enemyFit = 0, allyFit = 0;
        void Enemy(bool condition, double points, string text)
        { if (condition) { enemyFit += points; (points > 0 ? reasons : risks).Add(text); } }
        void Ally(bool condition, double points, string text)
        { if (condition) { allyFit += points; (points > 0 ? reasons : risks).Add(text); } }
        Enemy(enemy.Ad >= 3 && c.Has("AntiAD"), 22, "castiga el daño físico rival");
        Enemy(enemy.Ap >= 3 && c.Has("AntiAD"), -14, "su defensa contra ataques físicos pierde valor frente a tanto AP");
        Enemy(enemy.Dive >= 2 && c.Has("AntiDive"), 18, "frena las entradas rivales");
        Enemy(enemy.Peel >= 2 && (c.Has("Assassin") || c.Has("Dive")), -14, "el peel rival dificulta alcanzar a sus carries");
        Enemy(enemy.Frontline == 0 && (c.Has("Engage") || c.Has("Pick")), enemy.Count >= 3 ? 13 : 5,
            enemy.Count >= 3 ? "explota una composición rival frágil" : "puede presionar al objetivo frágil revelado");
        Enemy(enemy.Frontline >= 2 && (c.Has("Sustained") || c.Has("AntiTank")), 14, "aporta daño sostenido frente a la frontline rival");
        Enemy(enemy.Frontline >= 2 && c.Has("Assassin") && !c.Has("Sustained"), -13, "dos frontline limitan el acceso y el burst");
        Enemy(enemy.Cc >= 3 && c.Has("Reset"), -16, "el CC rival puede cortar tus resets antes de la primera baja");
        Enemy(enemy.Engage >= 2 && c.Has("Peel"), 12, "aporta contra-iniciación");
        Enemy(enemy.Range >= 3 && c.Has("Tank") && !c.Has("Engage"), -10, "pueden desgastarte sin que tengas una entrada fiable");
        if (allyCount > 0)
        {
            var majority = Math.Max(2, allyCount - 1);
            Ally(ally.Frontline == 0 && (c.Has("Tank") || c.Has("Frontline")), 22, "completa la frontline aliada");
            Ally(allyCount >= 3 && ally.Frontline == 0 && !c.Has("Tank") && !c.Has("Frontline"), -17, "tu equipo sigue sin primera línea");
            Ally(ally.Engage == 0 && c.Has("Engage"), 12, "añade iniciación que falta en tu equipo");
            Ally(ally.Cc == 0 && c.Has("CC"), 10, "incorpora control de masas al equipo");
            Ally(ally.Ad >= majority && c.Has("AP"), 15, "equilibra el daño aliado con AP");
            Ally(ally.Ap >= majority && c.Has("AD"), 15, "equilibra el daño aliado con AD");
            Ally(ally.Ad >= 3 && c.Has("AD"), -12, "acumular más AD facilita que el rival compre armadura");
            Ally(ally.Ap >= 3 && c.Has("AP"), -12, "acumular más AP facilita la resistencia mágica rival");
            Ally(ally.Range >= 2 && (c.Has("Peel") || c.Has("Tank")), 9, "crea espacio y protección para los carries de rango");
            Ally(ally.Dive >= 2 && (c.Has("Dive") || c.Has("Engage")), 8, "sincroniza con la entrada del equipo");
            Ally(ally.Peel >= 1 && c.Has("Scaling"), 7, "aprovecha la protección aliada para escalar");
            Ally(ally.Frontline >= 2 && (c.Has("Assassin") || c.Has("Marksman") || c.Has("Sustained")), 8, "puede ocupar el rol de carry con frontline aliada");
            Ally(ally.Scaling >= 2 && c.Has("Scaling") && enemy.Early >= 1, -10, "varias líneas necesitan escalar y el rival puede acelerar la partida");
        }
        var direct = DirectMatchup(c, opponent);
        MatchupStat? stat = null;
        var matchup = direct.Value;
        var matchupReason = direct.Reason;
        if (opponent is not null && _matchups.TryGetValue((c.Id, opponent.Id), out var found))
        {
            if (found.Games >= 30)
            {
                stat = found;
                var empirical = 50 + 100 * ((found.Wins + 15d) / (found.Games + 30d) - 0.5);
                var weight = 0.75 * found.Games / (found.Games + 60d);
                matchup = matchup * (1 - weight) + empirical * weight;
                matchupReason += $" Muestra importada: {found.WinRate:P1} WR en {found.Games} partidas; ajuste por tamaño de muestra.";
            }
            else matchupReason += $" Solo {found.Games} partidas importadas: muestra insuficiente (mínimo 30).";
        }
        _history.TryGetValue(c.Id, out var history);
        _mastery.TryGetValue(c.Id, out var mastery);
        var personal = history is { Games: > 0 }
            ? 50 + 80 * (history.AdjustedWinRate - 0.5) +
                20 * ((history.RecentWins + 4d) / (history.RecentGames + 8d) - 0.5) : 50;
        if (opponent is not null && history is { MatchupGames: > 0 })
        {
            matchup += 12 * ((history.MatchupWins + 6d) / (history.MatchupGames + 12d) - 0.5);
            matchupReason += $" Tu matchup: {history.MatchupWins}/{history.MatchupGames} victorias (regularizado).";
        }
        var weights = opponent is null ? (J: 0d, A: .50, E: .35, H: .15) : (J: .45, A: .30, E: .15, H: .10);
        if (allyCount == 0) weights = opponent is null ? (0, 0, .80, .20) : (.65, 0, .25, .10);
        var allied = Fit(allyFit);
        var enemyScore = Fit(enemyFit);
        var score = Math.Round(weights.J * matchup + weights.A * allied + weights.E * enemyScore + weights.H * personal, 2);
        if (opponent is not null && matchup < 45) risks.Add($"matchup directo desfavorable frente a {opponent.Name}: evita forzar el duelo");
        var statLine = stat is null ? "Sin muestra estadística fiable" : $"{stat.WinRate:P1} WR · {stat.Games:N0} partidas importadas";
        var personalLine = history is { Games: > 0 }
            ? $"Tu WR: {history.WinRate:P1} · {history.Wins}/{history.Games} · últimas {history.RecentGames}: {history.RecentWins} V"
            : "Sin partidas personales de jungla registradas";
        personalLine += mastery is null ? " · maestría pendiente" : $" · M{mastery.Level} · {mastery.Points:N0} pts";
        if (history is { Games: > 0 and < 12 }) risks.Add("tu historial aún tiene pocas partidas: su influencia está reducida");
        if (reasons.Count == 0) reasons.Add("perfil flexible; no se detecta una ventaja clara en las composiciones reveladas");
        if (risks.Count == 0) risks.Add("no se detecta una desventaja estructural clara; revisa la prioridad de líneas antes de disputar río");
        var profile = string.Join(" · ", c.Tags.Where(x => x is "Tank" or "Fighter" or "Mage" or "Assassin" or "Marksman" or "Engage" or "AntiDive" or "Peel").Distinct().Take(3));
        return new(c.Id, c.Name, c.ImageUrl, score, profile, string.Join("; ", reasons) + ".", statLine, stat is not null)
        {
            HistoricalWinRate = history is { Games: > 0 } ? history.WinRate : -1, HistoryGames = history?.Games ?? 0,
            MasteryLevel = mastery?.Level ?? 0, MasteryPoints = mastery?.Points ?? 0, PersonalLine = personalLine,
            MatchupReason = matchupReason,
            Breakdown = $"Jungla {matchup:F1} × {weights.J:P0} · Aliados {allied:F1} × {weights.A:P0} · Rival {enemyScore:F1} × {weights.E:P0} · Historial {personal:F1} × {weights.H:P0}",
            Risk = string.Join("; ", risks) + ".",
            Plan = direct.Plan + (ally.Frontline == 0 && allyCount > 0 && (c.Has("Tank") || c.Has("Frontline"))
                ? " En peleas, absorbe la primera entrada sin separarte de tus carries."
                : " Convierte la prioridad de líneas en visión y objetivos; no fuerces río sin apoyo.")
        };
    }

    private static double Fit(double value) => 50 + 45 * Math.Tanh(value / 40);

    private static (double Value, string Reason, string Plan) DirectMatchup(Champion c, Champion? o)
    {
        if (o is null) return (50, "Jungla rival sin confirmar; counter directo pendiente.", "Evita comprometer tu ruta hasta conocer al jungla rival.");
        double value = 50;
        var strengths = new List<string>();
        var weaknesses = new List<string>();
        void Rule(bool condition, double points, string text)
        { if (condition) { value += points; (points > 0 ? strengths : weaknesses).Add(text); } }
        Rule(c.Has("AntiAD") && o.Has("AutoAttack"), 17, "tu defensa castiga sus ataques básicos");
        Rule(o.Has("AntiAD") && c.Has("AutoAttack"), -17, "sus defensas castigan tus ataques básicos; evita pegar durante su defensa activa");
        Rule(c.Id == "Jax" && o.Has("AutoAttack"), 15, "tu E esquiva sus ataques básicos y puede cerrar el intercambio con aturdimiento");
        Rule(c.Has("AntiAD") && o.Has("AP"), -12, "su daño mágico evita buena parte de tu especialización anti-AD");
        Rule(c.Id == "Poppy" && o.Has("Dash"), 18, "tu W puede interrumpir su desplazamiento y su entrada");
        Rule(o.Id == "Poppy" && c.Has("Dash"), -18, "su W puede cortar tu desplazamiento de entrada");
        Rule(c.Has("AntiDive") && o.Has("Dive"), 11, "puedes responder a su entrada con contra-iniciación");
        Rule(c.Has("Early") && o.Has("Scaling") && !o.Has("Early"), 10, "tienes herramientas para presionar antes de que escale");
        Rule(c.Has("Scaling") && !c.Has("Early") && o.Has("Early"), -12, "puede presionarte en los primeros niveles");
        Rule(c.Has("Duelist") && !o.Has("Duelist") && !o.Has("Tank"), 7, "tu perfil favorece escaramuzas aisladas");
        Rule(o.Has("Duelist") && c.Has("Tank") && !c.Has("Duelist"), -7, "su daño de duelo castiga peleas largas sin aliados");
        Rule(c.Has("AntiTank") && (o.Has("Tank") || o.Has("Frontline")), 13, "tu daño o robo de resistencias castiga su resistencia");
        Rule(c.Has("Reset") && (o.Has("CC") || o.Has("Peel")), -10, "puede guardar control o protección para negar tu primera baja");
        Rule(c.Has("CC") && o.Has("Reset"), 10, "puedes reservar control para cortar su cadena de bajas");
        Rule(c.Has("Assassin") && o.Has("Tank"), -9, "su resistencia reduce el valor de tu burst en el duelo");
        var reason = $"Contra {o.Name} (lectura de kit, no WR medido): " +
            (strengths.Count == 0 ? "sin ventaja directa clara" : string.Join("; ", strengths)) + ".";
        if (weaknesses.Count > 0) reason += " Riesgo: " + string.Join("; ", weaknesses) + ".";
        var plan = value >= 58 ? $"Presiona a {o.Name} cuando tus líneas puedan moverse; tu ventaja de kit no garantiza ganar el 1v1."
            : value <= 43 ? $"Evita disputar a {o.Name} sin apoyo; busca intercambios en el lado opuesto y protege tus entradas."
            : $"Contra {o.Name}, disputa con ventaja de nivel, vida y prioridad; prepara el contra-gank.";
        if (c.Id == "Jax") plan += " Reserva E para sus ataques y usa Q con un objetivo de salida; evita volver a entrar sin E.";
        if (c.Id == "Nocturne") plan += " Reserva el escudo de hechizos para su control y usa R con seguimiento aliado.";
        if (c.Id == "Viego") plan += " Espera una primera baja alcanzable; conserva una salida antes del primer reset.";
        if (c.Id == "JarvanIV") plan += " Coordina E-Q con el control de tu línea y revisa qué rivales pueden salir de tu R.";
        return (Math.Clamp(value, 15, 85), reason, plan);
    }

    private static TeamProfile ReadProfile(IReadOnlyList<Champion> team) => new(team.Count,
        team.Count(x => x.Has("AD") && !x.Has("Utility")), team.Count(x => x.Has("AP") && !x.Has("Utility")), team.Count(x => x.Has("Dive")),
        team.Count(x => x.Has("Engage")), team.Count(x => x.Has("CC")), team.Count(x => x.Has("Frontline") || x.Has("Tank")),
        team.Count(x => x.Has("Range")), team.Count(x => x.Has("Peel")), team.Count(x => x.Has("Scaling")), team.Count(x => x.Has("Early")));
    private sealed record TeamProfile(int Count, int Ad, int Ap, int Dive, int Engage, int Cc, int Frontline,
        int Range, int Peel, int Scaling, int Early);
}
