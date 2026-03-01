using NexusForever.Network.World.Message.Model.Challenges;

namespace NexusForever.Game.Abstract.Challenge
{
    public interface IChallenge
    {
        uint Id { get; }
        bool IsUnlocked { get; }
        bool IsActivated { get; }
        bool IsCompleted { get; }

        /// <summary>
        /// Activate this challenge, starting the timer and progress tracking.
        /// </summary>
        void Activate();

        /// <summary>
        /// Abandon this challenge, clearing progress.
        /// </summary>
        void Abandon();

        /// <summary>
        /// Notify the challenge that a creature was killed. Returns true if progress advanced.
        /// </summary>
        bool OnEntityKilled(uint creatureId);

        /// <summary>
        /// Build a network packet representation of the current challenge state.
        /// </summary>
        ServerChallengeUpdate.Challenge Build();
    }
}
