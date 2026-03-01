using System;
using NexusForever.Game.Abstract.Challenge;
using NexusForever.Game.Static.Challenges;
using NexusForever.Network.World.Message.Model.Challenges;

namespace NexusForever.Game.Challenge
{
    public class Challenge : IChallenge
    {
        private const double ActiveDuration   = 300.0;   // 5 minutes
        private const double CooldownDuration = 1800.0;  // 30 minutes

        public uint Id            => info.Id;
        public uint RewardTrackId => info.RewardTrackId;
        public bool IsUnlocked    { get; private set; } = true;
        public bool IsActivated   { get; private set; }
        public bool IsCompleted   { get; private set; }
        public bool IsOnCooldown  { get; private set; }
        public uint CurrentCount => currentCount;
        public uint CurrentTier => currentTier;
        public uint CompletionCount => completionCount;
        public double TimeRemainingSeconds => timeRemaining;
        public double CooldownRemainingSeconds => cooldownRemaining;
        public uint ActivatedDt => activatedDt;

        private readonly IChallengeInfo info;
        private uint   currentCount;
        private uint   currentTier;
        private uint   completionCount;
        private double timeRemaining;
        private double cooldownRemaining;
        private uint   activatedDt;    // Unix timestamp for packet
        private uint?  pendingTier;    // 0-based tier just reached, awaiting notify

        public Challenge(IChallengeInfo info)
        {
            this.info = info;
        }

        public void Activate()
        {
            if (IsActivated || IsOnCooldown)
                return;

            IsActivated   = true;
            IsCompleted   = false;
            currentCount  = 0;
            currentTier   = 0;
            timeRemaining = ActiveDuration;
            activatedDt   = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            pendingTier   = null;
        }

        public void Abandon()
        {
            IsActivated       = false;
            currentCount      = 0;
            currentTier       = 0;
            pendingTier       = null;
            IsOnCooldown      = true;
            cooldownRemaining = CooldownDuration;
        }

        /// <summary>
        /// Tick timers. Returns true if the challenge just expired due to timer running out.
        /// </summary>
        public bool Update(double lastTick)
        {
            if (IsOnCooldown)
            {
                cooldownRemaining -= lastTick;
                if (cooldownRemaining <= 0d)
                {
                    IsOnCooldown      = false;
                    cooldownRemaining = 0d;
                }
                return false;
            }

            if (!IsActivated || IsCompleted)
                return false;

            timeRemaining -= lastTick;
            if (timeRemaining <= 0d)
            {
                IsActivated       = false;
                IsOnCooldown      = true;
                cooldownRemaining = CooldownDuration;
                timeRemaining     = 0d;
                pendingTier       = null;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the 0-based tier index just advanced to, then clears it. Null if none pending.
        /// </summary>
        public uint? ConsumePendingTierNotify()
        {
            uint? tier = pendingTier;
            pendingTier = null;
            return tier;
        }

        public bool OnEntityKilled(uint creatureId)
        {
            if (!IsActivated || IsCompleted)
                return false;

            if (info.Type != ChallengeType.Combat)
                return false;

            if (info.Target != 0 && info.Target != creatureId)
                return false;

            return AdvanceCount();
        }

        public bool OnSpellCast(uint spell4Id)
        {
            if (!IsActivated || IsCompleted)
                return false;

            if (info.Type != ChallengeType.Ability)
                return false;

            if (info.Target != 0 && info.Target != spell4Id)
                return false;

            return AdvanceCount();
        }

        public bool OnItemCollected(uint itemId)
        {
            if (!IsActivated || IsCompleted)
                return false;

            if (info.Type is not (ChallengeType.Item or ChallengeType.Collect))
                return false;

            if (info.Target != 0 && info.Target != itemId)
                return false;

            return AdvanceCount();
        }

        private bool AdvanceCount()
        {
            currentCount++;

            uint prevTier = currentTier;

            for (uint tier = currentTier; tier < 3; tier++)
            {
                uint goal = info.TierGoalCounts[tier];
                if (goal == 0)
                    break;

                if (currentCount >= goal)
                    currentTier = tier + 1;
            }

            // Queue tier notification if a new tier was reached (0-based index)
            if (currentTier > prevTier)
                pendingTier = currentTier - 1;

            uint completionGoal = GetCompletionGoal();
            if (completionGoal > 0 && currentCount >= completionGoal)
            {
                IsActivated       = false;
                IsCompleted       = true;
                IsOnCooldown      = true;
                cooldownRemaining = CooldownDuration;
                completionCount++;
            }

            return true;
        }

        private uint GetCompletionGoal()
        {
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
            uint cooldownEndDt = IsOnCooldown
                ? (uint)DateTimeOffset.UtcNow.AddSeconds(Math.Max(0d, cooldownRemaining)).ToUnixTimeSeconds()
                : 0u;

            return new ServerChallengeUpdate.Challenge
            {
                ChallengeId       = Id,
                Type              = info.Type,
                TargetGroupId     = info.TargetGroupId,
                CurrentCount      = currentCount,
                GoalCount         = completionGoal,
                CurrentTier       = currentTier,
                CompletionCount   = completionCount,
                TierGoalCount     = info.TierGoalCounts.ToArray(),
                Unlocked          = IsUnlocked,
                Activated         = IsActivated,
                OnCooldown        = IsOnCooldown,
                TimeActivatedDt   = IsActivated ? activatedDt : 0u,
                TimeTotalActive   = (uint)ActiveDuration,
                TimeCooldownDt    = cooldownEndDt,
                TimeTotalCooldown = (uint)CooldownDuration,
            };
        }

        public void RestoreState(
            bool unlocked,
            bool activated,
            bool completed,
            bool onCooldown,
            uint currentCount,
            uint currentTier,
            uint completionCount,
            double timeRemainingSeconds,
            double cooldownRemainingSeconds,
            uint activatedDt)
        {
            IsUnlocked        = unlocked;
            IsActivated       = activated;
            IsCompleted       = completed;
            IsOnCooldown      = onCooldown;
            this.currentCount = currentCount;
            this.currentTier  = currentTier;
            this.completionCount = completionCount;
            timeRemaining     = Math.Max(0d, timeRemainingSeconds);
            cooldownRemaining = Math.Max(0d, cooldownRemainingSeconds);
            this.activatedDt  = activatedDt;
            pendingTier       = null;
        }
    }
}
