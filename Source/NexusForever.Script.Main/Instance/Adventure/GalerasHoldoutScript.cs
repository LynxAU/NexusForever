using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Galeras Holdout adventure (WorldId 1233).
    ///
    /// Type: Wave defense — players hold a fortified position in Galeras against
    /// Dominion assault waves culminating in a commander fight.
    ///
    /// Data source: local world DB spawns in
    /// WorldDatabaseRepo/Olyssia/Auroria.sql filtered to WorldId 1233 and EntityType 10.
    /// IDs are selected as likely command-tier encounters and should be tuned from
    /// live parity playtesting.
    /// </summary>
    [ScriptFilterOwnerId(1233)]
    public class GalerasHoldoutScript : AdventureScript
    {
        private const uint Wave1CommanderCreatureId = 12909u;
        private const uint Wave2CommanderCreatureId = 33904u;
        private const uint FinalCommanderCreatureId = 39445u;

        protected override void OnAdventureLoad()
        {
            AddWave(Wave1CommanderCreatureId);
            AddWave(Wave2CommanderCreatureId);
            AddWave(FinalCommanderCreatureId);
        }
    }
}
