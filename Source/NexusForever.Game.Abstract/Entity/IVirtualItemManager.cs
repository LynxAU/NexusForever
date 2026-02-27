using System.Collections.Generic;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Abstract.Entity
{
    public interface IVirtualItemManager : IDatabaseCharacter
    {
        /// <summary>
        /// Initialise virtual item manager with data from database.
        /// </summary>
        void Initialise(IEnumerable<CharacterVirtualItemModel> dbVirtualItems);

        /// <summary>
        /// Get the count of a virtual item.
        /// </summary>
        uint GetVirtualItemCount(ushort virtualItemId);

        /// <summary>
        /// Add count to a virtual item and return the new total.
        /// </summary>
        uint AddVirtualItem(ushort virtualItemId, uint count);
    }
}
