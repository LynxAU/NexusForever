using NexusForever.Game.Abstract.Trade;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Trade
{
    public class ClientP2PTradingAddItemHandler : IMessageHandler<IWorldSession, ClientP2PTradingAddItem>
    {
        private readonly ITradeManager tradeManager;

        public ClientP2PTradingAddItemHandler(ITradeManager tradeManager)
        {
            this.tradeManager = tradeManager;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingAddItem message)
        {
            var item = session.Player.Inventory.GetItem(message.ItemGuid);
            if (item == null)
                return;

            tradeManager.AddItem(session.Player, item.BagIndex, item.StackCount);
        }
    }
}
