using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Malgrave Trail adventure (WorldId 1181).
    ///
    /// Type: Survival/resource management — navigate the Malgrave desert managing
    /// supplies while fending off threat encounters and bandit ambushes.
    ///
    /// Creature IDs require in-game testing (no bracket prefix in Creature2.tbl).
    /// TODO: Identify boss/threat creature IDs and populate AddWave() calls.
    /// </summary>
    [ScriptFilterOwnerId(1181)]
    public class MalgraveTrailScript : AdventureScript
    {
        protected override void OnAdventureLoad()
        {
            // TODO: Populate once creature IDs are confirmed via in-game testing.
            //   AddWave(banditAmbushBossId);   // Mid-trail ambush
            //   AddWave(finalAmbushBossId);    // End boss
        }
    }
}
