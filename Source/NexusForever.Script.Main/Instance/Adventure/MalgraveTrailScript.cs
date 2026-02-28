using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Malgrave Trail adventure (WorldId 1181).
    ///
    /// Type: Survival/resource management — navigate the Malgrave desert managing
    /// supplies while fending off threat encounters and bandit ambushes.
    ///
    /// Data source: local world DB spawns in
    /// WorldDatabaseRepo/Alizar/Whitevale.sql filtered to WorldId 1181 and EntityType 10.
    /// IDs are selected as high-confidence encounter candidates and should be tuned with
    /// in-game verification.
    /// </summary>
    [ScriptFilterOwnerId(1181)]
    public class MalgraveTrailScript : AdventureScript
    {
        private const uint AmbushCommanderCreatureId = 23042u;
        private const uint MidTrailThreatCreatureId = 26293u;
        private const uint FinalCommanderA = 34645u;
        private const uint FinalCommanderB = 34646u;

        protected override void OnAdventureLoad()
        {
            AddWave(AmbushCommanderCreatureId);               // Early ambush commander
            AddWave(MidTrailThreatCreatureId);                // Mid-trail threat
            AddWave(FinalCommanderA, FinalCommanderB);        // Final paired commanders
        }
    }
}
