using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    /// <summary>
    /// Map script for Hall of the Hundred dungeon (WorldId 3009, internal "HalloftheHundred").
    ///
    /// NOTE: Boss creature IDs for this dungeon could not be reliably extracted from the
    /// PublicEvent objective data. The directly referenced creature IDs (12162, 12262, 14048, 14128)
    /// resolve to creature variant templates and quest NPCs — not actual boss creatures.
    /// The TargetGroup-based objectives (TG 12157, 14275) were not resolved.
    ///
    /// This script is a framework placeholder. Completion tracking will activate once
    /// the correct boss creature IDs are identified via retail sniff data or in-game testing.
    ///
    /// PublicEvent IDs for this world: 666 (main), 677, 678, 693, 696, 874, 875.
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Dungeon/Hall of the Hundred.sql
    /// This script uses objective-derived fallback IDs so dungeon completion can progress
    /// while deeper TargetGroup resolution is still pending.
    /// </summary>
    [ScriptFilterOwnerId(3009)]
    public class HallOfTheHundredScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        // Objective-derived fallback candidates from PublicEvent 666:
        //   14128 — resolves to a quest NPC (incorrect)
        //   12162 — resolves to a creature variant template (incorrect)
        //   14048 — not found in Creature2.tbl
        //   12262 — resolves to a creature variant template (incorrect)
        //   TG 12157 and TG 14275 target groups are still unresolved.
        //
        // Pragmatic fallback: finish on first qualifying kill so the map can complete
        // until authoritative encounter IDs are wired.
        private static readonly HashSet<uint> BossCreatureIds = new()
        {
            12162u,
            12262u,
            14048u,
            14128u
        };

        private IContentMapInstance owner;
        private bool completed;

        /// <inheritdoc/>
        public void OnLoad(IContentMapInstance owner)
        {
            this.owner = owner;
            completed = false;
        }

        /// <inheritdoc/>
        public void OnBossDeath(uint creatureId)
        {
            if (completed || !BossCreatureIds.Contains(creatureId))
                return;

            completed = true;
            if (owner.Match != null)
                owner.Match.MatchFinish();
            else
                owner.OnMatchFinish();
        }

        /// <inheritdoc/>
        public void OnEncounterReset()
        {
            completed = false;
        }
    }
}
