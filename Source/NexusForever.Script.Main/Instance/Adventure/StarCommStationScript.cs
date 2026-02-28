using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Star-Comm Station adventure (WorldId 1437).
    ///
    /// Type: Combat — players fight through the Eldan relay station clearing
    /// automated defenses including SCS-72 Commander and SCS-83 Augmentor.
    ///
    /// Potential creature IDs from [LBPCP] bracket in Creature2.tbl (92 entries);
    /// specific IDs for SCS-72/SCS-83 require in-game testing to confirm.
    /// TODO: Extract [LBPCP] creature IDs and populate AddWave() calls.
    /// </summary>
    [ScriptFilterOwnerId(1437)]
    public class StarCommStationScript : AdventureScript
    {
        protected override void OnAdventureLoad()
        {
            // TODO: Populate once creature IDs are confirmed.
            // Candidates from [LBPCP] Creature2.tbl entries:
            //   AddWave(scs72CommanderId);   // SCS-72 Commander
            //   AddWave(scs83AugmentorId);   // SCS-83 Augmentor (final)
        }
    }
}
