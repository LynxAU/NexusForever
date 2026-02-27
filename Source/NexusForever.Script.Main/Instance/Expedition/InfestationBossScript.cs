using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Expedition
{
    /// <summary>
    /// Boss encounter script for Creature2Id 69871 — the final boss of the Infestation expedition.
    /// On death this notifies the InfestationScript (bound to the containing ContentMapInstance)
    /// via <see cref="NexusForever.Game.Abstract.Map.Instance.IContentMapInstance.TriggerBossDeath"/>.
    /// </summary>
    [ScriptFilterCreatureId(69871u)]
    public class InfestationBossScript : EncounterBossScript
    {
    }
}
