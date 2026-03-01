using NexusForever.Game.Static.Challenges;

namespace NexusForever.Game.Abstract.Challenge
{
    public interface IChallengeInfo
    {
        uint Id { get; }
        ChallengeType Type { get; }

        /// <summary>
        /// The creature ID (Combat type) or other target identifier relevant to this challenge.
        /// </summary>
        uint Target { get; }

        /// <summary>
        /// Maximum number of times this challenge may be completed.
        /// </summary>
        uint CompletionCount { get; }

        /// <summary>
        /// TargetGroup identifier used in the reward pane network packet.
        /// </summary>
        uint TargetGroupId { get; }

        /// <summary>
        /// Kill goals for each tier (3 tiers max). Zero means the tier is unused.
        /// </summary>
        uint[] TierGoalCounts { get; }
    }
}
