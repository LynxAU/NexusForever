using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Levian Bay adventure (WorldId 3176, internal "LevianBayAdventure").
    ///
    /// NOTE: Boss creature IDs have not been identified. Creature2.tbl "[LBPCP]" entries
    /// (Levian Bay Player-Controlled Phase, 92 entries) may cover content in this adventure
    /// world, but specific creature IDs were not extracted in the available search results.
    ///
    /// This script is a framework placeholder. Completion tracking will activate once the correct
    /// boss creature IDs are identified via in-game testing or retail sniff data.
    ///
    /// TODO: Extract [LBPCP] creature IDs relevant to Levian Bay adventure (WorldId 3176).
    /// </summary>
    [ScriptFilterOwnerId(3176)]
    public class LevianBayAdventureScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        // TODO: Populate with correct boss creature IDs once identified.
        // Candidates: [LBPCP] entries (Purewater, Grimvoid encounters — creature IDs unknown).
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
