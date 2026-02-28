using System.Collections.Generic;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Expedition
{
    /// <summary>
    /// Map script for the Deep Space Exploration expedition (WorldId 2188).
    ///
    /// Completion condition: Riptide the Stone Serpent must die.
    ///   69299  level 50 elite, faction 340 (veteran)
    ///   48704  level 35-50 elite, faction 340 (scaling variant)
    ///   Source: expedition-data-report.md, PublicEvent objective 1852 KillTargetGroup.
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Expedition/Deep Space Exploration.sql
    /// </summary>
    [ScriptFilterOwnerId(2188)]
    public class DeepSpaceExplorationScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        private static readonly HashSet<uint> BossCreatureIds = new() { 69299u, 48704u };

        private IContentMapInstance owner;
        private bool bossDefeated;

        /// <inheritdoc/>
        public void OnLoad(IContentMapInstance owner)
        {
            this.owner = owner;
            bossDefeated = false;
        }

        /// <inheritdoc/>
        public void OnBossDeath(uint creatureId)
        {
            if (!BossCreatureIds.Contains(creatureId) || bossDefeated)
                return;

            bossDefeated = true;

            if (owner.Match != null)
                owner.Match.MatchFinish();
            else
                owner.OnMatchFinish();
        }

        /// <inheritdoc/>
        public void OnEncounterReset()
        {
            bossDefeated = false;
        }
    }
}
