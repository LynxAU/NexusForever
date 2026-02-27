using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Expedition
{
    /// <summary>
    /// Map script for the Fragment Zero expedition (WorldId 3180).
    ///
    /// Completion condition: all five elite bosses must die.
    ///   Obj 4417 KillTargetGroup — Creature2Id 69664 (level 50 elite, faction 248)
    ///   Obj 4423 KillTargetGroup — Creature2Id 69672 (level 50 elite, faction 248)
    ///   Obj 4444 KillTargetGroup — Creature2Id 69635 (level 50 elite, faction 219)
    ///   Obj 4449 KillTargetGroup — Creature2Id 69673 (level 50 elite, faction 248)
    ///   Obj 4450 KillTargetGroup — Creature2Id 69674 (level 50 elite, faction 248) — final boss
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Expedition/Fragment Zero.sql
    /// </summary>
    [ScriptFilterOwnerId(3180)]
    public class FragmentZeroScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        // All five elite KillTargetGroup bosses from PublicEvent objectives 4417/4423/4444/4449/4450.
        // Source: expedition-data-report.md
        private static readonly HashSet<uint> BossCreatureIds = new()
        {
            69664u, // Obj 4417
            69672u, // Obj 4423
            69635u, // Obj 4444
            69673u, // Obj 4449
            69674u, // Obj 4450 — final boss
        };

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

            if (defeatedBosses.Count < BossCreatureIds.Count)
                return;

            // All five bosses defeated — instance complete.
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
