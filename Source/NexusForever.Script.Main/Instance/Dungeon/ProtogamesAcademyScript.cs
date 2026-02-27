using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    /// <summary>
    /// Map script for Protogames Academy dungeon (WorldId 3173, internal "UltimateProtogamesJuniors").
    ///
    /// Completion condition: all six boss encounters must be defeated.
    ///   Invulnotron        — TG 12356 — Creature2Id 67475 (Normal) / 71082 (Veteran)
    ///   Osun Witch         — TG 12357 — Creature2Id 67594 (Normal) / 71319 (Veteran)
    ///   Iruki              — TG 12358 — Creature2Id 67663 (Normal) / 71538 (Veteran)
    ///   Seek-N-Slaughter   — TG 12359 — Creature2Id 67668 (Normal) / 71203 (Veteran)
    ///   Icebox Mk. 2       — TG 12360 — Creature2Id 67757 (Normal) / 71205 (Veteran)
    ///   Super-Invulnotron  — TG 12361 — Creature2Id 68096 (Normal) / 71209 (Veteran) — final boss
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Dungeon/Protogames Academy.sql
    /// Source: TargetGroup entries for PublicEvent 667 (WorldId 3173).
    /// </summary>
    [ScriptFilterOwnerId(3173)]
    public class ProtogamesAcademyScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        private static readonly HashSet<uint> BossCreatureIds = new()
        {
            67475u, 71082u,  // Invulnotron (Normal, Veteran)
            67594u, 71319u,  // Osun Witch (Normal, Veteran)
            67663u, 71538u,  // Iruki (Normal, Veteran)
            67668u, 71203u,  // Seek-N-Slaughter (Normal, Veteran)
            67757u, 71205u,  // Icebox Mk. 2 (Normal, Veteran)
            68096u, 71209u,  // Super-Invulnotron (Normal, Veteran)
        };

        private const int RequiredBossCount = 6;

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
