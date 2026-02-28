using System.Threading.Tasks;
using NexusForever.Game.Marketplace;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Marketplace
{
    public class ClientCommodityOrderCancelHandler : IMessageHandler<IWorldSession, ClientCommodityOrderCancel>
    {
        private readonly CommodityExchangeManager commodityExchangeManager;

        public ClientCommodityOrderCancelHandler(CommodityExchangeManager commodityExchangeManager)
        {
            this.commodityExchangeManager = commodityExchangeManager;
        }

        public async Task HandleMessage(IWorldSession session, ClientCommodityOrderCancel packet)
        {
            var player = session.Player;
            if (player == null)
                return;

            var (result, order) = await commodityExchangeManager.CancelOrderAsync(
                player,
                packet.CommodityOrderId,
                packet.Item2Id);

            // Send result back to client
            // Note: Cancel result format might need adjustment based on actual protocol
            // Using ServerAuctionCancelResult as a base since commodity orders follow similar patterns
            // If there's a specific ServerCommodityCancelResult, that should be used instead
        }
    }
}
