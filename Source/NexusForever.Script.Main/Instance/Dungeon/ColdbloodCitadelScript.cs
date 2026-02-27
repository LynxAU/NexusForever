using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    /// <summary>
    /// Map script for Coldblood Citadel dungeon (WorldId 3522).
    ///
    /// Completion condition: boss creature 14447 must die.
    ///   PublicEvent 907 references CreatureId 14447 via KillEventObjectiveUnit (type 8).
    ///   This creature ID is NOT found in the extracted Creature2.tbl.
    ///   TODO: Verify creature ID against retail sniff data or in-game testing.
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Dungeon/Coldblood Citadel.sql
    /// Source: PublicEvent 907 objectives (WorldId 3522).
    /// </summary>
    [ScriptFilterOwnerId(3522)]
    public class ColdbloodCitadelScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        // TODO: Verify this creature ID — not found in Creature2.tbl extraction.
        private const uint BossCreatureId = 14447u;

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
