using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Hycrest Insurrection adventure (WorldId 1149).
    ///
    /// Type: Wave defense — players hold Hycrest Hamlet against multiple invasion waves
    /// culminating in a final commander fight.
    ///
    /// Creature IDs for this map are still being verified; until then this script uses
    /// fallback completion after three unique boss deaths.
    /// </summary>
    [ScriptFilterOwnerId(1149)]
    public class HycrestInsurrectionScript : AdventureScript
    {
        protected override int FallbackRequiredBossKills => 3;

        protected override void OnAdventureLoad() { }
    }
}
