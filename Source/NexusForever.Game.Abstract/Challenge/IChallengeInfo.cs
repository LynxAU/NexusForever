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

        /// <summary>
        /// RewardTrack identifier for delivering rewards on completion.
        /// </summary>
        uint RewardTrackId { get; }

        /// <summary>
        /// Optional world-zone restriction (0 means unrestricted).
        /// </summary>
        uint RequiredWorldZoneId { get; }

        /// <summary>
        /// Optional creature category/faction filter used for combat challenges (0 means unrestricted).
        /// </summary>
        uint CreatureCategoryFilterId { get; }

        /// <summary>
        /// Raw challenge flags from Challenge.tbl.
        /// </summary>
        uint Flags { get; }
    }
}
