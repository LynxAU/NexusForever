using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Levian Bay adventure (WorldId 3176).
    ///
    /// Type: Combat — players push through Levian Bay fighting Purewater zealots
    /// and Grimvoid operatives in sequential encounters.
    ///
    /// Potential creature IDs from [LBPCP] bracket in Creature2.tbl (92 entries);
    /// exact Levian Bay IDs require in-game testing to confirm WorldId mapping.
    /// TODO: Extract [LBPCP] creature IDs relevant to WorldId 3176 via testing.
    /// </summary>
    [ScriptFilterOwnerId(3176)]
    public class LevianBayAdventureScript : AdventureScript
    {
        protected override void OnAdventureLoad()
        {
            // TODO: Populate once creature IDs are confirmed.
            // Potential [LBPCP] candidates (WorldId 3176):
            //   AddWave(puewaterBossId);     // Purewater encounter boss
            //   AddWave(grimvoidBossId);     // Grimvoid encounter boss
            //   AddWave(finalBossId);        // Final confrontation
        }
    }
}
