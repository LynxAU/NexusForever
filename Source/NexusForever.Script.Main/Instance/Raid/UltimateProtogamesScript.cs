using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Raid
{
    /// <summary>
    /// Map script for Ultimate Protogames raid (WorldId 3041, internal "UltiProtogames").
    ///
    /// Completion condition: both main boss encounters must be defeated.
    ///   e2675 — Hut-Hut (Gorganoth Boss)      — Creature2Id 61417
    ///   e2680 — Bev-O-Rage (Vending Machine)  — Creature2Id 61463
    ///
    /// Optional miniboss encounters (not required for completion):
    ///   e2673 — Crate Destruction Miniboss    — Creature2Id 62575
    ///   e2674 — Mixed Wave Miniboss           — Creature2Id 63319
    ///   Mixed Wave disguises (Fire/Water/Air/Earth) — Creature2Id 67340–67343
    ///
    /// Source: Creature2.tbl "[UP]" (Ub3r-Proto) name tag search.
    /// Spawn data: see WorldDatabaseRepo/Instance/Raid/Ultimate Protogames.sql (stub — no coordinate data).
    /// </summary>
    [ScriptFilterOwnerId(3041)]
    public class UltimateProtogamesScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        private static readonly HashSet<uint> BossCreatureIds = new()
        {
            61417u,  // e2675 — Hut-Hut (Gorganoth Boss)
            61463u,  // e2680 — Bev-O-Rage (Vending Machine Boss)
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
