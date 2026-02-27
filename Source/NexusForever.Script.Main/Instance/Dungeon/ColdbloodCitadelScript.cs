using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    /// <summary>
    /// Map script for Coldblood Citadel dungeon (WorldId 3522).
    ///
    /// Completion condition: Harizog Coldblood (final boss) must die.
    ///   Harizog Coldblood — Creature2Id 75459 ("[CBC] Harizog Coldblood - Final Boss")
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Dungeon/Coldblood Citadel.sql
    /// Source: Creature2.tbl name search (prefix [CBC]).
    /// </summary>
    [ScriptFilterOwnerId(3522)]
    public class ColdbloodCitadelScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        private const uint BossCreatureId = 75459u;  // Harizog Coldblood — final boss

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
