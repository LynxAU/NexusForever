using System;
using System.Collections.Generic;
using System.Linq;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Entity
{
    public class InstanceManager : IInstanceManager
    {
        private readonly IPlayer player;
        private readonly Dictionary<ushort, CharacterInstanceModel> instances = new();

        public InstanceManager(IPlayer player)
        {
            this.player = player;
        }

        /// <summary>
        /// Initialise instance manager with data from database.
        /// </summary>
        public void Initialise(IEnumerable<CharacterInstanceModel> dbInstances)
        {
            instances.Clear();
            foreach (var instance in dbInstances)
            {
                // Only add non-expired instances or instances with future expiry
                if (instance.LockoutExpiry == DateTime.MaxValue || instance.LockoutExpiry > DateTime.UtcNow)
                {
                    instances[instance.WorldId] = instance;
                }
            }
        }

        public IEnumerable<CharacterInstanceModel> GetInstances()
        {
            return instances.Values;
        }

        public CharacterInstanceModel GetInstance(ushort worldId)
        {
            return instances.TryGetValue(worldId, out var instance) ? instance : null;
        }

        public bool HasInstanceLockout(ushort worldId)
        {
            if (!instances.TryGetValue(worldId, out var instance))
                return false;

            return instance.LockoutExpiry > DateTime.UtcNow;
        }

        public bool IsInstanceLockoutExpired(ushort worldId)
        {
            if (!instances.TryGetValue(worldId, out var instance))
                return true;

            return instance.LockoutExpiry <= DateTime.UtcNow;
        }

        public void SetInstance(CharacterInstanceModel instance)
        {
            instance.CharacterId = player.CharacterId;
            instances[instance.WorldId] = instance;
        }

        public void RemoveInstance(ushort worldId)
        {
            instances.Remove(worldId);
        }

        public void ResetAllInstances()
        {
            instances.Clear();
        }

        public void ResetInstance(ushort worldId)
        {
            instances.Remove(worldId);
        }

        /// <summary>
        /// Set instance lockout from completing a match.
        /// </summary>
        public void SetInstanceLockout(ushort worldId, TimeSpan lockoutDuration, byte difficulty, byte primeLevel)
        {
            var instance = new CharacterInstanceModel
            {
                WorldId = worldId,
                LockoutExpiry = DateTime.UtcNow + lockoutDuration,
                Difficulty = difficulty,
                PrimeLevel = primeLevel
            };
            SetInstance(instance);
        }

        /// <summary>
        /// Save instance data to the provided CharacterContext.
        /// </summary>
        public void Save(CharacterContext context)
        {
            // Remove all existing instance entries for this character
            var existingInstances = context.CharacterInstance.Where(i => i.CharacterId == player.CharacterId);
            context.CharacterInstance.RemoveRange(existingInstances);
            
            // Add all current instances
            foreach (var instance in instances.Values)
            {
                instance.CharacterId = player.CharacterId;
                context.CharacterInstance.Add(instance);
            }
        }
    }
}
