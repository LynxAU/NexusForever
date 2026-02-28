using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Galeras Holdout adventure (WorldId 1233).
    ///
    /// Type: Wave defense — players hold a fortified position in Galeras against
    /// Dominion assault waves culminating in a commander fight.
    ///
    /// Creature IDs require in-game testing (no bracket prefix in Creature2.tbl).
    /// TODO: Identify wave boss creature IDs and populate AddWave() calls.
    /// </summary>
    [ScriptFilterOwnerId(1233)]
    public class GalerasHoldoutScript : AdventureScript
    {
        protected override void OnAdventureLoad()
        {
            // TODO: Populate once creature IDs are confirmed via in-game testing.
            //   AddWave(wave1CommanderId);    // First assault wave commander
            //   AddWave(wave2CommanderId);    // Second wave
            //   AddWave(finalCommanderId);    // Final commander
        }
    }
}
