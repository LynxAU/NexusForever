using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Expedition
{
    /// <summary>
    /// Boss script for Katja Zarkhov  primary boss of Evil From The Ether.
    /// Creature2Id 71037 (level 23, normal) and 71039 (level 50, veteran).
    /// No Jabbithole spell data available  stub with death notification only.
    /// TODO: Add spell rotation when spell data is obtained from in-game testing.
    /// </summary>
    [ScriptFilterCreatureId(71037u)]
    public class EvilFromTheEtherBossScript : EncounterBossScript { }

    /// <summary>Katja Zarkhov  level 50 veteran variant. Creature2Id 71039.</summary>
    [ScriptFilterCreatureId(71039u)]
    public class EvilFromTheEtherBoss50Script : EncounterBossScript { }
}
