using NexusForever.Game.Abstract.Trade;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Trade
{
    public class ClientP2PTradingSetMoneyHandler : IMessageHandler<IWorldSession, ClientP2PTradingSetMoney>
    {
        private readonly ITradeManager tradeManager;

        public ClientP2PTradingSetMoneyHandler(ITradeManager tradeManager)
        {
            this.tradeManager = tradeManager;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingSetMoney message)
        {
            tradeManager.SetMoney(session.Player, message.Credits);
        }
    }
}
