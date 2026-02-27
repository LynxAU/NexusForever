using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Expedition
{
    /// <summary>
    /// Boss script for Creature2Id 71037 — primary boss of Evil From The Ether.
    /// Obj 4925 KillTargetGroup, level 23 elite, faction 218.
    /// Note: 71039 is the level-50 scaling variant; add a separate boss script for it when needed.
    /// </summary>
    [ScriptFilterCreatureId(71037u)]
    public class EvilFromTheEtherBossScript : EncounterBossScript { }
}
