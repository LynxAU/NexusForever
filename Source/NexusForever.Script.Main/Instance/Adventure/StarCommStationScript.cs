using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Star-Comm Station adventure (WorldId 1437).
    ///
    /// Type: Combat — players fight through the Eldan relay station clearing
    /// automated defenses including SCS-72 Commander and SCS-83 Augmentor.
    ///
    /// Data source: local world DB spawns in
    /// WorldDatabaseRepo/Olyssia/Wilderrun.sql filtered to WorldId 1437 and EntityType 10.
    /// IDs are selected as high-confidence station boss candidates.
    /// </summary>
    [ScriptFilterOwnerId(1437)]
    public class StarCommStationScript : AdventureScript
    {
        private const uint SCS72CommanderCreatureId = 40353u;
        private const uint SCS83AugmentorCreatureId = 40365u;

        protected override void OnAdventureLoad()
        {
            AddWave(SCS72CommanderCreatureId);
            AddWave(SCS83AugmentorCreatureId);
        }
    }
}
