using System.Collections.Concurrent;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Trade;
using NexusForever.Game.Entity;
using NexusForever.Game.Static;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using CurrencyType = NexusForever.Game.Static.Entity.CurrencyType;
using TradeResult = NexusForever.Network.World.Message.Model.ServerP2PTradeResult.P2PTradeResult;
using InventoryLocation = NexusForever.Game.Static.Entity.InventoryLocation;

namespace NexusForever.Game.Trade
{
    public class TradeManager : ITradeManager
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Active trade sessions, keyed by player character ID.
        /// </summary>
        private readonly ConcurrentDictionary<ulong, TradeSession> activeTrades = new();

        public void InitiateTrade(IPlayer player, IPlayer target)
        {
            // Check if either player is already in a trade
            if (activeTrades.ContainsKey(player.CharacterId) || activeTrades.ContainsKey(target.CharacterId))
            {
                player.Session.EnqueueMessageEncrypted(new ServerP2PTradeResult
                {
                    Result = TradeResult.InviteFailedNoTrading
                });
                return;
            }

            // Check if players are valid for trading
            if (!CanTrade(player))
            {
                player.Session.EnqueueMessageEncrypted(new ServerP2PTradeResult
                {
                    Result = TradeResult.TargetBusy
                });
                return;
            }

            if (!CanTrade(target))
            {
                player.Session.EnqueueMessageEncrypted(new ServerP2PTradeResult
                {
                    Result = TradeResult.TargetBusy
                });
                return;
            }

            // Create trade session
            var session = new TradeSession(player, target);
            activeTrades[player.CharacterId] = session;
            activeTrades[target.CharacterId] = session;

            // Send invite to target
            target.Session.EnqueueMessageEncrypted(new ServerP2PTradeInvite
            {
                TradeInviterUnitId = player.Guid
            });

            log.Trace($"Trade invite sent from {player.Name} to {target.Name}");
        }

        public void AcceptTrade(IPlayer player)
        {
            if (!activeTrades.TryGetValue(player.CharacterId, out var session))
                return;

            if (session.State != TradeState.Pending)
                return;

            // Both players have accepted - start the trade
            session.StartTrade();

            // Notify both players
            player.Session.EnqueueMessageEncrypted(new ServerP2PTradeResult
            {
                Result = TradeResult.PlayerAcceptedInvite
            });
            session.Target.Session.EnqueueMessageEncrypted(new ServerP2PTradeResult
            {
                Result = TradeResult.PlayerAcceptedInvite
            });

            log.Trace($"Trade started between {session.Player.Name} and {session.Target.Name}");
        }

        public void DeclineTrade(IPlayer player)
        {
            if (!activeTrades.TryGetValue(player.CharacterId, out var session))
                return;

            if (session.State != TradeState.Pending)
                return;

            // Notify the other player
            var otherPlayer = session.GetOtherPlayer(player);
            otherPlayer?.Session.EnqueueMessageEncrypted(new ServerP2PTradeResult
            {
                Result = TradeResult.PlayerDeclinedInvite
            });

            // Cancel the trade
            CancelTradeInternal(session, player);
        }

        public void CancelTrade(IPlayer player)
        {
            if (!activeTrades.TryGetValue(player.CharacterId, out var session))
                return;

            CancelTradeInternal(session, player);
        }

        private void CancelTradeInternal(TradeSession session, IPlayer cancelledBy)
        {
            // Return items and money to the player who offered them
            session.ReturnItemsAndMoney();

            // Notify both players
            session.Player.Session.EnqueueMessageEncrypted(new ServerP2PTradeResult
            {
                Result = TradeResult.PlayerCanceled,
                Cancelled = true
            });
            session.Target.Session.EnqueueMessageEncrypted(new ServerP2PTradeResult
            {
                Result = TradeResult.PlayerCanceled,
                Cancelled = true
            });

            // Remove from active trades
            activeTrades.TryRemove(session.Player.CharacterId, out _);
            activeTrades.TryRemove(session.Target.CharacterId, out _);

            log.Trace($"Trade cancelled between {session.Player.Name} and {session.Target.Name} by {cancelledBy.Name}");
        }

        public void AddItem(IPlayer player, uint bagIndex, uint quantity)
        {
            if (!activeTrades.TryGetValue(player.CharacterId, out var session))
                return;

            if (session.State != TradeState.Active)
                return;

            var location = new ItemLocation { Location = InventoryLocation.Inventory, BagIndex = bagIndex };
            var item = player.Inventory.GetItem(location);
            if (item == null)
                return;

            var tradeItem = new TradeItem
            {
                ItemId   = item.Info.Entry.Id,
                Quantity = Math.Min(quantity, item.StackCount),
                BagIndex = bagIndex,
                ItemGuid = item.Guid
            };

            session.AddItem(player, tradeItem);
            NotifyItemUpdate(session, player, tradeItem);
        }

        public void RemoveItem(IPlayer player, ulong itemGuid)
        {
            if (!activeTrades.TryGetValue(player.CharacterId, out var session))
                return;

            if (session.State != TradeState.Active)
                return;

            session.RemoveItem(player, itemGuid);
            NotifyItemRemoved(session, player, itemGuid);
        }

        public void SetMoney(IPlayer player, ulong amount)
        {
            if (!activeTrades.TryGetValue(player.CharacterId, out var session))
                return;

            if (session.State != TradeState.Active)
                return;

            // Check player has enough money
            if (!player.CurrencyManager.CanAfford(CurrencyType.Credits, amount))
            {
                player.Session.EnqueueMessageEncrypted(new ServerP2PTradeResult
                {
                    Result = TradeResult.ErrorAddingItem
                });
                return;
            }

            session.SetMoney(player, amount);

            // Notify both players
            NotifyMoneyUpdate(session, player, amount);
        }

        public void CommitTrade(IPlayer player)
        {
            if (!activeTrades.TryGetValue(player.CharacterId, out var session))
                return;

            if (session.State != TradeState.Active)
                return;

            session.SetReady(player);

            // Check if both players are ready
            if (session.IsPlayerReady && session.IsTargetReady)
            {
                // Both committed - finalize the trade
                if (FinalizeTrade(session))
                {
                    session.Player.Session.EnqueueMessageEncrypted(new ServerP2PTradeResult
                    {
                        Result = TradeResult.FinishedSuccess
                    });
                    session.Target.Session.EnqueueMessageEncrypted(new ServerP2PTradeResult
                    {
                        Result = TradeResult.FinishedSuccess
                    });
                }
                else
                {
                    // Trade failed - return items
                    session.ReturnItemsAndMoney();
                    session.Player.Session.EnqueueMessageEncrypted(new ServerP2PTradeResult
                    {
                        Result = TradeResult.DbFailed
                    });
                    session.Target.Session.EnqueueMessageEncrypted(new ServerP2PTradeResult
                    {
                        Result = TradeResult.DbFailed
                    });
                }

                // Clean up
                activeTrades.TryRemove(session.Player.CharacterId, out _);
                activeTrades.TryRemove(session.Target.CharacterId, out _);
            }
            else
            {
                // Notify other player that their partner has committed
                var otherPlayer = session.GetOtherPlayer(player);
                otherPlayer?.Session.EnqueueMessageEncrypted(new ServerP2PTradeResult
                {
                    Result = TradeResult.InitiatorCommitted
                });
            }
        }

        private bool FinalizeTrade(TradeSession session)
        {
            try
            {
                // Transfer items from player to target
                foreach (var item in session.PlayerItems)
                {
                    var location = new ItemLocation 
                    { 
                        Location = InventoryLocation.Inventory, 
                        BagIndex = item.BagIndex 
                    };
                    var inventoryItem = session.Player.Inventory.GetItem(location);
                    if (inventoryItem != null)
                    {
                        // Remove from player
                        session.Player.Inventory.ItemDelete(location, ItemUpdateReason.Trade);

                        // Add to target
                        session.Target.Inventory.ItemCreate(InventoryLocation.Inventory, item.ItemId, item.Quantity, ItemUpdateReason.Trade);
                    }
                }

                // Transfer items from target to player
                foreach (var item in session.TargetItems)
                {
                    var location = new ItemLocation 
                    { 
                        Location = InventoryLocation.Inventory, 
                        BagIndex = item.BagIndex 
                    };
                    var inventoryItem = session.Target.Inventory.GetItem(location);
                    if (inventoryItem != null)
                    {
                        // Remove from target
                        session.Target.Inventory.ItemDelete(location, ItemUpdateReason.Trade);

                        // Add to player
                        session.Player.Inventory.ItemCreate(InventoryLocation.Inventory, item.ItemId, item.Quantity, ItemUpdateReason.Trade);
                    }
                }

                // Transfer money
                if (session.PlayerMoney > 0)
                {
                    session.Player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, session.PlayerMoney);
                    session.Target.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, session.PlayerMoney);
                }

                if (session.TargetMoney > 0)
                {
                    session.Target.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, session.TargetMoney);
                    session.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Credits, session.TargetMoney);
                }

                log.Trace($"Trade finalized between {session.Player.Name} and {session.Target.Name}");
                return true;
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Error finalizing trade between {session.Player.Name} and {session.Target.Name}");
                return false;
            }
        }

        public ITradeSession GetTradeSession(IPlayer player)
        {
            activeTrades.TryGetValue(player.CharacterId, out var session);
            return session;
        }

        private bool CanTrade(IPlayer player)
        {
            // Cast to UnitEntity to access InCombat property
            if (player is UnitEntity unitEntity)
            {
                // Check if player is in combat
                if (unitEntity.InCombat)
                    return false;
            }

            return true;
        }

        private void NotifyItemUpdate(TradeSession session, IPlayer player, TradeItem item)
        {
            uint tradeIndex = player.CharacterId == session.Player.CharacterId
                ? (uint)session.PlayerItems.Count - 1
                : (uint)session.TargetItems.Count - 1;

            var msg = new ServerP2PTradeUpdateItem
            {
                TradeIndex  = tradeIndex,
                OwnerUnitId = player.Guid,
                Item2Id     = item.ItemId,
                ItemGuid    = item.ItemGuid,
                Quantity    = item.Quantity
            };

            player.Session.EnqueueMessageEncrypted(msg);
            session.GetOtherPlayer(player)?.Session.EnqueueMessageEncrypted(msg);
        }

        private void NotifyItemRemoved(TradeSession session, IPlayer player, ulong itemGuid)
        {
            var msg = new ServerPTPTradeItemRemoved { ItemGuid = itemGuid };
            player.Session.EnqueueMessageEncrypted(msg);
            session.GetOtherPlayer(player)?.Session.EnqueueMessageEncrypted(msg);
        }

        private void NotifyMoneyUpdate(TradeSession session, IPlayer player, ulong amount)
        {
            var msg = new ServerP2PTradeUpdateMoney
            {
                Credits = amount,
                UnitId  = player.Guid
            };

            player.Session.EnqueueMessageEncrypted(msg);
            session.GetOtherPlayer(player)?.Session.EnqueueMessageEncrypted(msg);
        }
    }
}
