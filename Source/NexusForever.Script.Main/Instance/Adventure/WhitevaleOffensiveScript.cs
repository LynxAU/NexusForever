using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Whitevale Offensive adventure (WorldId 1323).
    ///
    /// Type: Assault — players push through Whitevale Detention Center taking down
    /// Dominion command staff in sequential encounters.
    ///
    /// Creature IDs require in-game testing (no bracket prefix in Creature2.tbl).
    /// TODO: Identify officer/commander creature IDs and populate AddWave() calls.
    /// </summary>
    [ScriptFilterOwnerId(1323)]
    public class WhitevaleOffensiveScript : AdventureScript
    {
        protected override void OnAdventureLoad()
        {
            // TODO: Populate once creature IDs are confirmed via in-game testing.
            //   AddWave(officer1Id);    // First Dominion officer
            //   AddWave(officer2Id);    // Second officer
            //   AddWave(commanderId);   // Final commander
        }
    }
}
