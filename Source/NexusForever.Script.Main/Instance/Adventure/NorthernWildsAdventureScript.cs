using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Northern Wilds adventure (WorldId 1393).
    ///
    /// Type: Rescue/combat — players rescue colonists and fight through Northern Wilds
    /// threat encounters, ending with a final confrontation.
    ///
    /// Creature IDs require in-game testing (no bracket prefix in Creature2.tbl).
    /// TODO: Identify threat creature IDs and populate AddWave() calls.
    /// </summary>
    [ScriptFilterOwnerId(1393)]
    public class NorthernWildsAdventureScript : AdventureScript
    {
        protected override void OnAdventureLoad()
        {
            // TODO: Populate once creature IDs are confirmed via in-game testing.
            //   AddWave(threat1BossId);    // First threat encounter
            //   AddWave(threat2BossId);    // Second encounter
            //   AddWave(finalBossId);      // Final boss
        }
    }
}
