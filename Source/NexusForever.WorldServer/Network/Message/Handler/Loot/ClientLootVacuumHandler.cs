using NexusForever.Game.Loot;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.WorldServer.Network;

namespace NexusForever.WorldServer.Network.Message.Handler.Loot
{
    public class ClientLootVacuumHandler : IMessageHandler<IWorldSession, ClientLootVacuum>
    {
        public void HandleMessage(IWorldSession session, ClientLootVacuum lootVacuum)
        {
            if (session.Player == null)
                return;

            LootRollManager.Instance.HandleVacuum(session.Player);
        }
    }
}
