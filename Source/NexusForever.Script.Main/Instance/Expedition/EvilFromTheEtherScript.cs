using System.Collections.Generic;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Expedition
{
    /// <summary>
    /// Map script for the Evil From The Ether expedition (WorldId 3404).
    ///
    /// Completion condition: Katja Zarkhov must die.
    ///   71037  level 23 elite, faction 218 (normal)
    ///   71039  level 50 elite, faction 218 (veteran scaling variant)
    ///   Source: expedition-data-report.md, PublicEvent objective 4925 KillTargetGroup.
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Expedition/Evil from the Ether.sql
    /// </summary>
    [ScriptFilterOwnerId(3404)]
    public class EvilFromTheEtherScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        private static readonly HashSet<uint> BossCreatureIds = new() { 71037u, 71039u };

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
