using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Expedition
{
    /// <summary>
    /// Map script for The Gauntlet expedition (WorldId 2183).
    ///
    /// Completion condition: all four elite bosses must die.
    ///   Obj 1864 KillTargetGroup — Creature2Id 69254 (level 50 elite, faction 218)
    ///   Obj 1865 KillTargetGroup — Creature2Id 69255 (level 50 elite, faction 988)
    ///   Obj 1873 KillTargetGroup — Creature2Id 69308 (level 50 elite, faction 219)
    ///   Obj 1874 KillTargetGroup — Creature2Id 69305 (level 50 elite, faction 219) — final boss
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Expedition/Gauntlet.sql
    /// </summary>
    [ScriptFilterOwnerId(2183)]
    public class GauntletScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        // All four elite KillTargetGroup bosses from PublicEvent objectives 1864/1865/1873/1874.
        // Source: expedition-data-report.md
        private static readonly HashSet<uint> BossCreatureIds = new() { 69254u, 69255u, 69308u, 69305u };

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

            // All four bosses defeated — instance complete.
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
