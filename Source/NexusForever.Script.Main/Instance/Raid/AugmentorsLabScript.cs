using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Raid
{
    /// <summary>
    /// Map script for Augmentors' Lab raid (WorldId 3040, internal "AugmentorsLab").
    ///
    /// NOTE: Boss creature IDs are unverified. Candidate IDs from Creature2.tbl "[IC]" tag search
    /// (which references "Augmentors Raid" and "w3040" in creature name strings):
    ///   e2681 — Augmenters God Unit (50979)
    ///   e2681 — Prime Evolutionary Operant (50472)
    ///
    /// The "[IC]" prefix likely stands for "Infinite Crimelabs" — the facility used for both
    /// Infinite Labs (WorldId 2980) and Augmentors' Lab (WorldId 3040). Creature IDs require
    /// in-game testing to confirm which world they spawn in.
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Raid/Augmentors Lab.sql (stub — no coordinate data).
    /// TODO: Confirm creature IDs via in-game testing or retail sniff data.
    /// </summary>
    [ScriptFilterOwnerId(3040)]
    public class AugmentorsLabScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        // Candidate IDs from [IC] Creature2.tbl search — unverified.
        // 50979 = [IC] e2681 - Augmentors - Augmenters God Unit
        // 50472 = [IC] e2681 - Augmentors - Prime Evolutionary Operant
        private static readonly HashSet<uint> BossCreatureIds = new()
        {
            50979u,  // Augmenters God Unit (main boss — unverified)
            50472u,  // Prime Evolutionary Operant (unverified)
        };

        private const int RequiredBossCount = 2;

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
