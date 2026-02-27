using NexusForever.Game.Marketplace;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Marketplace
{
    public class ClientAuctionCancelHandler : IMessageHandler<IWorldSession, ClientAuctionCancel>
    {
        private readonly AuctionManager auctionManager;

        public ClientAuctionCancelHandler(AuctionManager auctionManager)
        {
            this.auctionManager = auctionManager;
        }

        public void HandleMessage(IWorldSession session, ClientAuctionCancel packet)
        {
            var player = session.Player;
            if (player == null)
                return;

            var (result, auction) = auctionManager.CancelAuctionAsync(
                player,
                packet.AuctionId,
                packet.Item2Id).Result;

            player.Session.EnqueueMessageEncrypted(new ServerAuctionCancelResult
            {
                Result = result,
                Auction = auction
            });
        }
    }
}
