using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Raid
{
    /// <summary>
    /// Map script for Red Moon Terror 40-player raid (WorldId 3102, internal "RedMoonTerror40").
    ///
    /// This is the 40-player difficulty version of Red Moon Terror.
    /// Boss encounters are assumed to be the same creature IDs as the 20-man (3032) but
    /// this requires in-game verification — the 40-man may use different creature IDs.
    ///
    /// Current boss set (from [RMT] Creature2.tbl search, shared with 20-man):
    ///   e4550 Engineers — Hammer (65758) + Gun (65759)
    ///   e4554 Mordechai Redmoon (65800)
    ///         Laveka (65997)
    ///         Robomination (66085)
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Raid/Red Moon Terror 40.sql (stub — no coordinate data).
    /// Note: Confirm whether 40-man uses distinct creature IDs or the same set as 20-man.
    /// </summary>
    [ScriptFilterOwnerId(3102)]
    public class RedMoonTerror40Script : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        // Shared with RedMoonTerror20Script pending confirmation that 40-man uses the same IDs.
        private static readonly HashSet<uint> BossCreatureIds = new()
        {
            65758u, 65759u,   // e4550 — Engineers (Hammer, Gun)
            65800u,           // e4554 — Mordechai Redmoon
            65997u,           // Laveka
            66085u,           // Robomination
        };

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
