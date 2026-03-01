using NexusForever.Game.Abstract.Challenge;
using NexusForever.Game.Static.Challenges;
using NexusForever.Network.World.Message.Model.Challenges;

namespace NexusForever.Game.Challenge
{
    public class Challenge : IChallenge
    {
        public uint Id => info.Id;
        public bool IsUnlocked { get; private set; } = true;
        public bool IsActivated { get; private set; }
        public bool IsCompleted { get; private set; }

        private readonly IChallengeInfo info;
        private uint currentCount;
        private uint currentTier;
        private uint completionCount;

        public Challenge(IChallengeInfo info)
        {
            this.info = info;
        }

        public void Activate()
        {
            if (IsActivated || IsCompleted)
                return;

            IsActivated  = true;
            currentCount = 0;
        }

        public void Abandon()
        {
            IsActivated  = false;
            currentCount = 0;
            currentTier  = 0;
        }

        /// <summary>
        /// Notify the challenge that a creature was killed.
        /// Returns true if progress advanced.
        /// </summary>
        public bool OnEntityKilled(uint creatureId)
        {
            if (!IsActivated || IsCompleted)
                return false;

            if (info.Type != ChallengeType.Combat)
                return false;

            if (info.Target != 0 && info.Target != creatureId)
                return false;

            currentCount++;

            // Advance tier when the tier goal count is reached
            for (uint tier = currentTier; tier < 3; tier++)
            {
                uint goal = info.TierGoalCounts[tier];
                if (goal == 0)
                    break;

                if (currentCount >= goal)
                    currentTier = tier + 1;
            }

            // Check completion: highest non-zero tier goal reached OR fallback to CompletionCount
            uint completionGoal = GetCompletionGoal();
            if (completionGoal > 0 && currentCount >= completionGoal)
            {
                IsActivated  = false;
                IsCompleted  = true;
                completionCount++;
            }

            return true;
        }

        private uint GetCompletionGoal()
        {
            // Use highest tier goal as the completion threshold
            for (int i = 2; i >= 0; i--)
            {
                if (info.TierGoalCounts[i] > 0)
                    return info.TierGoalCounts[i];
            }
            return info.CompletionCount;
        }

        public ServerChallengeUpdate.Challenge Build()
        {
            uint completionGoal = GetCompletionGoal();
            return new ServerChallengeUpdate.Challenge
            {
                ChallengeId      = Id,
                Type             = info.Type,
                TargetGroupId    = info.TargetGroupId,
                CurrentCount     = currentCount,
                GoalCount        = completionGoal,
                CurrentTier      = currentTier,
                CompletionCount  = completionCount,
                TierGoalCount    = info.TierGoalCounts.ToArray(),
                Unlocked         = IsUnlocked,
                Activated        = IsActivated,
            };
        }
    }
}
