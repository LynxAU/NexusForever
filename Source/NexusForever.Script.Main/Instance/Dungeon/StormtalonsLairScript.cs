using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    /// <summary>
    /// Map script for Stormtalon's Lair dungeon (WorldId 382, internal name "EthnDunon").
    ///
    /// Completion condition: all four boss encounters must be defeated.
    ///   Invoker           — TG 2585 — Creature2Id 17160 (Normal) / 33405 (Veteran)
    ///   Aethros           — TG 2586 — Creature2Id 17166 (Normal) / 32703 (Veteran) — final boss
    ///   Arcanist Breeze-Binder — TG 3030 — Creature2Id 24474 (Normal) / 34711 (Veteran) — miniboss
    ///   Overseer Drift-Catcher — TG 3921 — Creature2Id 33361 (Normal) / 33362 (Veteran) — miniboss
    ///
    /// Both Normal and Veteran creature IDs are tracked; the instance completes when any
    /// 4 distinct creature IDs from the tracked set have died (covers both difficulty modes).
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Dungeon/Stormtalons Lair.sql
    /// Source: TargetGroup entries for PublicEvent 145 (WorldId 382).
    /// </summary>
    [ScriptFilterOwnerId(382)]
    public class StormtalonsLairScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        // Boss creature IDs — both Normal and Veteran variants.
        // Only one set will spawn per run; RequiredBossCount ensures either set triggers completion.
        private static readonly HashSet<uint> BossCreatureIds = new()
        {
            17160u, 33405u,  // Invoker (Normal, Veteran)
            17166u, 32703u,  // Aethros (Normal, Veteran)
            24474u, 34711u,  // Arcanist Breeze-Binder (Normal, Veteran)
            33361u, 33362u,  // Overseer Drift-Catcher (Normal, Veteran)
        };

        // Number of distinct boss encounters — 4 regardless of Normal/Veteran.
        private const int RequiredBossCount = 4;

        private IContentMapInstance owner;
        private HashSet<uint> defeatedBosses = new();

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

            // All four boss encounters defeated — instance complete.
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
