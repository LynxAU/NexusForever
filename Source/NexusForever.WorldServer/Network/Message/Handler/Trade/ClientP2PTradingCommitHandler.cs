using NexusForever.Game.Abstract.Trade;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Trade
{
    public class ClientP2PTradingCommitHandler : IMessageHandler<IWorldSession, ClientP2PTradingCommit>
    {
        private readonly ITradeManager tradeManager;

        public ClientP2PTradingCommitHandler(ITradeManager tradeManager)
        {
            this.tradeManager = tradeManager;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingCommit message)
        {
            tradeManager.CommitTrade(session.Player);
        }
    }
}
