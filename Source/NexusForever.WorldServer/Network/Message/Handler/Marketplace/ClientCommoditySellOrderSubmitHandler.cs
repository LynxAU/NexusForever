using NexusForever.Game.Marketplace;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Marketplace
{
    /// <summary>
    /// Handles commodity buy and sell order submissions.
    /// </summary>
    public class ClientCommoditySellOrderSubmitHandler : IMessageHandler<IWorldSession, ClientCommoditySellOrderSubmit>
    {
        private readonly CommodityExchangeManager commodityExchangeManager;

        public ClientCommoditySellOrderSubmitHandler(CommodityExchangeManager commodityExchangeManager)
        {
            this.commodityExchangeManager = commodityExchangeManager;
        }

        public void HandleMessage(IWorldSession session, ClientCommoditySellOrderSubmit packet)
        {
            var player = session.Player;
            if (player == null)
                return;

            var (result, order) = commodityExchangeManager.CreateOrderAsync(
                player,
                packet.Order.Item2Id,
                packet.Order.Quantity,
                packet.Order.PricePerUnit,
                packet.Order.IsBuyOrder,
                packet.Order.ForceImmediate)
                .GetAwaiter()
                .GetResult();

            player.Session.EnqueueMessageEncrypted(new ServerCommodityOrderResult
            {
                Result = result,
                OrderPosted = order ?? new CommodityOrder(),
                CostToPostOrder = 0 // No posting fee for commodities
            });
        }
    }
}
