using NexusForever.Game.Abstract.Trade;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Trade
{
    public class ClientP2PTradingDeclineInviteHandler : IMessageHandler<IWorldSession, ClientP2PTradingDeclineInvite>
    {
        private readonly ITradeManager tradeManager;

        public ClientP2PTradingDeclineInviteHandler(ITradeManager tradeManager)
        {
            this.tradeManager = tradeManager;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingDeclineInvite message)
        {
            tradeManager.DeclineTrade(session.Player);
        }
    }
}
