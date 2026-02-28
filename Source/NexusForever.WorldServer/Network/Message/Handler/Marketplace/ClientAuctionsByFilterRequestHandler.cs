using System;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Item;
using NexusForever.Game.Static.Marketplace;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Model.Marketplace.Filter;

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

            // Extract secondary filters from packet.Filters
            ulong? buyoutMax      = null;
            uint?  levelMin       = null;
            uint?  levelMax       = null;
            Quality? qualityMin   = null;
            Quality? qualityMax   = null;
            uint?  raceRequired   = null;
            uint?  classRequired  = null;

            foreach (IAuctionFilter filter in packet.Filters)
            {
                switch (filter)
                {
                    case BuyoutMaxAuctionFilter f:
                        buyoutMax = f.BuyoutPrice;
                        break;
                    case LevelAuctionFilter f:
                        if (f.Minimum > 0) levelMin = f.Minimum;
                        if (f.Maximum > 0) levelMax = f.Maximum;
                        break;
                    case QualityAuctionFilter f:
                        qualityMin = f.Minimum;
                        qualityMax = f.Maximum;
                        break;
                    case EquippableByAuctionFilter f:
                        if (f.RaceId  != 0) raceRequired  = (uint)f.RaceId;
                        if (f.ClassId != 0) classRequired = (uint)f.ClassId;
                        if (f.Level   > 0)  levelMax      = levelMax.HasValue ? Math.Min(levelMax.Value, f.Level) : f.Level;
                        break;
                    // PropertyMin/Max and RuneSlot require item stat data — not implemented yet
                }
            }

            var (totalResults, auctions) = auctionManager.SearchAuctionsAsync(
                packet.Item2FamilyId   > 0 ? packet.Item2FamilyId   : null,
                packet.Item2CategoryId > 0 ? packet.Item2CategoryId : null,
                packet.Item2TypeId     > 0 ? packet.Item2TypeId     : null,
                packet.Item2Ids.Count  > 0 ? packet.Item2Ids        : null,
                packet.AuctionSort,
                packet.SortPropertyId,
                packet.ReverseSort > 0,
                packet.Page,
                buyoutMax:     buyoutMax,
                levelMin:      levelMin,
                levelMax:      levelMax,
                qualityMin:    qualityMin,
                qualityMax:    qualityMax,
                raceRequired:  raceRequired,
                classRequired: classRequired).Result;

            player.Session.EnqueueMessageEncrypted(new ServerAuctionSearchResults
            {
                TotalResultCount = totalResults,
                CurrentPage      = packet.Page,
                Auctions         = auctions
            });
        }
    }
}
