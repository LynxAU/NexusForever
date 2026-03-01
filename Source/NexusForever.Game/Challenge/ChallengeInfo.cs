using NexusForever.Game.Abstract.Challenge;
using NexusForever.Game.Static.Challenges;
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

        public ChallengeInfo(ChallengeEntry entry, ChallengeTierEntry[] tiers)
        {
            Id             = entry.Id;
            Type           = (ChallengeType)entry.ChallengeTypeEnum;
            Target         = entry.Target;
            CompletionCount = entry.CompletionCount;
            TargetGroupId  = entry.TargetGroupIdRewardPane;

            TierGoalCounts = new uint[3];
            if (tiers[0] != null) TierGoalCounts[0] = tiers[0].Count;
            if (tiers[1] != null) TierGoalCounts[1] = tiers[1].Count;
            if (tiers[2] != null) TierGoalCounts[2] = tiers[2].Count;
        }
    }
}
