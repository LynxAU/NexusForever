using System.Collections.Generic;
using System.Linq;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Quest;

namespace NexusForever.Game.Entity
{
    public class VirtualItemManager : IVirtualItemManager
    {
        private readonly IPlayer player;
        private readonly Dictionary<ushort, CharacterVirtualItemModel> virtualItems = new();

        public VirtualItemManager(IPlayer player)
        {
            this.player = player;
        }

        /// <summary>
        /// Initialise virtual item manager with data from database.
        /// </summary>
        public void Initialise(IEnumerable<CharacterVirtualItemModel> dbVirtualItems)
        {
            virtualItems.Clear();
            foreach (var virtualItem in dbVirtualItems)
            {
                virtualItems[virtualItem.VirtualItemId] = virtualItem;
            }
        }

        /// <summary>
        /// Get the count of a virtual item.
        /// </summary>
        public uint GetVirtualItemCount(ushort virtualItemId)
        {
            return virtualItems.TryGetValue(virtualItemId, out var virtualItem) ? virtualItem.Count : 0u;
        }

        /// <summary>
        /// Add count to a virtual item and return the new total.
        /// </summary>
        public uint AddVirtualItem(ushort virtualItemId, uint count)
        {
            if (!virtualItems.TryGetValue(virtualItemId, out var virtualItem))
            {
                virtualItem = new CharacterVirtualItemModel
                {
                    Id = player.CharacterId,
                    VirtualItemId = virtualItemId,
                    Count = 0
                };
                virtualItems[virtualItemId] = virtualItem;
            }

            uint oldCount = virtualItem.Count;
            virtualItem.Count += count;

            // Update quest objectives for VirtualCollect
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.VirtualCollect, virtualItemId, virtualItem.Count);

            return virtualItem.Count;
        }

        /// <summary>
        /// Save virtual item data to the provided CharacterContext.
        /// </summary>
        public void Save(CharacterContext context)
        {
            // Remove all existing virtual item entries for this character
            var existingVirtualItems = context.CharacterVirtualItem.Where(v => v.Id == player.CharacterId);
            context.CharacterVirtualItem.RemoveRange(existingVirtualItems);

            // Add all current virtual items
            foreach (var virtualItem in virtualItems.Values)
            {
                virtualItem.Id = player.CharacterId;
                context.CharacterVirtualItem.Add(virtualItem);
            }
        }
    }
}
