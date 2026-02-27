using NexusForever.Game.Abstract.Trade;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Trade
{
    public class ClientP2PTradingRemoveItemHandler : IMessageHandler<IWorldSession, ClientP2PTradingRemoveItem>
    {
        private readonly ITradeManager tradeManager;

        public ClientP2PTradingRemoveItemHandler(ITradeManager tradeManager)
        {
            this.tradeManager = tradeManager;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingRemoveItem message)
        {
            tradeManager.RemoveItem(session.Player, message.ItemGuid);
        }
    }
}
