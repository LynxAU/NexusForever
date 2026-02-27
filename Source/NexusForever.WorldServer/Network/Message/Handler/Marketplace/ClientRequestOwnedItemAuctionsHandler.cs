using NexusForever.Game.Marketplace;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Marketplace;

namespace NexusForever.WorldServer.Network.Message.Handler.Marketplace
{
    public class ClientRequestOwnedItemAuctionsHandler : IMessageHandler<IWorldSession, ClientRequestOwnedItemAuctions>
    {
        private readonly AuctionManager auctionManager;

        public ClientRequestOwnedItemAuctionsHandler(AuctionManager auctionManager)
        {
            this.auctionManager = auctionManager;
        }

        public void HandleMessage(IWorldSession session, ClientRequestOwnedItemAuctions packet)
        {
            var player = session.Player;
            if (player == null)
                return;

            var auctions = auctionManager.GetOwnedAuctionsAsync(player.CharacterId).Result;

            player.Session.EnqueueMessageEncrypted(new ServerOwnedItemAuctions
            {
                Auctions = auctions
            });
        }
    }
}
