using NexusForever.Game.Loot;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.WorldServer.Network;

namespace NexusForever.WorldServer.Network.Message.Handler.Loot
{
    public class ClientLootAssignMasterHandler : IMessageHandler<IWorldSession, ClientLootAssignMaster>
    {
        public void HandleMessage(IWorldSession session, ClientLootAssignMaster assignMaster)
        {
            if (session.Player == null)
                return;

            LootRollManager.Instance.HandleAssignMaster(
                session.Player,
                assignMaster.OwnerUnitId,
                assignMaster.LootUnitId,
                assignMaster.Assignee.Id);
        }
    }
}
