namespace MetaLol.Desktop;

public static class RoleCatalog
{
    private static readonly HashSet<string> Top = Set(
        "Aatrox Akali Ambessa Aurora Camille Chogath Darius DrMundo Fiora Gangplank Garen Gnar Gragas Gwen Illaoi Irelia Jax Jayce Kayle Kennen Kled KSante Malphite Mordekaiser Nasus Olaf Ornn Pantheon Poppy Quinn Renekton Rengar Riven Rumble Sett Shen Singed Sion Skarner TahmKench Teemo Trundle Tryndamere Udyr Urgot Vayne Vladimir Volibear Warwick MonkeyKing Yasuo Yone Yorick Zaahen");

    private static readonly HashSet<string> Jungle = Set(
        "Ambessa Amumu Belveth Brand Briar Chogath Diana DrMundo Ekko Elise Evelynn Fiddlesticks Gragas Graves Gwen Hecarim Ivern JarvanIV Jax Karthus Kayn Khazix Kindred LeeSin Lillia MasterYi Maokai Morgana Naafiri Nidalee Nocturne Nunu Olaf Pantheon Poppy Qiyana Rammus RekSai Rengar Sejuani Shaco Shyvana Skarner Sylas Taliyah Talon Teemo Trundle Tryndamere Udyr Vi Viego Volibear Warwick MonkeyKing XinZhao Zac Zed Zaahen Zyra");

    private static readonly HashSet<string> Mid = Set(
        "Ahri Akali Akshan Anivia Annie AurelionSol Aurora Azir Cassiopeia Corki Diana Ekko Fizz Galio Heimerdinger Hwei Irelia Kassadin Katarina Kennen Leblanc Lissandra Locke Lux Malzahar Mel Naafiri Neeko Orianna Qiyana Ryze Smolder Swain Sylas Syndra Taliyah Talon Tristana TwistedFate Veigar Velkoz Vex Viktor Vladimir Xerath Yasuo Yone Zed Ziggs Zoe");

    private static readonly HashSet<string> Adc = Set(
        "Aphelios Ashe Caitlyn Corki Draven Ezreal Jhin Jinx Kaisa Kalista KogMaw Lucian MissFortune Nilah Samira Senna Seraphine Sivir Smolder Tristana Twitch Varus Vayne Xayah Yunara Zeri Ziggs");

    private static readonly HashSet<string> Support = Set(
        "Alistar Annie Ashe Bard Blitzcrank Brand Braum Fiddlesticks Heimerdinger Hwei Ivern Janna Karma Leona Lulu Lux Maokai Mel Milio Morgana Nami Nautilus Neeko Poppy Pyke Rakan Rell Renata Senna Seraphine Shaco Sona Soraka Swain TahmKench Taric Thresh Velkoz Xerath Yuumi Zilean Zyra");

    private static readonly HashSet<string> ApDamage = Set(
        "Ahri Amumu Anivia Annie AurelionSol Aurora Azir Brand Cassiopeia Diana Ekko Elise Evelynn Fiddlesticks Fizz Galio Gragas Heimerdinger Hwei Ivern Karthus Kassadin Katarina Kennen Leblanc Lillia Lissandra Locke Lux Malphite Malzahar Maokai Mel Mordekaiser Morgana Neeko Nidalee Nunu Orianna Rammus Rumble Ryze Seraphine Shyvana Sona Swain Sylas Syndra Taliyah Teemo Veigar Velkoz Vex Viktor Vladimir Xerath Zac Ziggs Zilean Zoe Zyra");

    private static readonly HashSet<string> Engage = Set(
        "Alistar Amumu Briar Diana Fiddlesticks Galio Gnar Gragas Hecarim JarvanIV Leona Malphite Maokai Nautilus Neeko Nocturne Nunu Ornn Pantheon Rakan Rammus Rell RekSai Sejuani Sett Skarner Vi Volibear MonkeyKing Zac");

    private static readonly HashSet<string> AntiDive = Set("Gragas Ivern Janna Kindred Lissandra Milio Poppy Taliyah Thresh Vex");
    private static readonly HashSet<string> Peel = Set("Braum Ivern Janna Kindred Lulu Milio Nami Poppy Renata TahmKench Taric Thresh");
    private static readonly HashSet<string> Scaling = Set("AurelionSol Azir Belveth Gwen Jax Jinx Karthus Kayle Kindred KogMaw Lillia MasterYi Nasus Senna Shyvana Smolder Sona Veigar Viego Vladimir Yunara");
    private static readonly HashSet<string> Early = Set("Elise Graves JarvanIV LeeSin Nidalee Olaf Pantheon RekSai Rengar Shaco Udyr Volibear Warwick XinZhao");
    private static readonly HashSet<string> Duelists = Set("Belveth Briar Graves Gwen Jax Kindred MasterYi Olaf Trundle Udyr Viego Volibear Warwick XinZhao");
    private static readonly HashSet<string> AutoAttack = Set("Belveth Briar Graves Jax Kindred MasterYi Nocturne Olaf Rengar Shyvana Trundle Udyr Viego Volibear Warwick XinZhao MonkeyKing");
    private static readonly HashSet<string> BlockableDashes = Set("Ambessa Belveth Briar Diana Ekko Elise Gragas Graves Gwen Hecarim Ivern JarvanIV Jax Kayn Khazix Kindred LeeSin Maokai Nidalee Poppy RekSai Rengar Sejuani Sylas Talon Vi Viego Warwick MonkeyKing XinZhao Zac");
    private static readonly HashSet<string> SustainDamage = Set("Belveth Briar Graves Gwen Jax Karthus Kindred Lillia MasterYi Olaf Shyvana Trundle Udyr Viego Volibear Warwick XinZhao");
    private static readonly HashSet<string> AntiTank = Set("Gwen Lillia MasterYi Trundle");
    private static readonly HashSet<string> HardCc = Set("Aatrox Ahri Anivia Annie Ashe Azir Briar Camille Cassiopeia Chogath Darius Ekko Elise Evelynn Fiora Hwei Irelia Ivern Jax Jhin Jinx Kennen Leblanc Lissandra Lux Malzahar Morgana Pantheon Poppy Qiyana RekSai Renekton Rengar Riven Sejuani Shaco Shen Sion Skarner Swain Sylas Syndra Taliyah Taric Thresh Trundle TwistedFate Udyr Vayne Veigar Vex Viego Volibear Warwick MonkeyKing XinZhao Yasuo Yone Zac Zyra");
    private static readonly HashSet<string> Utility = Set("Alistar Braum Ivern Janna Leona Lulu Milio Nami Nautilus Rakan Rell Renata Sona Soraka Taric Thresh Yuumi Zilean");
    private static readonly HashSet<string> LaneEarly = Set("Caitlyn Darius Draven Jayce Lucian Pantheon Renekton");
    private static readonly HashSet<string> LaneDashes = Set("Ahri Akali Akshan Azir Camille Irelia Leblanc Lucian Nilah Riven Samira Vayne Yasuo Yone Zeri");
    private static readonly HashSet<string> Immobile = Set("Anivia Annie Aphelios Ashe Brand Cassiopeia Darius Jhin Jinx Karthus KogMaw Lux Malzahar MissFortune Orianna Senna Seraphine Sivir Smolder Soraka Swain Syndra Varus Veigar Velkoz Viktor Xerath Ziggs Zyra");

    public static IReadOnlyList<string> GetRoles(string id, IReadOnlyList<string> baseTags)
    {
        var roles = new List<string>();
        if (Top.Contains(id)) roles.Add("Top");
        if (Jungle.Contains(id)) roles.Add("Jungle");
        if (Mid.Contains(id)) roles.Add("Mid");
        if (Adc.Contains(id)) roles.Add("ADC");
        if (Support.Contains(id)) roles.Add("Support");
        if (roles.Count == 0)
        {
            if (baseTags.Contains("Marksman")) roles.Add("ADC");
            else if (baseTags.Contains("Support")) roles.Add("Support");
            else if (baseTags.Contains("Mage")) roles.Add("Mid");
            else roles.Add("Top");
        }
        return roles;
    }

    public static IReadOnlyList<string> GetTacticalTags(string id, IReadOnlyList<string> baseTags)
    {
        var tags = new HashSet<string>(baseTags, StringComparer.OrdinalIgnoreCase) { ApDamage.Contains(id) ? "AP" : "AD" };
        if (Engage.Contains(id)) tags.Add("Engage");
        if (Engage.Contains(id) || HardCc.Contains(id) || baseTags.Contains("Support") || baseTags.Contains("Tank")) tags.Add("CC");
        if (Utility.Contains(id)) tags.Add("Utility");
        if (AntiDive.Contains(id)) tags.Add("AntiDive");
        if (Peel.Contains(id)) tags.Add("Peel");
        if (Scaling.Contains(id)) tags.Add("Scaling");
        if (Early.Contains(id)) tags.Add("Early");
        if (LaneEarly.Contains(id)) tags.Add("Early");
        if (LaneDashes.Contains(id)) tags.Add("Dash");
        if (Immobile.Contains(id)) tags.Add("Immobile");
        if (id is "Caitlyn" or "Ashe" or "Senna") tags.Add("LongRange");
        if (id is "Vayne" or "Lucian" or "Sivir" or "Samira" or "Nilah") tags.Add("ShortRange");
        if (id is "Xayah" or "Sivir" or "Ezreal") tags.Add("SelfPeel");
        if (id is "Darius" or "Aatrox" or "Vladimir" or "Warwick" or "Nasus") tags.Add("Sustain");
        if (id is "Vayne" or "KogMaw" or "Varus") tags.Add("AntiTank");
        if (id is "Ahri" or "Leblanc") tags.Add("Pick");
        if (Duelists.Contains(id)) tags.Add("Duelist");
        if (AutoAttack.Contains(id)) tags.Add("AutoAttack");
        if (BlockableDashes.Contains(id)) tags.Add("Dash");
        if (SustainDamage.Contains(id)) tags.Add("Sustained");
        if (AntiTank.Contains(id)) tags.Add("AntiTank");
        if (baseTags.Contains("Tank")) tags.Add("Frontline");
        if (baseTags.Contains("Assassin")) { tags.Add("Dive"); tags.Add("Pick"); }
        if (baseTags.Contains("Marksman")) { tags.Add("Range"); tags.Add("AutoAttack"); tags.Add("Sustained"); }
        if (baseTags.Contains("Mage") && id is not ("Diana" or "Ekko" or "Evelynn" or "Fizz" or "Gwen" or "Kassadin" or "Katarina" or "Lillia" or "Mordekaiser" or "Rumble" or "Sylas"))
        { tags.Add("Poke"); tags.Add("Range"); }
        if (id == "Rammus") tags.Add("AntiAD");
        if (id is "Viego" or "MasterYi" or "Belveth") tags.Add("Reset");
        return tags.ToList();
    }

    private static HashSet<string> Set(string values) => values.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
}
