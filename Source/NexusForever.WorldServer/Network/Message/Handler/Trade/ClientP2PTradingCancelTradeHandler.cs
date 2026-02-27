using NexusForever.Game.Abstract.Trade;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Trade
{
    public class ClientP2PTradingCancelTradeHandler : IMessageHandler<IWorldSession, ClientP2PTradingCancelTrade>
    {
        private readonly ITradeManager tradeManager;

        public ClientP2PTradingCancelTradeHandler(ITradeManager tradeManager)
        {
            this.tradeManager = tradeManager;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingCancelTrade message)
        {
            tradeManager.CancelTrade(session.Player);
        }
    }
}
