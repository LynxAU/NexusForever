using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Raid
{
    /// <summary>
    /// Map script for Augmentors' Lab raid (WorldId 3040, internal "AugmentorsLab").
    ///
    /// Completion condition: main bosses of the Augmentors encounter (e2681) must be defeated.
    ///   Augmenters God Unit        — Creature2Id 50979 (main final boss)
    ///   Prime Evolutionary Operant — Creature2Id 50472
    ///   Phaged Evolutionary Operant — Creature2Id 50423
    ///   Chestacabra                — Creature2Id 50425
    ///   Circuit Breaker            — Creature2Id 61597
    ///
    /// Source: Creature2.tbl "[IC]" (Infinite Crimelabs) name tag search.
    /// "[IC]" prefix covers WorldId 3040 Augmentors' Lab content (distinct from
    /// Infinite Labs dungeon at WorldId 2980 which has no Creature2 tag).
    ///
    /// Note: Confirm RequiredBossCount and which creatures are required vs optional.
    /// Spawn data: see WorldDatabaseRepo/Instance/Raid/Augmentors Lab.sql (stub — no coordinate data).
    /// </summary>
    [ScriptFilterOwnerId(3040)]
    public class AugmentorsLabScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        private static readonly HashSet<uint> BossCreatureIds = new()
        {
            50979u,  // Augmenters God Unit (final boss)
            50472u,  // Prime Evolutionary Operant
            50423u,  // Phaged Evolutionary Operant
            50425u,  // Chestacabra
            61597u,  // Circuit Breaker
        };

        // Note: Verify — may only require GodUnit death for completion.
        private const int RequiredBossCount = 5;

        private IContentMapInstance owner;
        private readonly HashSet<uint> defeatedBosses = new();

        /// <inheritdoc/>
        public void OnLoad(IContentMapInstance owner)
        {
            this.owner = owner;
            defeatedBosses.Clear();
        }

        /// <inheritdoc/>
        public void OnBossDeath(uint creatureId)
        {
            if (!BossCreatureIds.Contains(creatureId) || !defeatedBosses.Add(creatureId))
                return;

            if (defeatedBosses.Count < RequiredBossCount)
                return;

            if (owner.Match != null)
                owner.Match.MatchFinish();
            else
                owner.OnMatchFinish();
        }

        /// <inheritdoc/>
        public void OnEncounterReset()
        {
            defeatedBosses.Clear();
        }
    }
}
