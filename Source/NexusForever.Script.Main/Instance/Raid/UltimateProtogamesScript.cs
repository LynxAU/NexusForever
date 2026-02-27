using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Raid
{
    /// <summary>
    /// Map script for Ultimate Protogames raid (WorldId 3041, internal "UltiProtogames").
    ///
    /// NOTE: Boss creature IDs have not been verified. Creature2.tbl "[UP]" name tag search
    /// (Ub3r-Proto / Ultimate Protogames) found two encounter zones:
    ///   e2675 — Hut-Hut encounter
    ///   e2680 — Vending Machine encounter
    ///
    /// Specific creature IDs for these encounters have not been extracted.
    /// This script is a framework placeholder; completion tracking will activate once
    /// the correct creature IDs are identified via in-game testing or retail sniff data.
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Raid/Ultimate Protogames.sql (stub — no coordinate data).
    /// TODO: Extract [UP] encounter creature IDs and populate BossCreatureIds.
    /// </summary>
    [ScriptFilterOwnerId(3041)]
    public class UltimateProtogamesScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        // TODO: Populate with correct boss creature IDs once identified.
        // [UP] e2675 — Hut-Hut encounter (creature ID unknown)
        // [UP] e2680 — Vending Machine encounter (creature ID unknown)
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
