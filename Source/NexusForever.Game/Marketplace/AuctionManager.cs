using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Game;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Item;
using NexusForever.Game.Static.Mail;
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
            // Return previous bid to the previous bidder (if any) before updating auction state.
            if (auction.CurrentBid > 0 && auction.TopBidderCharacterId != 0)
            {
                IPlayer previousBidder = GetOnlinePlayer(auction.TopBidderCharacterId);
                if (previousBidder != null)
                {
                    previousBidder.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, auction.CurrentBid);
                    previousBidder.Session.EnqueueMessageEncrypted(new ServerAuctionOutbid
                    {
                        Auction = ConvertToAuctionInfo(auction)
                    });
                }
                else
                {
                    MailCreditsToCharacter(db, auction.TopBidderCharacterId, auction.CurrentBid,
                        "Auction Outbid", "You were outbid on an auction. Your bid has been returned.");
                }
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
            IPlayer seller = GetOnlinePlayer(auction.OwnerCharacterId);
            if (seller != null)
                seller.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, sellerAmount);
            else
                MailCreditsToCharacter(db, auction.OwnerCharacterId, sellerAmount,
                    "Auction Sold", "Your auction item was purchased. Credits enclosed.");

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
            uint pageSize = 20,
            ulong? buyoutMax = null,
            uint? levelMin = null,
            uint? levelMax = null,
            Quality? qualityMin = null,
            Quality? qualityMax = null,
            uint? raceRequired = null,
            uint? classRequired = null)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();

            // Fetch all active auctions in one DB query, then filter in-memory using GameTable lookups.
            // This avoids EF Core translation failures for custom method calls.
            var now = DateTime.UtcNow;
            var allActive = await db.CharacterAuction
                .Where(a => a.ExpirationTime > now)
                .ToListAsync();

            IEnumerable<CharacterAuctionModel> filtered = allActive;

            // Buyout cap can be evaluated directly (DB field available)
            if (buyoutMax.HasValue)
                filtered = filtered.Where(a => a.BuyoutPrice == 0 || a.BuyoutPrice <= buyoutMax.Value);

            // GameTable-backed in-memory filters
            if (itemFamilyId.HasValue)
                filtered = filtered.Where(a => GetItemFamilyId(a.ItemId) == itemFamilyId.Value);

            if (itemCategoryId.HasValue)
                filtered = filtered.Where(a => GetItemCategoryId(a.ItemId) == itemCategoryId.Value);

            if (itemTypeId.HasValue)
                filtered = filtered.Where(a => GetItemTypeId(a.ItemId) == itemTypeId.Value);

            if (itemIds != null && itemIds.Count > 0)
                filtered = filtered.Where(a => itemIds.Contains(a.ItemId));

            if (levelMin.HasValue || levelMax.HasValue)
            {
                filtered = filtered.Where(a =>
                {
                    uint req = GameTableManager.Instance.Item.GetEntry(a.ItemId)?.RequiredLevel ?? 0u;
                    return (!levelMin.HasValue || req >= levelMin.Value)
                        && (!levelMax.HasValue || req <= levelMax.Value);
                });
            }

            if (qualityMin.HasValue || qualityMax.HasValue)
            {
                filtered = filtered.Where(a =>
                {
                    uint qualityId = GameTableManager.Instance.Item.GetEntry(a.ItemId)?.ItemQualityId ?? 0u;
                    return (!qualityMin.HasValue || qualityId >= (uint)qualityMin.Value)
                        && (!qualityMax.HasValue || qualityId <= (uint)qualityMax.Value);
                });
            }

            if (raceRequired.HasValue && raceRequired.Value != 0)
            {
                filtered = filtered.Where(a =>
                {
                    uint race = GameTableManager.Instance.Item.GetEntry(a.ItemId)?.RaceRequired ?? 0u;
                    return race == 0 || race == raceRequired.Value;
                });
            }

            if (classRequired.HasValue && classRequired.Value != 0)
            {
                filtered = filtered.Where(a =>
                {
                    uint cls = GameTableManager.Instance.Item.GetEntry(a.ItemId)?.ClassRequired ?? 0u;
                    return cls == 0 || cls == classRequired.Value;
                });
            }

            var list = filtered.ToList();
            uint totalCount = (uint)list.Count;

            // Apply sorting
            list = (sortType switch
            {
                Game.Static.Marketplace.AuctionSort.MinBid => reverseSort
                    ? list.OrderByDescending(a => a.MinimumBid)
                    : list.OrderBy(a => a.MinimumBid),
                Game.Static.Marketplace.AuctionSort.Buyout => reverseSort
                    ? list.OrderByDescending(a => a.BuyoutPrice)
                    : list.OrderBy(a => a.BuyoutPrice),
                Game.Static.Marketplace.AuctionSort.TimeLeft => reverseSort
                    ? list.OrderByDescending(a => a.ExpirationTime)
                    : list.OrderBy(a => a.ExpirationTime),
                _ => list.OrderBy(a => a.ExpirationTime)
            }).ToList();

            // Paginate
            var page_results = list
                .Skip((int)(page * pageSize))
                .Take((int)pageSize)
                .ToList();

            return (totalCount, page_results.Select(ConvertToAuctionInfo).ToList());
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
            IPlayer winner = GetOnlinePlayer(auction.TopBidderCharacterId);
            if (winner != null)
            {
                winner.Inventory.ItemCreate(InventoryLocation.Inventory, auction.ItemId, auction.Quantity, ItemUpdateReason.NoReason);
                winner.Session.EnqueueMessageEncrypted(new ServerAuctionWon
                {
                    Auction = ConvertToAuctionInfo(auction)
                });
            }
            else
            {
                MailItemToCharacter(db, auction.TopBidderCharacterId, auction.ItemGuid,
                    "Auction Won", "You won an auction. The item is attached.");
            }

            // Give seller their credits (minus 5% tax)
            ulong sellerAmount = (ulong)(auction.CurrentBid * 0.95);
            IPlayer seller = GetOnlinePlayer(auction.OwnerCharacterId);
            if (seller != null)
            {
                seller.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, sellerAmount);
                seller.Session.EnqueueMessageEncrypted(new ServerAuctionExpired
                {
                    Auction = ConvertToAuctionInfo(auction)
                });
            }
            else
            {
                MailCreditsToCharacter(db, auction.OwnerCharacterId, sellerAmount,
                    "Auction Sold", "Your auction has ended. Credits enclosed.");
            }

            db.CharacterAuction.Remove(auction);
            await db.SaveChangesAsync();

            log.Trace($"Finalized expired auction {auction.AuctionId} - winner: {auction.TopBidderCharacterId}, amount: {auction.CurrentBid}");
        }

        private async Task ReturnExpiredItemAsync(CharacterContext db, CharacterAuctionModel auction)
        {
            IPlayer owner = GetOnlinePlayer(auction.OwnerCharacterId);
            if (owner != null)
            {
                owner.Inventory.ItemCreate(InventoryLocation.Inventory, auction.ItemId, auction.Quantity, ItemUpdateReason.NoReason);
                owner.Session.EnqueueMessageEncrypted(new ServerAuctionExpired
                {
                    Auction = ConvertToAuctionInfo(auction)
                });
            }
            else
            {
                MailItemToCharacter(db, auction.OwnerCharacterId, auction.ItemGuid,
                    "Auction Expired", "Your auction expired with no bids. The item is attached.");
            }

            db.CharacterAuction.Remove(auction);
            await db.SaveChangesAsync();

            log.Trace($"Returned expired auction {auction.AuctionId} item to owner {auction.OwnerCharacterId}");
        }

        private static IPlayer GetOnlinePlayer(ulong characterId)
        {
            return PlayerManager.Instance.GetPlayer(characterId);
        }

        private static void MailCreditsToCharacter(CharacterContext db, ulong recipientId, ulong credits, string subject, string body)
        {
            db.CharacterMail.Add(new CharacterMailModel
            {
                Id             = AssetManager.Instance.NextMailId,
                RecipientId    = recipientId,
                SenderType     = (byte)SenderType.ItemAuction,
                Subject        = subject,
                Message        = body,
                CurrencyType   = (byte)CurrencyType.Credits,
                CurrencyAmount = credits,
                DeliveryTime   = (byte)DeliverySpeed.Instant,
                CreateTime     = DateTime.UtcNow
            });
        }

        private static void MailItemToCharacter(CharacterContext db, ulong recipientId, ulong itemGuid, string subject, string body)
        {
            ulong mailId = AssetManager.Instance.NextMailId;

            var mail = new CharacterMailModel
            {
                Id          = mailId,
                RecipientId = recipientId,
                SenderType  = (byte)SenderType.ItemAuction,
                Subject     = subject,
                Message     = body,
                DeliveryTime = (byte)DeliverySpeed.Instant,
                CreateTime  = DateTime.UtcNow
            };

            mail.Attachment.Add(new CharacterMailAttachmentModel
            {
                Id       = mailId,
                Index    = 0,
                ItemGuid = itemGuid
            });

            db.CharacterMail.Add(mail);
        }

        private uint GetItemFamilyId(uint itemId)
        {
            return GameTableManager.Instance.Item.GetEntry(itemId)?.Item2FamilyId ?? 0u;
        }

        private uint GetItemCategoryId(uint itemId)
        {
            return GameTableManager.Instance.Item.GetEntry(itemId)?.Item2CategoryId ?? 0u;
        }

        private uint GetItemTypeId(uint itemId)
        {
            return GameTableManager.Instance.Item.GetEntry(itemId)?.Item2TypeId ?? 0u;
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
