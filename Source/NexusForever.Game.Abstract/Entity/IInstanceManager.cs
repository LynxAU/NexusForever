using System.Collections.Generic;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Abstract.Entity
{
    public interface IInstanceManager : IDatabaseCharacter
    {
        /// <summary>
        /// Initialise instance manager with instance data from the database character model.
        /// </summary>
        void Initialise(IEnumerable<CharacterInstanceModel> dbInstances);

        /// <summary>
        /// Get all instance entries for the player.
        /// </summary>
        IEnumerable<CharacterInstanceModel> GetInstances();

        /// <summary>
        /// Get instance entry by world id.
        /// </summary>
        CharacterInstanceModel GetInstance(ushort worldId);

        /// <summary>
        /// Check if player has a valid instance lockout for the given world.
        /// </summary>
        bool HasInstanceLockout(ushort worldId);

        /// <summary>
        /// Check if player's instance lockout has expired.
        /// </summary>
        bool IsInstanceLockoutExpired(ushort worldId);

        /// <summary>
        /// Add or update an instance entry for the player.
        /// </summary>
        void SetInstance(CharacterInstanceModel instance);

        /// <summary>
        /// Remove instance entry for the given world.
        /// </summary>
        void RemoveInstance(ushort worldId);

        /// <summary>
        /// Reset all instance lockouts for the player.
        /// </summary>
        void ResetAllInstances();

        /// <summary>
        /// Reset a specific instance lockout.
        /// </summary>
        void ResetInstance(ushort worldId);
    }
}
