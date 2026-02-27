using System;
using System.Collections.Generic;
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
        /// Save instance data to the provided CharacterContext.
        /// Note: The CharacterInstance table needs to be added to CharacterContext and migrated.
        /// </summary>
        public void Save(CharacterContext context)
        {
            // Database table CharacterInstance and migration required before this can be enabled
            // var existingInstances = context.CharacterInstance.Where(i => i.CharacterId == player.CharacterId);
            // context.CharacterInstance.RemoveRange(existingInstances);
            
            // foreach (var instance in instances.Values)
            // {
            //     instance.CharacterId = player.CharacterId;
            //     context.CharacterInstance.Add(instance);
            // }
        }
    }
}
