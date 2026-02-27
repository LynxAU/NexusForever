using System.Threading.Tasks;
using NexusForever.Game.Marketplace;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network.Message.Handler;

namespace NexusForever.WorldServer.Network.Message.Handler.Marketplace
{
    public class ClientAuctionSellOrderSubmitHandler : IMessageHandler<IWorldSession, ClientAuctionSellOrderSubmit>
    {
        private readonly AuctionManager auctionManager;

        public ClientAuctionSellOrderSubmitHandler(AuctionManager auctionManager)
        {
            this.auctionManager = auctionManager;
        }

        public void HandleMessage(IWorldSession session, ClientAuctionSellOrderSubmit packet)
        {
            var player = session.Player;
            if (player == null)
                return;

            var (result, auction) = auctionManager.CreateAuctionAsync(
                player,
                packet.ItemGuid,
                packet.MinimumBid,
                packet.BuyoutPrice).Result;

            player.Session.EnqueueMessageEncrypted(new ServerAuctionPostResult
            {
                Result = result,
                Auction = auction
            });
        }
    }
}
