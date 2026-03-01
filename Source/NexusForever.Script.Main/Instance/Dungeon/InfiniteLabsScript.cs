using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    /// <summary>
    /// Map script for Infinite Labs dungeon (WorldId 2980, internal "InfiniteLabs").
    ///
    /// This dungeon hosts the Ultimate Protogames game-show experience - players
    /// compete in a series of mini-game events (Hut-Hut, Bev-O-Rage, Crate Destruction,
    /// etc.). Completion requires defeating the two main event bosses.
    ///
    ///   Hut-Hut Gorganoth  - event e2675 - Creature2Id 61417 (Gorganoth Boss)
    ///   Bev-O-Rage         - event e2680 - Creature2Id 61463 (Boss)
    ///
    /// Miniboss creatures also present (not required for completion):
    ///   Crate Destruction  - event e2673 - Creature2Id 62575 (Miniboss)
    ///   Mixed Wave         - event e2674 - Creature2Id 63319 (Miniboss)
    ///
    /// Creature IDs confirmed via Creature2.tbl description search ([UP]/w2980 tags).
    /// Source: PublicEvent 594 objectives (WorldId 2980).
    /// </summary>
    [ScriptFilterOwnerId(2980)]
    public class InfiniteLabsScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        private static readonly HashSet<uint> RequiredBossIds = new()
        {
            61417u,  // Hut-Hut Gorganoth Boss (event e2675)
            61463u,  // Bev-O-Rage Boss (event e2680)
        };

        private const int RequiredBossCount = 2;

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
            if (!RequiredBossIds.Contains(creatureId) || !defeatedBosses.Add(creatureId))
                return;

            if (defeatedBosses.Count < RequiredBossCount)
                return;

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
