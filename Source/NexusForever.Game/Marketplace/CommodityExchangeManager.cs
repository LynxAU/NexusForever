using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Item;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Static.Marketplace;
using NexusForever.GameTable;
using NexusForever.GameTable.Static;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Static;
using NLog;

namespace NexusForever.Game.Marketplace
{
    /// <summary>
    /// Manages the Commodity Exchange - a market for trading stackable items (crafting materials, runes, etc.)
    /// </summary>
    public class CommodityExchangeManager
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Default order duration in hours (48 hours typical)
        /// </summary>
        private const int OrderDurationHours = 48;

        /// <summary>
        /// Maximum number of commodity orders a player can have active
        /// </summary>
        private const int MaxOrdersPerPlayer = 50;

        /// <summary>
        /// Transaction fee percentage (5%)
        /// </summary>
        private const double TransactionFee = 0.05;

        private readonly IDbContextFactory<CharacterContext> dbContextFactory;

        public CommodityExchangeManager(IDbContextFactory<CharacterContext> dbContextFactory)
        {
            this.dbContextFactory = dbContextFactory;
        }

        /// <summary>
        /// Create a new commodity order (buy or sell) for a stackable item.
        /// </summary>
        public async Task<(GenericError Result, CommodityOrder Order)> CreateOrderAsync(
            IPlayer player,
            uint itemId,
            uint quantity,
            ulong unitPrice,
            bool isBuyOrder,
            bool forceImmediate)
        {
            // Validate item exists
            var itemEntry = GameTableManager.Instance.Item.GetEntry(itemId);
            if (itemEntry == null)
                return (GenericError.ItemBadId, null);

            // Check if item is stackable (required for commodity exchange)
            if (itemEntry.MaxStackCount <= 1)
                return (GenericError.AuctionItemAuctionDisabled, null);

            // Validate quantity
            if (quantity == 0 || quantity > itemEntry.MaxStackCount)
                return (GenericError.AuctionOrderTooBig, null);

            // Validate price
            if (unitPrice == 0)
                return (GenericError.AuctionBidTooLow, null);

            // Check player order limits
            var existingOrders = await GetOrdersByCharacterAsync(player.CharacterId);
            if (existingOrders.Count >= MaxOrdersPerPlayer)
                return (GenericError.AuctionTooManyOrders, null);

            ulong totalPrice = unitPrice * quantity;

            if (isBuyOrder)
            {
                // For buy orders, player needs to have the money upfront
                if (!player.CurrencyManager.CanAfford(CurrencyType.Credits, totalPrice))
                    return (GenericError.VendorNotEnoughCash, null);

                // Deduct the credits
                player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, totalPrice);
            }
            else
            {
                // For sell orders, check if player has the items
                if (!player.Inventory.HasItemCount(itemId, quantity))
                    return (GenericError.ItemNoItems, null);

                // Remove items from inventory
                player.Inventory.ItemDelete(itemId, quantity, ItemUpdateReason.Auction);
            }

            // Create the order
            var order = new CharacterCommodityOrderModel
            {
                OrderId = GenerateOrderId(),
                Id = player.CharacterId,
                CharacterId = player.CharacterId,
                ItemId = itemId,
                Quantity = quantity,
                FilledQuantity = 0,
                UnitPrice = unitPrice,
                IsBuyOrder = isBuyOrder,
                ExpirationTime = DateTime.UtcNow.AddHours(OrderDurationHours),
                CreateTime = DateTime.UtcNow
            };

            await using var db = await dbContextFactory.CreateDbContextAsync();
            db.CharacterCommodityOrder.Add(order);
            await db.SaveChangesAsync();

            log.Trace($"Created {(isBuyOrder ? "buy" : "sell")} order {order.OrderId} for {quantity}x item {itemId} at {unitPrice} per unit by character {player.CharacterId}");

            // If forceImmediate, try to match with existing orders
            if (forceImmediate)
            {
                await TryMatchOrderAsync(db, order, player);
            }

            return (GenericError.Ok, ConvertToCommodityOrder(order));
        }

        /// <summary>
        /// Cancel a commodity order and return items/credits to the owner.
        /// </summary>
        public async Task<(GenericError Result, CommodityOrder Order)> CancelOrderAsync(
            IPlayer player,
            ulong orderId,
            uint itemId)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();

            var order = await db.CharacterCommodityOrder.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
                return (GenericError.AuctionNotFound, null);

            // Verify ownership
            if (order.CharacterId != player.CharacterId)
                return (GenericError.AuctionOwnItem, null);

            // Can't cancel if order is already filled
            if (order.FilledQuantity >= order.Quantity)
                return (GenericError.AuctionAlreadyHasBid, null);

            uint remainingQuantity = order.Quantity - order.FilledQuantity;

            // Return remaining credits to buyer (for buy orders)
            if (order.IsBuyOrder && remainingQuantity > 0)
            {
                ulong refundAmount = order.UnitPrice * remainingQuantity;
                player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, refundAmount);
            }

            // Return remaining items to seller (for sell orders)
            if (!order.IsBuyOrder && remainingQuantity > 0)
            {
                player.Inventory.ItemCreate(InventoryLocation.Inventory, order.ItemId, remainingQuantity, ItemUpdateReason.Auction);
            }

            // Delete order
            db.CharacterCommodityOrder.Remove(order);
            await db.SaveChangesAsync();

            log.Trace($"Cancelled order {orderId} by character {player.CharacterId}");

            return (GenericError.Ok, ConvertToCommodityOrder(order));
        }

        /// <summary>
        /// Get commodity market info for an item (buy/sell order summaries).
        /// </summary>
        public async Task<ServerCommodityInfoResults> GetCommodityInfoAsync(uint itemId)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();

            var now = DateTime.UtcNow;
            var orders = await db.CharacterCommodityOrder
                .Where(o => o.ItemId == itemId && o.ExpirationTime > now)
                .ToListAsync();

            var buyOrders = orders.Where(o => o.IsBuyOrder && o.FilledQuantity < o.Quantity).ToList();
            var sellOrders = orders.Where(o => !o.IsBuyOrder && o.FilledQuantity < o.Quantity).ToList();

            // Calculate price buckets (1, 10, 50 units)
            var buyOrderPrices = CalculatePriceBuckets(buyOrders);
            var sellOrderPrices = CalculatePriceBuckets(sellOrders);

            // Get top 20 orders for detailed view
            var topOrders = orders
                .Where(o => o.FilledQuantity < o.Quantity)
                .OrderBy(o => o.IsBuyOrder ? 0 : 1) // Buy orders first
                .ThenBy(o => o.UnitPrice)
                .Take(20)
                .Select(ConvertToCommodityOrder)
                .ToList();

            return new ServerCommodityInfoResults
            {
                Item2Id = itemId,
                BuyOrderCount = (uint)buyOrders.Count,
                BuyOrderPrices = buyOrderPrices,
                SellOrderCount = (uint)sellOrders.Count,
                SellOrderPrices = sellOrderPrices,
                Orders = topOrders
            };
        }

        /// <summary>
        /// Get all commodity orders owned by a character.
        /// </summary>
        public async Task<List<CommodityOrder>> GetOwnedOrdersAsync(ulong characterId)
        {
            var orders = await GetOrdersByCharacterAsync(characterId);
            return orders.Select(ConvertToCommodityOrder).ToList();
        }

        /// <summary>
        /// Process expired commodity orders - return items/credits to owners.
        /// </summary>
        public async Task ProcessExpiredOrdersAsync()
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();

            var expiredOrders = await db.CharacterCommodityOrder
                .Where(o => o.ExpirationTime <= DateTime.UtcNow && o.FilledQuantity < o.Quantity)
                .ToListAsync();

            foreach (var order in expiredOrders)
            {
                await ReturnExpiredOrderAsync(db, order);
            }

            if (expiredOrders.Count > 0)
            {
                log.Trace($"Processed {expiredOrders.Count} expired commodity orders");
            }
        }

        /// <summary>
        /// Try to match a new order with existing opposite orders.
        /// </summary>
        private async Task TryMatchOrderAsync(CharacterContext db, CharacterCommodityOrderModel newOrder, IPlayer player)
        {
            // Buy orders match with sell orders and vice versa
            bool lookingForBuyOrders = !newOrder.IsBuyOrder;

            var now = DateTime.UtcNow;
            var matchingOrdersQuery = db.CharacterCommodityOrder
                .Where(o => o.ItemId == newOrder.ItemId 
                    && o.IsBuyOrder == lookingForBuyOrders 
                    && o.ExpirationTime > now 
                    && o.FilledQuantity < o.Quantity
                    && o.CharacterId != newOrder.CharacterId); // Don't match with self

            // Best-price-first ordering: when selling, match highest buy; when buying, match lowest sell.
            var matchingOrders = await (lookingForBuyOrders
                ? matchingOrdersQuery.OrderByDescending(o => o.UnitPrice)
                : matchingOrdersQuery.OrderBy(o => o.UnitPrice))
                .ToListAsync();

            uint remainingToFill = newOrder.Quantity - newOrder.FilledQuantity;

            foreach (var matchOrder in matchingOrders)
            {
                if (remainingToFill == 0)
                    break;

                // Check price compatibility
                if (newOrder.IsBuyOrder)
                {
                    // Buy order can match if seller's price <= buyer's price
                    if (matchOrder.UnitPrice > newOrder.UnitPrice)
                        continue;
                }
                else
                {
                    // Sell order can match if buyer's price >= seller's price
                    if (matchOrder.UnitPrice < newOrder.UnitPrice)
                        continue;
                }

                uint matchOrderRemaining = matchOrder.Quantity - matchOrder.FilledQuantity;
                uint fillAmount = Math.Min(remainingToFill, matchOrderRemaining);

                // Determine the transaction price (the existing order's price)
                ulong transactionPrice = matchOrder.UnitPrice * fillAmount;

                // Execute the trade
                await ExecuteTradeAsync(db, newOrder, matchOrder, fillAmount, transactionPrice);

                remainingToFill -= fillAmount;
            }

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Execute a trade between two orders.
        /// </summary>
        private async Task ExecuteTradeAsync(
            CharacterContext db,
            CharacterCommodityOrderModel newOrder,
            CharacterCommodityOrderModel existingOrder,
            uint quantity,
            ulong totalPrice)
        {
            // Update filled quantities
            newOrder.FilledQuantity += quantity;
            existingOrder.FilledQuantity += quantity;

            // Determine buyer and seller
            var buyerOrder = newOrder.IsBuyOrder ? newOrder : existingOrder;
            var sellerOrder = newOrder.IsBuyOrder ? existingOrder : newOrder;

            // Process the trade
            await ProcessTradeAsync(db, buyerOrder, sellerOrder, quantity, totalPrice);

            log.Trace($"Matched trade: {quantity}x item {newOrder.ItemId} at {totalPrice / quantity} per unit between orders {newOrder.OrderId} and {existingOrder.OrderId}");
        }

        /// <summary>
        /// Process a completed trade between buyer and seller.
        /// </summary>
        private async Task ProcessTradeAsync(
            CharacterContext db,
            CharacterCommodityOrderModel buyerOrder,
            CharacterCommodityOrderModel sellerOrder,
            uint quantity,
            ulong totalPrice)
        {
            ulong sellerAmount = (ulong)(totalPrice * (1 - TransactionFee));

            // Give items to buyer
            IPlayer buyer = GetOnlinePlayer(buyerOrder.CharacterId);
            if (buyer != null)
            {
                buyer.Inventory.ItemCreate(InventoryLocation.Inventory, buyerOrder.ItemId, quantity, ItemUpdateReason.Auction);
            }
            else
            {
                await MailNewItemToCharacterAsync(db, buyerOrder.CharacterId, buyerOrder.ItemId, quantity,
                    "Commodity Purchase", "Your commodity buy order was filled. The item is attached.");
            }

            // Give credits to seller (minus fee)
            IPlayer seller = GetOnlinePlayer(sellerOrder.CharacterId);
            if (seller != null)
            {
                seller.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, sellerAmount);
            }
            else
            {
                // Mail credits to offline seller
                await MailCreditsToCharacterAsync(db, sellerOrder.CharacterId, sellerAmount,
                    "Commodity Exchange Sale", $"Your commodity order for {quantity} items sold. Credits enclosed (minus {(int)(TransactionFee * 100)}% fee).");
            }

            // Handle buyer overpayment refund (if buyer paid more than seller asked)
            if (buyerOrder.UnitPrice > sellerOrder.UnitPrice)
            {
                ulong overpayment = (buyerOrder.UnitPrice - sellerOrder.UnitPrice) * quantity;
                if (buyer != null)
                {
                    buyer.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, overpayment);
                }
            }
        }

        /// <summary>
        /// Return an expired order's items/credits to the owner.
        /// </summary>
        private async Task ReturnExpiredOrderAsync(CharacterContext db, CharacterCommodityOrderModel order)
        {
            uint remainingQuantity = order.Quantity - order.FilledQuantity;

            if (remainingQuantity == 0)
            {
                // Order is fully filled, just remove it
                db.CharacterCommodityOrder.Remove(order);
                await db.SaveChangesAsync();
                return;
            }

            IPlayer owner = GetOnlinePlayer(order.CharacterId);

            if (order.IsBuyOrder)
            {
                // Return credits to buyer
                ulong refundAmount = order.UnitPrice * remainingQuantity;
                if (owner != null)
                {
                    owner.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, refundAmount);
                }
                else
                {
                    await MailCreditsToCharacterAsync(db, order.CharacterId, refundAmount,
                        "Commodity Order Expired", "Your commodity buy order expired. Credits refunded.");
                }
            }
            else
            {
                // Return items to seller
                if (owner != null)
                {
                    owner.Inventory.ItemCreate(InventoryLocation.Inventory, order.ItemId, remainingQuantity, ItemUpdateReason.Auction);
                }
                else
                {
                    await MailNewItemToCharacterAsync(db, order.CharacterId, order.ItemId, remainingQuantity,
                        "Commodity Order Expired", "Your commodity sell order expired. The item is returned.");
                }
            }

            db.CharacterCommodityOrder.Remove(order);
            await db.SaveChangesAsync();

            log.Trace($"Returned expired commodity order {order.OrderId} to character {order.CharacterId}");
        }

        /// <summary>
        /// Get all active orders for a character.
        /// </summary>
        private async Task<List<CharacterCommodityOrderModel>> GetOrdersByCharacterAsync(ulong characterId)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();
            return await db.CharacterCommodityOrder
                .Where(o => o.CharacterId == characterId && o.ExpirationTime > DateTime.UtcNow && o.FilledQuantity < o.Quantity)
                .ToListAsync();
        }

        /// <summary>
        /// Calculate price buckets for 1, 10, and 50 unit quantities.
        /// </summary>
        private ulong[] CalculatePriceBuckets(List<CharacterCommodityOrderModel> orders)
        {
            var prices = new ulong[3];
            uint[] bucketSizes = { 1, 10, 50 };

            for (int i = 0; i < 3; i++)
            {
                uint needed = bucketSizes[i];
                ulong totalCost = 0;
                uint found = 0;

                foreach (var order in orders.OrderBy(o => o.UnitPrice))
                {
                    uint available = order.Quantity - order.FilledQuantity;
                    uint take = Math.Min(needed - found, available);
                    totalCost += order.UnitPrice * take;
                    found += take;

                    if (found >= needed)
                        break;
                }

                prices[i] = found >= needed ? totalCost : 0;
            }

            return prices;
        }

        /// <summary>
        /// Mail credits to an offline character.
        /// </summary>
        private async Task MailCreditsToCharacterAsync(CharacterContext db, ulong recipientId, ulong amount, string subject, string body)
        {
            db.CharacterMail.Add(new CharacterMailModel
            {
                Id             = AssetManager.Instance.NextMailId,
                RecipientId    = recipientId,
                SenderType     = (byte)SenderType.ItemAuction,
                Subject        = subject,
                Message        = body,
                CurrencyAmount = amount,
                DeliveryTime   = (byte)DeliverySpeed.Instant,
                CreateTime     = DateTime.UtcNow
            });

            await db.SaveChangesAsync();

            log.Trace($"Mailed {amount} credits to character {recipientId}");
        }

        /// <summary>
        /// Create a new item and mail it to an offline character.
        /// </summary>
        private async Task MailNewItemToCharacterAsync(CharacterContext db, ulong recipientId, uint itemId, uint quantity, string subject, string body)
        {
            ulong itemGuid = ItemManager.Instance.NextItemId;
            ulong mailId   = AssetManager.Instance.NextMailId;

            db.Item.Add(new ItemModel
            {
                Id         = itemGuid,
                OwnerId    = null,
                ItemId     = itemId,
                Location   = 0,
                BagIndex   = 0,
                StackCount = quantity,
                Charges    = 0,
                Durability = 1.0f
            });

            var mail = new CharacterMailModel
            {
                Id           = mailId,
                RecipientId  = recipientId,
                SenderType   = (byte)SenderType.ItemAuction,
                Subject      = subject,
                Message      = body,
                DeliveryTime = (byte)DeliverySpeed.Instant,
                CreateTime   = DateTime.UtcNow
            };

            mail.Attachment.Add(new CharacterMailAttachmentModel
            {
                Id       = mailId,
                Index    = 0,
                ItemGuid = itemGuid
            });

            db.CharacterMail.Add(mail);
            await db.SaveChangesAsync();

            log.Trace($"Mailed item {itemId}x{quantity} to character {recipientId}");
        }

        /// <summary>
        /// Get an online player by character ID.
        /// </summary>
        private IPlayer GetOnlinePlayer(ulong characterId)
        {
            return PlayerManager.Instance.GetPlayer(characterId);
        }

        /// <summary>
        /// Generate a unique order ID.
        /// </summary>
        private ulong GenerateOrderId()
        {
            return ((ulong)DateTime.UtcNow.Subtract(new DateTime(2020, 1, 1)).TotalSeconds << 32) | (ulong)Random.Shared.Next(int.MaxValue);
        }

        /// <summary>
        /// Convert a database model to a network message model.
        /// </summary>
        private CommodityOrder ConvertToCommodityOrder(CharacterCommodityOrderModel model)
        {
            uint remaining = model.Quantity - model.FilledQuantity;
            ulong totalPrice = model.UnitPrice * remaining;

            return new CommodityOrder
            {
                CommodityOrderId = model.OrderId,
                Item2Id = model.ItemId,
                Quantity = remaining,
                PricePerUnit = model.UnitPrice,
                Price = totalPrice,
                IsBuyOrder = model.IsBuyOrder,
                ForceImmediate = false,
                ListTime = (ulong)(model.CreateTime - DateTime.MinValue).TotalSeconds,
                ExpirationTime = (ulong)(model.ExpirationTime - DateTime.UtcNow).TotalSeconds
            };
        }
    }
}
