using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Star-Comm Station adventure (WorldId 1437, internal "StarCommStation").
    ///
    /// NOTE: Boss creature IDs have not been identified. Creature2.tbl "[LBPCP]" entries
    /// (Levian Bay Player-Controlled Phase, 92 entries) reference "Star-Comm Station" bosses
    /// including SCS-72 Commander and SCS-83 Augmentor, but specific creature IDs for those
    /// entries were not extracted in the available search results.
    ///
    /// This script is a framework placeholder. Completion tracking will activate once the correct
    /// boss creature IDs are identified via in-game testing or retail sniff data.
    ///
    /// TODO: Extract [LBPCP] creature IDs for SCS-72 Commander and SCS-83 Augmentor.
    /// </summary>
    [ScriptFilterOwnerId(1437)]
    public class StarCommStationScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        // TODO: Populate with correct boss creature IDs once identified.
        // Candidates: [LBPCP] SCS-72 Commander, [LBPCP] SCS-83 Augmentor (creature IDs unknown).
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
