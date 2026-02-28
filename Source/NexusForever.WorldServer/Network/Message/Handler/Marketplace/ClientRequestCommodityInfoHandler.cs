using System.Threading.Tasks;
using NexusForever.Game.Marketplace;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Marketplace;

namespace NexusForever.WorldServer.Network.Message.Handler.Marketplace
{
    public class ClientRequestCommodityInfoHandler : IMessageHandler<IWorldSession, ClientRequestCommodityInfo>
    {
        private readonly CommodityExchangeManager commodityExchangeManager;

        public ClientRequestCommodityInfoHandler(CommodityExchangeManager commodityExchangeManager)
        {
            this.commodityExchangeManager = commodityExchangeManager;
        }

        public async Task HandleMessage(IWorldSession session, ClientRequestCommodityInfo packet)
        {
            var player = session.Player;
            if (player == null)
                return;

            var info = await commodityExchangeManager.GetCommodityInfoAsync(packet.Item2Id);
            player.Session.EnqueueMessageEncrypted(info);
        }
    }
}
