using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.GenericUnlock;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.GenericUnlock;

namespace NexusForever.WorldServer.Network.Message.Handler.Item
{
    public class ClientItemGenericUnlockHandler : IMessageHandler<IWorldSession, ClientItemGenericUnlock>
    {
        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;

        public ClientItemGenericUnlockHandler(
            IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientItemGenericUnlock itemGenericUnlock)
        {
            IItem item = session.Player.Inventory.GetItem(itemGenericUnlock.Location);
            if (item == null)
                throw new InvalidPacketValueException();

            GenericUnlockEntryEntry entry = gameTableManager.GenericUnlockEntry.GetEntry(item.Info.Entry.GenericUnlockSetId);
            if (entry == null)
                throw new InvalidPacketValueException();

            // If this unlock is already owned, tell the client and don't consume the item.
            if (session.Account.GenericUnlockManager.IsUnlocked(entry.GenericUnlockTypeEnum, entry.UnlockObject))
            {
                session.Account.GenericUnlockManager.SendUnlockResult(GenericUnlockResult.AlreadyUnlocked);
                return;
            }

            if (session.Player.Inventory.ItemUse(item))
                session.Account.GenericUnlockManager.Unlock((ushort)entry.Id);
        }
    }
}
