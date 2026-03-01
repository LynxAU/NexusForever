using NexusForever.Game.Loot;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.WorldServer.Network;

namespace NexusForever.WorldServer.Network.Message.Handler.Loot
{
    public class ClientLootRollActionHandler : IMessageHandler<IWorldSession, ClientLootRollAction>
    {
        public void HandleMessage(IWorldSession session, ClientLootRollAction lootRollAction)
        {
            if (session.Player == null)
                return;

            LootRollManager.Instance.HandleRollAction(
                session.Player,
                lootRollAction.OwnerUnitId,
                lootRollAction.LootUnitId,
                lootRollAction.Action);
        }
    }
}
