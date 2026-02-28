using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Farside adventure (WorldId 3010).
    ///
    /// Type: Combat — players fight through the Farside moon base against
    /// Eldan-constructed threats in sequential encounter rooms.
    ///
    /// Creature IDs require in-game testing (no bracket prefix in Creature2.tbl).
    /// TODO: Identify Farside encounter creature IDs and populate AddWave() calls.
    /// </summary>
    [ScriptFilterOwnerId(3010)]
    public class FarsideAdventureScript : AdventureScript
    {
        protected override void OnAdventureLoad()
        {
            // TODO: Populate once creature IDs are confirmed via in-game testing.
            //   AddWave(encounter1BossId);
            //   AddWave(encounter2BossId);
            //   AddWave(finalBossId);
        }
    }
}
