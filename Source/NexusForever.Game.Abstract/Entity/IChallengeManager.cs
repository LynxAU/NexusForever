using NexusForever.Shared;

namespace NexusForever.Game.Abstract.Entity
{
    public interface IChallengeManager : IUpdate, IDisposable
    {
        /// <summary>
        /// Send initial challenge state packets to the client on login.
        /// </summary>
        void SendInitialPackets();

        /// <summary>
        /// Activate a challenge by id (player chose to start it).
        /// </summary>
        void ChallengeActivate(uint challengeId);

        /// <summary>
        /// Abandon an active challenge by id.
        /// </summary>
        void ChallengeAbandon(uint challengeId);

        /// <summary>
        /// Notify the manager that a creature was killed so Combat-type challenges can advance.
        /// </summary>
        void OnEntityKilled(uint creatureId);

        /// <summary>
        /// Notify the manager that a spell was cast so Ability-type challenges can advance.
        /// </summary>
        void OnSpellCast(uint spell4Id);

        /// <summary>
        /// Notify the manager that an item was collected so Item/Collect challenges can advance.
        /// </summary>
        void OnItemCollected(uint itemId);
    }
}
