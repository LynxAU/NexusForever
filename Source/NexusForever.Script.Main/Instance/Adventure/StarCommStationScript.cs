using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Star-Comm Station adventure (WorldId 1437).
    ///
    /// Type: Combat  players fight through the Eldan relay station clearing
    /// automated defenses.
    ///
    /// Boss creature IDs could not be confirmed from Creature2.tbl data
    /// the original IDs (40353, 40365) mapped to Wilderrun quest objects.
    /// Uses FallbackRequiredBossKills so that any 2 EncounterBossScript deaths
    /// complete the adventure once correct creature IDs are added.
    ///
    /// TODO: Add boss scripts once creature IDs are confirmed via in-game testing.
    /// </summary>
    [ScriptFilterOwnerId(1437)]
    public class StarCommStationScript : AdventureScript
    {
        protected override int FallbackRequiredBossKills => 2;

        protected override void OnAdventureLoad()
        {
            // No explicit waves  awaiting confirmed creature IDs from in-game testing.
        }
    }
}
