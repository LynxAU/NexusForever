using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    /// <summary>
    /// Map script for Hall of the Hundred dungeon (WorldId 3009, internal "HalloftheHundred").
    ///
    /// Completion condition: both mandatory boss encounters must be defeated.
    ///   Varegor the Abominable - Creature2Id 67457 (Ice Gorganoth, World Story 2)
    ///   Harizog Coldblood      - Creature2Id 67444 (Part 6 - Hall of the Hundred, final boss)
    ///
    /// Additional boss creatures present (optional/side encounters):
    ///   Icebound Overlord   - 71577 (WS2 Ice Boss)
    ///   Darkwitch Yotul     - 71173 (Optional Osun Witch Boss)
    ///   Unbound Flame Elemental - 71414 (Optional Boss)
    ///
    /// PublicEvent IDs for this world: 666 (main), 677, 678, 693, 696, 874, 875.
    ///
    /// Creature IDs confirmed via Creature2.tbl description search (w3009 tag).
    /// </summary>
    [ScriptFilterOwnerId(3009)]
    public class HallOfTheHundredScript : IContentMapScript, IOwnedScript<IContentMapInstance>
    {
        private static readonly HashSet<uint> RequiredBossIds = new()
        {
            67457u,  // Varegor the Abominable (Ice Gorganoth, World Story 2 boss)
            67444u,  // Harizog Coldblood (Part 6 - Hall of the Hundred, final boss)
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
