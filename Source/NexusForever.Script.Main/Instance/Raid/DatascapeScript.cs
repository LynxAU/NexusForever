using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Raid
{
    /// <summary>
    /// Map script for Datascape raid (WorldId 1333, internal "DataScape").
    ///
    /// Completion condition: all main boss encounters must be defeated.
    ///   e385 System Daemons — Null Boss (30495) + Binary Boss (30496)
    ///   e390 Maelstrom Authority — Air Boss (30497)
    ///   e393 Gloomclaw (30498)
    ///   e395 Elementals — Earth (30499), Water (30500), Life (30501), Air (30502), Fire (30503), Logic (30504)
    ///   e399 Avatus — Final Boss (30505)
    ///
    /// RequiredBossCount = 2 + 1 + 1 + 6 + 1 = 11 individual boss deaths.
    ///
    /// Source: Creature2.tbl "[DS] eXXX" name tag search.
    /// Spawn data: see WorldDatabaseRepo/Instance/Raid/Datascape.sql (stub — no coordinate data).
    /// TODO: Verify creature IDs and encounter order against retail sniff data.
    /// </summary>
    [ScriptFilterOwnerId(1333)]
    public class DatascapeScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        private static readonly HashSet<uint> BossCreatureIds = new()
        {
            30495u, 30496u,                                      // e385 — System Daemons (Null, Binary)
            30497u,                                              // e390 — Maelstrom Authority Air Boss
            30498u,                                              // e393 — Gloomclaw
            30499u, 30500u, 30501u, 30502u, 30503u, 30504u,    // e395 — Elementals (Earth/Water/Life/Air/Fire/Logic)
            30505u,                                              // e399 — Avatus (final boss)
        };

        // 2 (System Daemons) + 1 (Maelstrom) + 1 (Gloomclaw) + 6 (Elementals) + 1 (Avatus) = 11
        private const int RequiredBossCount = 11;

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

            // All main boss encounters defeated — raid complete.
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
