using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Northern Wilds adventure (WorldId 1393).
    ///
    /// Type: Rescue/combat — players rescue colonists and fight through Northern Wilds
    /// threat encounters, ending with a final confrontation.
    ///
    /// Data source: local world DB spawns in
    /// WorldDatabaseRepo/Olyssia/Auroria.sql filtered to WorldId 1393 and EntityType 10.
    /// IDs are selected as encounter anchors and should be validated against retail ordering.
    /// </summary>
    [ScriptFilterOwnerId(1393)]
    public class NorthernWildsAdventureScript : AdventureScript
    {
        private const uint ThreatEncounter1CreatureId = 27568u;
        private const uint ThreatEncounter2CreatureId = 39332u;
        private const uint FinalEncounterCreatureId = 29638u;

        protected override void OnAdventureLoad()
        {
            AddWave(ThreatEncounter1CreatureId);
            AddWave(ThreatEncounter2CreatureId);
            AddWave(FinalEncounterCreatureId);
        }
    }
}
