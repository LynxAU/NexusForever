using NexusForever.Game.Marketplace;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Marketplace
{
    public class ClientAuctionBuyOrderSubmitHandler : IMessageHandler<IWorldSession, ClientAuctionBuyOrderSubmit>
    {
        private readonly AuctionManager auctionManager;

        public ClientAuctionBuyOrderSubmitHandler(AuctionManager auctionManager)
        {
            this.auctionManager = auctionManager;
        }

        public void HandleMessage(IWorldSession session, ClientAuctionBuyOrderSubmit packet)
        {
            var player = session.Player;
            if (player == null)
                return;

            var (result, auction) = auctionManager.PlaceBidAsync(
                player,
                packet.Item2Id,
                packet.AuctionId,
                packet.AmountOffered).Result;

            player.Session.EnqueueMessageEncrypted(new ServerAuctionBidResult
            {
                Result = result,
                Auction = auction
            });
        }
    }
}
