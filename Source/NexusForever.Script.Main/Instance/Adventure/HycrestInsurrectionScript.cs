using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Hycrest Insurrection adventure (WorldId 1149).
    ///
    /// Type: Wave defense — players hold Hycrest Hamlet against multiple invasion waves
    /// culminating in a final commander fight.
    ///
    /// Creature IDs require in-game testing (no bracket prefix in Creature2.tbl).
    /// TODO: Populate AddWave() calls with verified creature IDs per wave once confirmed.
    /// </summary>
    [ScriptFilterOwnerId(1149)]
    public class HycrestInsurrectionScript : AdventureScript
    {
        protected override void OnAdventureLoad()
        {
            // TODO: Add waves once creature IDs are confirmed via in-game testing.
            // Expected structure (retail):
            //   AddWave(wave1BossId);      // Wave 1 commander
            //   AddWave(wave2BossId);      // Wave 2 commander
            //   AddWave(finalCommanderId); // Final boss
        }
    }
}
