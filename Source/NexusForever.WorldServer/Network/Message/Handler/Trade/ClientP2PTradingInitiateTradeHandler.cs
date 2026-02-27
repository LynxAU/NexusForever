using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Trade;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Trade
{
    public class ClientP2PTradingInitiateTradeHandler : IMessageHandler<IWorldSession, ClientP2PTradingInitiateTrade>
    {
        private readonly ITradeManager tradeManager;

        public ClientP2PTradingInitiateTradeHandler(ITradeManager tradeManager)
        {
            this.tradeManager = tradeManager;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingInitiateTrade message)
        {
            // Target is looked up by their entity Guid (uint) which is their in-world unit ID.
            IPlayer target = session.Player.GetVisible<IPlayer>(message.TargetUnitId);
            if (target == null)
                return;

            tradeManager.InitiateTrade(session.Player, target);
        }
    }
}
