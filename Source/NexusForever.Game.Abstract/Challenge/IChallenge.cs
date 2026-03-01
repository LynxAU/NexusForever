using NexusForever.Network.World.Message.Model.Challenges;

namespace NexusForever.Game.Abstract.Challenge
{
    public interface IChallenge
    {
        uint Id { get; }
        uint RewardTrackId { get; }
        bool IsUnlocked { get; }
        bool IsActivated { get; }
        bool IsCompleted { get; }
        bool IsOnCooldown { get; }

        /// <summary>
        /// Activate this challenge, starting the timer and progress tracking.
        /// </summary>
        void Activate();

        /// <summary>
        /// Abandon this challenge, clearing progress and entering cooldown.
        /// </summary>
        void Abandon();

        /// <summary>
        /// Tick timers. Returns true if the challenge just expired.
        /// </summary>
        bool Update(double lastTick);

        /// <summary>
        /// Returns the tier index (0-based) just advanced to, then clears it. Null if no pending tier.
        /// </summary>
        uint? ConsumePendingTierNotify();

        /// <summary>
        /// Notify the challenge that a creature was killed. Returns true if progress advanced.
        /// </summary>
        bool OnEntityKilled(uint creatureId);

        /// <summary>
        /// Notify the challenge that a spell was cast. Returns true if progress advanced (Ability type).
        /// </summary>
        bool OnSpellCast(uint spell4Id);

        /// <summary>
        /// Build a network packet representation of the current challenge state.
        /// </summary>
        ServerChallengeUpdate.Challenge Build();
    }
}
