using NexusForever.Game.Abstract.Challenge;
using NexusForever.Game.Static.Challenges;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Challenge
{
    public class ChallengeInfo : IChallengeInfo
    {
        public uint Id { get; }
        public ChallengeType Type { get; }
        public uint Target { get; }
        public uint CompletionCount { get; }
        public uint TargetGroupId { get; }
        public uint[] TierGoalCounts { get; }
        public uint RewardTrackId { get; }
        public uint RequiredWorldZoneId { get; }
        public uint CreatureCategoryFilterId { get; }

        public ChallengeInfo(ChallengeEntry entry, ChallengeTierEntry[] tiers)
        {
            Id              = entry.Id;
            Type            = (ChallengeType)entry.ChallengeTypeEnum;
            Target          = entry.Target;
            CompletionCount = entry.CompletionCount;
            TargetGroupId   = entry.TargetGroupIdRewardPane;
            RewardTrackId   = entry.RewardTrackId;
            RequiredWorldZoneId = entry.WorldZoneIdRestriction != 0u
                ? entry.WorldZoneIdRestriction
                : entry.WorldZoneId;

            // Some challenge rows use a category/faction-like filter instead of a concrete creature target.
            // Prefer explicit restriction field, then fall back to non-creature targets encoded in Target.
            CreatureCategoryFilterId = entry.TriggerVolume2IdRestriction;
            if (CreatureCategoryFilterId == 0u && entry.Target != 0u
                && GameTableManager.Instance.Creature2.GetEntry(entry.Target) == null)
                CreatureCategoryFilterId = entry.Target;

            TierGoalCounts = new uint[3];
            if (tiers[0] != null) TierGoalCounts[0] = tiers[0].Count;
            if (tiers[1] != null) TierGoalCounts[1] = tiers[1].Count;
            if (tiers[2] != null) TierGoalCounts[2] = tiers[2].Count;
        }
    }
}
