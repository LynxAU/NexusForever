using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Static;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Static;
using NLog;

namespace NexusForever.Game.Marketplace
{
    public class AuctionManager
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Default auction duration in hours (48 hours typical for auction houses)
        /// </summary>
        private const int AuctionDurationHours = 48;

        /// <summary>
        /// Maximum number of auctions a player can have active
        /// </summary>
        private const int MaxAuctionsPerPlayer = 50;

        private readonly IDbContextFactory<CharacterContext> dbContextFactory;

        public AuctionManager(IDbContextFactory<CharacterContext> dbContextFactory)
        {
            this.dbContextFactory = dbContextFactory;
        }

        /// <summary>
        /// Create a new auction listing for an item.
        /// </summary>
        public async Task<(GenericError Result, AuctionInfo Auction)> CreateAuctionAsync(
            IPlayer player,
            ulong itemGuid,
            ulong minimumBid,
            ulong buyoutPrice)
        {
            // Validate player can list items
            if (player == null)
                return (GenericError.PlayerBusy, null);

            // Get the item from player's inventory
            var inventoryItem = player.Inventory.GetItem(itemGuid);
            if (inventoryItem == null)
                return (GenericError.ItemBadId, null);

            // Check if player has too many auctions
            var existingAuctions = await GetAuctionsByOwnerAsync(player.CharacterId);
            if (existingAuctions.Count >= MaxAuctionsPerPlayer)
                return (GenericError.AuctionTooManyOrders, null);

            // Validate minimum bid (must be > 0)
            if (minimumBid == 0)
                return (GenericError.AuctionBidTooLow, null);

            // Create the auction
            var auction = new CharacterAuctionModel
            {
                AuctionId = GenerateAuctionId(),
                Id = player.CharacterId,
                ItemGuid = itemGuid,
                ItemId = inventoryItem.Info.Entry.Id,
                Quantity = inventoryItem.StackCount,
                MinimumBid = minimumBid,
                BuyoutPrice = buyoutPrice,
                CurrentBid = 0,
                OwnerCharacterId = player.CharacterId,
                TopBidderCharacterId = 0,
                ExpirationTime = DateTime.UtcNow.AddHours(AuctionDurationHours),
                CreateTime = DateTime.UtcNow,
                WorldRequirementItem2Id = 0,
                GlyphData = 0,
                ThresholdData = 0,
                CircuitData = 0,
                Unknown2 = 0
            };

            // Remove the item from player's inventory
            // Use the location from the item
            player.Inventory.ItemRemove(inventoryItem, ItemUpdateReason.NoReason);

            // Save to database
            await using var db = await dbContextFactory.CreateDbContextAsync();
            db.CharacterAuction.Add(auction);
            await db.SaveChangesAsync();

            log.Trace($"Created auction {auction.AuctionId} for item {auction.ItemId} by character {player.CharacterId}");

            return (GenericError.Ok, ConvertToAuctionInfo(auction));
        }

        /// <summary>
        /// Cancel an auction and return the item to the owner.
        /// </summary>
        public async Task<(GenericError Result, AuctionInfo Auction)> CancelAuctionAsync(
            IPlayer player,
            ulong auctionId,
            uint itemId)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();
            
            var auction = await db.CharacterAuction.FirstOrDefaultAsync(a => a.AuctionId == auctionId);
            if (auction == null)
                return (GenericError.AuctionNotFound, null);

            // Verify ownership
            if (auction.OwnerCharacterId != player.CharacterId)
                return (GenericError.AuctionOwnItem, null);

            // Can't cancel if there's a current bid
            if (auction.CurrentBid > 0)
                return (GenericError.AuctionAlreadyHasBid, null);

            // Return item to player
            player.Inventory.ItemCreate(InventoryLocation.Inventory, auction.ItemId, auction.Quantity, ItemUpdateReason.NoReason);

            // Delete auction
            db.CharacterAuction.Remove(auction);
            await db.SaveChangesAsync();

            log.Trace($"Cancelled auction {auctionId} by character {player.CharacterId}");

            return (GenericError.Ok, ConvertToAuctionInfo(auction));
        }

        /// <summary>
        /// Place a bid on an auction.
        /// </summary>
        public async Task<(GenericError Result, AuctionInfo Auction)> PlaceBidAsync(
            IPlayer player,
            uint itemId,
            ulong auctionId,
            ulong bidAmount)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();

            var auction = await db.CharacterAuction.FirstOrDefaultAsync(a => a.AuctionId == auctionId);
            if (auction == null)
                return (GenericError.AuctionNotFound, null);

            // Can't bid on own auction
            if (auction.OwnerCharacterId == player.CharacterId)
                return (GenericError.AuctionOwnItem, null);

            // Check if auction has expired
            if (auction.ExpirationTime < DateTime.UtcNow)
                return (GenericError.AuctionNotFound, null);

            // Validate bid amount
            ulong minBid = auction.CurrentBid > 0 ? auction.CurrentBid + 1 : auction.MinimumBid;
            if (bidAmount < minBid)
                return (GenericError.AuctionBidTooLow, null);

            // Check if player can afford the bid
            if (!player.CurrencyManager.CanAfford(CurrencyType.Credits, bidAmount))
                return (GenericError.VendorNotEnoughCash, null);

            // Process the bid
            // Return previous bid to the previous bidder (if any)
            if (auction.CurrentBid > 0 && auction.TopBidderCharacterId != 0)
            {
                // TODO: Mail the previous bidder their bid back
                log.Trace($"Returning bid {auction.CurrentBid} to previous bidder {auction.TopBidderCharacterId}");
            }

            // Deduct bid amount from player
            player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, bidAmount);

            // If this is a buyout (bid >= buyout price)
            if (auction.BuyoutPrice > 0 && bidAmount >= auction.BuyoutPrice)
            {
                // Complete the sale
                return await CompleteAuctionBuyoutAsync(db, auction, player, bidAmount);
            }

            // Update auction with new bid
            auction.CurrentBid = bidAmount;
            auction.TopBidderCharacterId = player.CharacterId;

            // Notify previous top bidder they've been outbid
            // TODO: Send outbid notification

            await db.SaveChangesAsync();

            log.Trace($"New bid {bidAmount} on auction {auctionId} by character {player.CharacterId}");

            return (GenericError.Ok, ConvertToAuctionInfo(auction));
        }

        private async Task<(GenericError Result, AuctionInfo Auction)> CompleteAuctionBuyoutAsync(
            CharacterContext db,
            CharacterAuctionModel auction,
            IPlayer buyer,
            ulong buyoutAmount)
        {
            // Give the item to the buyer
            buyer.Inventory.ItemCreate(InventoryLocation.Inventory, auction.ItemId, auction.Quantity, ItemUpdateReason.NoReason);

            // Give the seller their money (minus a tax - typically 5-10%)
            // For simplicity, we'll take 5% tax
            ulong sellerAmount = (ulong)(buyoutAmount * 0.95);
            var seller = await GetPlayerByIdAsync(db, auction.OwnerCharacterId);
            if (seller != null)
            {
                seller.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, sellerAmount);
            }

            // Delete the auction
            db.CharacterAuction.Remove(auction);
            await db.SaveChangesAsync();

            log.Trace($"Auction {auction.AuctionId} bought out by {buyer.CharacterId} for {buyoutAmount}");

            // Notify the winner
            buyer.Session.EnqueueMessageEncrypted(new ServerAuctionWon
            {
                Auction = ConvertToAuctionInfo(auction)
            });

            return (GenericError.Ok, ConvertToAuctionInfo(auction));
        }

        /// <summary>
        /// Search for auctions by filter criteria.
        /// </summary>
        public async Task<(uint TotalResults, List<AuctionInfo> Auctions)> SearchAuctionsAsync(
            uint? itemFamilyId,
            uint? itemCategoryId,
            uint? itemTypeId,
            List<uint> itemIds,
            Game.Static.Marketplace.AuctionSort sortType,
            Property sortProperty,
            bool reverseSort,
            uint page,
            uint pageSize = 20)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();

            var query = db.CharacterAuction.AsQueryable();

            // Filter by item criteria
            if (itemFamilyId.HasValue)
                query = query.Where(a => GetItemFamilyId(a.ItemId) == itemFamilyId.Value);

            if (itemCategoryId.HasValue)
                query = query.Where(a => GetItemCategoryId(a.ItemId) == itemCategoryId.Value);

            if (itemTypeId.HasValue)
                query = query.Where(a => GetItemTypeId(a.ItemId) == itemTypeId.Value);

            if (itemIds != null && itemIds.Count > 0)
                query = query.Where(a => itemIds.Contains(a.ItemId));

            // Only show active auctions
            query = query.Where(a => a.ExpirationTime > DateTime.UtcNow);

            // Get total count before pagination
            uint totalCount = (uint)query.Count();

            // Apply sorting - simplified for now
            query = sortType switch
            {
                Game.Static.Marketplace.AuctionSort.MinBid => reverseSort
                    ? query.OrderByDescending(a => a.MinimumBid)
                    : query.OrderBy(a => a.MinimumBid),
                Game.Static.Marketplace.AuctionSort.Buyout => reverseSort
                    ? query.OrderByDescending(a => a.BuyoutPrice)
                    : query.OrderBy(a => a.BuyoutPrice),
                Game.Static.Marketplace.AuctionSort.TimeLeft => reverseSort
                    ? query.OrderByDescending(a => a.ExpirationTime)
                    : query.OrderBy(a => a.ExpirationTime),
                _ => query.OrderBy(a => a.ExpirationTime)
            };

            // Apply pagination
            var auctions = await query
                .Skip((int)(page * pageSize))
                .Take((int)pageSize)
                .ToListAsync();

            return (totalCount, auctions.Select(ConvertToAuctionInfo).ToList());
        }

        /// <summary>
        /// Get all auctions owned by a character.
        /// </summary>
        public async Task<List<AuctionInfo>> GetOwnedAuctionsAsync(ulong characterId)
        {
            var auctions = await GetAuctionsByOwnerAsync(characterId);
            return auctions.Select(ConvertToAuctionInfo).ToList();
        }

        private async Task<List<CharacterAuctionModel>> GetAuctionsByOwnerAsync(ulong characterId)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();
            return await db.CharacterAuction
                .Where(a => a.OwnerCharacterId == characterId && a.ExpirationTime > DateTime.UtcNow)
                .ToListAsync();
        }

        /// <summary>
        /// Process expired auctions - return items to owners or finalize sales.
        /// </summary>
        public async Task ProcessExpiredAuctionsAsync()
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();

            var expiredAuctions = await db.CharacterAuction
                .Where(a => a.ExpirationTime <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var auction in expiredAuctions)
            {
                if (auction.CurrentBid > 0 && auction.TopBidderCharacterId != 0)
                {
                    // Auction was bid on - transfer item to winner and money to seller
                    await FinalizeExpiredAuctionAsync(db, auction);
                }
                else
                {
                    // No bids - return item to owner
                    await ReturnExpiredItemAsync(db, auction);
                }
            }

            if (expiredAuctions.Count > 0)
            {
                log.Trace($"Processed {expiredAuctions.Count} expired auctions");
            }
        }

        private async Task FinalizeExpiredAuctionAsync(CharacterContext db, CharacterAuctionModel auction)
        {
            // Get the winner
            var winner = await GetPlayerByIdAsync(db, auction.TopBidderCharacterId);
            if (winner != null)
            {
                // Give item to winner
                winner.Inventory.ItemCreate(InventoryLocation.Inventory, auction.ItemId, auction.Quantity, ItemUpdateReason.NoReason);
            }

            // Give seller their money (minus 5% tax)
            ulong sellerAmount = (ulong)(auction.CurrentBid * 0.95);
            var seller = await GetPlayerByIdAsync(db, auction.OwnerCharacterId);
            if (seller != null)
            {
                seller.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, sellerAmount);
            }

            // Notify winner
            if (winner != null)
            {
                winner.Session.EnqueueMessageEncrypted(new ServerAuctionWon
                {
                    Auction = ConvertToAuctionInfo(auction)
                });
            }

            // Notify seller
            if (seller != null)
            {
                seller.Session.EnqueueMessageEncrypted(new ServerAuctionExpired
                {
                    Auction = ConvertToAuctionInfo(auction)
                });
            }

            db.CharacterAuction.Remove(auction);
            await db.SaveChangesAsync();

            log.Trace($"Finalized expired auction {auction.AuctionId} - winner: {auction.TopBidderCharacterId}, amount: {auction.CurrentBid}");
        }

        private async Task ReturnExpiredItemAsync(CharacterContext db, CharacterAuctionModel auction)
        {
            var owner = await GetPlayerByIdAsync(db, auction.OwnerCharacterId);
            if (owner != null)
            {
                // Return the item to the owner
                owner.Inventory.ItemCreate(InventoryLocation.Inventory, auction.ItemId, auction.Quantity, ItemUpdateReason.NoReason);

                // Notify owner
                owner.Session.EnqueueMessageEncrypted(new ServerAuctionExpired
                {
                    Auction = ConvertToAuctionInfo(auction)
                });
            }

            db.CharacterAuction.Remove(auction);
            await db.SaveChangesAsync();

            log.Trace($"Returned expired auction {auction.AuctionId} item to owner {auction.OwnerCharacterId}");
        }

        private async Task<IPlayer> GetPlayerByIdAsync(CharacterContext db, ulong characterId)
        {
            // This is a simplified version - in a real implementation we'd need access to the player session
            // For now, we'll return null and handle the case where the player is offline
            return null;
        }

        private uint GetItemFamilyId(uint itemId)
        {
            // For now return 0 - need proper GameTable access
            return 0;
        }

        private uint GetItemCategoryId(uint itemId)
        {
            // For now return 0 - need proper GameTable access
            return 0;
        }

        private uint GetItemTypeId(uint itemId)
        {
            // For now return 0 - need proper GameTable access
            return 0;
        }

        private ulong GenerateAuctionId()
        {
            // Generate a unique auction ID based on timestamp and random component
            return ((ulong)DateTime.UtcNow.Subtract(new DateTime(2020, 1, 1)).TotalSeconds << 32) | (ulong)Random.Shared.Next(int.MaxValue);
        }

        private AuctionInfo ConvertToAuctionInfo(CharacterAuctionModel model)
        {
            var info = new AuctionInfo
            {
                AuctionId = model.AuctionId,
                OwnerCharacterId = model.OwnerCharacterId,
                MinimumBid = model.MinimumBid,
                BuyoutPrice = model.BuyoutPrice,
                CurrentBid = model.CurrentBid,
                TopBidderCharacterId = model.TopBidderCharacterId,
                ExpirationTime = (ulong)(model.ExpirationTime - DateTime.UtcNow).TotalSeconds,
                Item2Id = model.ItemId,
                Quantity = model.Quantity,
                WorldRequirement_Item2Id = model.WorldRequirementItem2Id,
                GlyphData = model.GlyphData,
                ThresholdData = model.ThresholdData,
                CircuitData = model.CircuitData,
                Unknown2 = model.Unknown2
            };

            return info;
        }
    }
}
