using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Malgrave Trail adventure (WorldId 1181, internal "MalgraveTrail").
    ///
    /// NOTE: Boss creature IDs have not been identified. Adventure creatures in Creature2.tbl do
    /// not use bracket prefixes and could not be reliably matched to this world without sniff data.
    ///
    /// This script is a framework placeholder. Completion tracking will activate once the correct
    /// boss creature IDs are identified via in-game testing or retail sniff data.
    ///
    /// TODO: Identify and populate boss creature IDs for Malgrave Trail.
    /// </summary>
    [ScriptFilterOwnerId(1181)]
    public class MalgraveTrailScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        // TODO: Populate with correct boss creature IDs once identified.
        private static readonly HashSet<uint> BossCreatureIds = new()
        {
            // TODO: Add verified creature IDs
        };

        private IContentMapInstance owner;

        /// <inheritdoc/>
        public void OnLoad(IContentMapInstance owner)
        {
            this.owner = owner;
        }

        /// <inheritdoc/>
        public void OnBossDeath(uint creatureId)
        {
            // No-op until boss IDs are confirmed.
        }

        /// <inheritdoc/>
        public void OnEncounterReset() { }
    }
}
