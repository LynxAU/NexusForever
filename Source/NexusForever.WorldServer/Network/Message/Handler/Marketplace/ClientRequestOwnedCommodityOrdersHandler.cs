using System.Threading.Tasks;
using NexusForever.Game.Marketplace;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Marketplace;

namespace NexusForever.WorldServer.Network.Message.Handler.Marketplace
{
    public class ClientRequestOwnedCommodityOrdersHandler : IMessageHandler<IWorldSession, ClientRequestOwnedCommodityOrders>
    {
        private readonly CommodityExchangeManager commodityExchangeManager;

        public ClientRequestOwnedCommodityOrdersHandler(CommodityExchangeManager commodityExchangeManager)
        {
            this.commodityExchangeManager = commodityExchangeManager;
        }

        public async Task HandleMessage(IWorldSession session, ClientRequestOwnedCommodityOrders packet)
        {
            var player = session.Player;
            if (player == null)
                return;

            var orders = await commodityExchangeManager.GetOwnedOrdersAsync(player.CharacterId);

            player.Session.EnqueueMessageEncrypted(new ServerOwnedCommodityOrders
            {
                Orders = orders
            });
        }
    }
}
</invoke>