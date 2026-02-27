using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    /// <summary>
    /// Map script for Skullcano dungeon (WorldId 1263).
    ///
    /// Completion condition: all five boss encounters must be defeated.
    ///   Stew-Shaman Tugga    — TG 2599 — Creature2Id 24493 (Normal) / 24898 (Veteran)
    ///   Thunderfoot          — TG 2600 — Creature2Id 24475 (Normal) / 24893 (Veteran)
    ///   Bosun Octog          — TG 2601 — Creature2Id 24486 (Normal) / 24894 (Veteran)
    ///   Quartermaster Gruh   — TG 2869 — Creature2Id 24490 (Normal) / 24896 (Veteran) — miniboss
    ///   Mordechai Redmoon    — Creature2Id 24489 (Normal) / 24895 (Veteran) — final boss
    ///
    /// Both Normal and Veteran creature IDs are tracked; the instance completes when any
    /// 5 distinct creature IDs from the tracked set have died.
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Dungeon/Skullcano.sql
    /// Source: TargetGroup entries for PublicEvent 148 (WorldId 1263) + Creature2.tbl name search.
    /// </summary>
    [ScriptFilterOwnerId(1263)]
    public class SkullcanoScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        private static readonly HashSet<uint> BossCreatureIds = new()
        {
            24493u, 24898u,  // Stew-Shaman Tugga (Normal, Veteran)
            24475u, 24893u,  // Thunderfoot (Normal, Veteran)
            24486u, 24894u,  // Bosun Octog (Normal, Veteran)
            24490u, 24896u,  // Quartermaster Gruh (Normal, Veteran)
            24489u, 24895u,  // Mordechai Redmoon — final boss (Normal, Veteran)
        };

        private const int RequiredBossCount = 5;

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
