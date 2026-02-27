using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Marketplace;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Marketplace;

namespace NexusForever.WorldServer.Network.Message.Handler.Marketplace
{
    public class ClientAuctionsByFilterRequestHandler : IMessageHandler<IWorldSession, ClientAuctionsByFilterRequest>
    {
        private readonly AuctionManager auctionManager;

        public ClientAuctionsByFilterRequestHandler(AuctionManager auctionManager)
        {
            this.auctionManager = auctionManager;
        }

        public void HandleMessage(IWorldSession session, ClientAuctionsByFilterRequest packet)
        {
            var player = session.Player;
            if (player == null)
                return;

            // TODO: Apply filters from packet.Filters
            // For now, we'll do basic search

            var (totalResults, auctions) = auctionManager.SearchAuctionsAsync(
                packet.Item2FamilyId > 0 ? packet.Item2FamilyId : null,
                packet.Item2CategoryId > 0 ? packet.Item2CategoryId : null,
                packet.Item2TypeId > 0 ? packet.Item2TypeId : null,
                packet.Item2Ids.Count > 0 ? packet.Item2Ids : null,
                packet.AuctionSort,
                packet.SortPropertyId,
                packet.ReverseSort > 0,
                packet.Page).Result;

            player.Session.EnqueueMessageEncrypted(new ServerAuctionSearchResults
            {
                TotalResultCount = totalResults,
                CurrentPage = packet.Page,
                Auctions = auctions
            });
        }
    }
}
