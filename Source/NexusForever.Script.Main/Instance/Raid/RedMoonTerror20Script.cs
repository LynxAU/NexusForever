using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Raid
{
    /// <summary>
    /// Map script for Red Moon Terror 20-player raid (WorldId 3032, internal "RedMoonTerror20").
    ///
    /// Completion condition: all main boss encounters must be defeated.
    ///   e4550 Engineers — Hammer (65758) + Gun (65759)
    ///   e4554 Mordechai Redmoon (65800)
    ///         Laveka (65997)
    ///         Robomination (66085)
    ///
    /// RequiredBossCount = 2 + 1 + 1 + 1 = 5 individual boss deaths.
    ///
    /// Source: Creature2.tbl "[RMT]" name tag search.
    /// Spawn data: see WorldDatabaseRepo/Instance/Raid/Red Moon Terror 20.sql (stub — no coordinate data).
    /// TODO: Verify encounter order and whether additional bosses exist in 20-man tuning.
    /// </summary>
    [ScriptFilterOwnerId(3032)]
    public class RedMoonTerror20Script : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        private static readonly HashSet<uint> BossCreatureIds = new()
        {
            65758u, 65759u,   // e4550 — Engineers (Hammer, Gun)
            65800u,           // e4554 — Mordechai Redmoon
            65997u,           // Laveka
            66085u,           // Robomination
        };

        // 2 (Engineers) + 1 (Redmoon) + 1 (Laveka) + 1 (Robomination) = 5
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
