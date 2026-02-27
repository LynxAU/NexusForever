using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Expedition
{
    /// <summary>
    /// Map script for the Infestation expedition (WorldId 1232).
    ///
    /// Completion condition: Creature2Id 69871 (the expedition final boss) must die.
    /// When the boss dies the active match (if present) is finished, which saves lockouts
    /// and signals the client that the instance is complete.
    ///
    /// Encounter reset fires when all players leave so the instance is clean for
    /// the next group that joins via a solo/group lock.
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Expedition/Infestation.sql
    /// </summary>
    [ScriptFilterOwnerId(1232)]
    public class InfestationScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        // Creature2Id 69871 — level 50 elite, faction 218 (enemy), final boss of Infestation.
        // Source: expedition-data-report.md, PublicEvent objective 4698 KillTargetGroup.
        private const uint BossCreatureId = 69871u;

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

            // If the instance was entered via the LFG/matching system a Match will be
            // attached. Finishing the match saves lockouts and sends the completion UI.
            // For direct (solo/group) entry Match is null, so save lockouts manually.
            if (owner.Match != null)
                owner.Match.MatchFinish();
            else
                owner.OnMatchFinish();
        }

        /// <inheritdoc/>
        public void OnEncounterReset()
        {
            bossDefeated = false;
            // Boss will respawn via BaseMap.ProcessRespawns (30s corpse + 30s respawn timer)
            // when its SpawnModel is set from the entity DB row.
        }
    }
}
