using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    /// <summary>
    /// Map script for Infinite Labs dungeon (WorldId 2980, internal "InfiniteLabs").
    ///
    /// Completion condition: boss creature 10569 must die.
    ///   PublicEvent 594 references CreatureId 10569 via KillEventObjectiveUnit (type 8/16).
    ///   This creature ID is NOT found in the extracted Creature2.tbl — it may be a
    ///   post-launch addition or use a different Creature2 table version.
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Dungeon/Infinite Labs.sql
    /// Source: PublicEvent 594 objectives (WorldId 2980).
    /// </summary>
    [ScriptFilterOwnerId(2980)]
    public class InfiniteLabsScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        // Objective-derived fallback from PublicEvent 594 kill objectives.
        private const uint BossCreatureId = 10569u;

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
