using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Expedition
{
    /// <summary>
    /// Map script for the Deep Space Exploration expedition (WorldId 2188).
    ///
    /// Completion condition: Creature2Id 69299 (level 50 elite, faction 340) must die.
    ///   Source: expedition-data-report.md, PublicEvent objective 1852 KillTargetGroup.
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Expedition/Deep Space Exploration.sql
    /// </summary>
    [ScriptFilterOwnerId(2188)]
    public class DeepSpaceExplorationScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        // Creature2Id 69299 — level 50 elite, faction 340, final boss of Deep Space Exploration.
        // Source: expedition-data-report.md, PublicEvent objective 1852 KillTargetGroup.
        private const uint BossCreatureId = 69299u;

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
            if (creatureId != BossCreatureId || bossDefeated)
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
