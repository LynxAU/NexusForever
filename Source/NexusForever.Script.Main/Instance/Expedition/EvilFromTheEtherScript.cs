using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Expedition
{
    /// <summary>
    /// Map script for the Evil From The Ether expedition (WorldId 3404).
    ///
    /// Completion condition: Creature2Id 71037 (level 23 elite, faction 218) must die.
    ///   Source: expedition-data-report.md, PublicEvent objective 4925 KillTargetGroup.
    ///   Note: 71039 is the level 50 scaling variant of the same boss — add that creature ID
    ///         to BossCreatureIds if the server is upgraded to serve level-50 content.
    ///
    /// Spawn data: see WorldDatabaseRepo/Instance/Expedition/Evil from the Ether.sql
    ///   (full sniff-captured spawn data from LaughingWS fork — real coordinates available)
    /// </summary>
    [ScriptFilterOwnerId(3404)]
    public class EvilFromTheEtherScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        // Creature2Id 71037 — level 23 elite, faction 218, primary boss of Evil From The Ether.
        // Source: expedition-data-report.md, PublicEvent objective 4925 KillTargetGroup.
        // 71039 is the level-50 scaling variant; add it here when needed.
        private const uint BossCreatureId = 71037u;

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
