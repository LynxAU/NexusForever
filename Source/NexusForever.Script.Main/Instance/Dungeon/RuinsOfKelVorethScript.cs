using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    /// <summary>
    /// Map script for Ruins of Kel Voreth dungeon (WorldId 1336, internal "OsunDungeon").
    ///
    /// Completion condition: all four boss encounters must be defeated.
    ///   Grond the Corpsemaker — TG 3841 — Creature2Id 32534 (Normal) / 32535 (Veteran)
    ///   Forgemaster Trogun   — Creature2Id 32531 (Normal) / 32533 (Veteran) — second boss
    ///   Slavemaster Drokk    — TG 3842 — Creature2Id 32536 (Normal) / 32539 (Veteran) — final boss
    ///   Darkwitch Gurka      — TG 3850 — Creature2Id 33049 (Normal) / 33050 (Veteran) — optional miniboss
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Dungeon/Ruins of Kel Voreth.sql
    /// Source: TargetGroup entries for PublicEvent 161 (WorldId 1336) + Creature2.tbl name search.
    /// </summary>
    [ScriptFilterOwnerId(1336)]
    public class RuinsOfKelVorethScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        private static readonly HashSet<uint> BossCreatureIds = new()
        {
            32534u, 32535u,  // Grond the Corpsemaker (Normal, Veteran)
            32531u, 32533u,  // Forgemaster Trogun (Normal, Veteran)
            32536u, 32539u,  // Slavemaster Drokk (Normal, Veteran)
            33049u, 33050u,  // Darkwitch Gurka — optional miniboss (Normal, Veteran)
        };

        // Grond + Trogun + Drokk + Gurka — all 4 encounters required for completion.
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
