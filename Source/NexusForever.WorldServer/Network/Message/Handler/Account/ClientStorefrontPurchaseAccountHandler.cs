using System;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Storefront;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;

namespace NexusForever.WorldServer.Network.Message.Handler.Account
{
    /// <summary>
    /// Handles account-level store purchases (e.g. CREDD packs).
    /// On a private server all account items are delivered for free — no currency
    /// is deducted regardless of what the client sends in CurrencyId.
    /// </summary>
    public class ClientStorefrontPurchaseAccountHandler : IMessageHandler<IWorldSession, ClientStorefrontPurchaseAccount>
    {
        #region Dependency Injection

        private readonly ILogger<ClientStorefrontPurchaseAccountHandler> log;
        private readonly IGlobalStorefrontManager globalStorefrontManager;

        public ClientStorefrontPurchaseAccountHandler(
            ILogger<ClientStorefrontPurchaseAccountHandler> log,
            IGlobalStorefrontManager globalStorefrontManager)
        {
            this.log                     = log;
            this.globalStorefrontManager = globalStorefrontManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientStorefrontPurchaseAccount purchase)
        {
            IAccount account = session.Account;

            // Resolve the offer item from the global catalogue.
            IOfferItem offerItem = globalStorefrontManager.GetStoreOfferItem(purchase.OfferId);
            if (offerItem == null)
            {
                log.LogWarning("Account {AccountId} attempted to purchase unknown offer id {OfferId}.",
                    account.Id, purchase.OfferId);
                return;
            }

            // Deliver all items in the offer at no cost (private server free-grant model).
            IPlayer player = session.Player;
            foreach (IOfferItemData itemData in offerItem.GetItemData())
            {
                AccountItemEntry entry = itemData.Entry;
                if (entry == null)
                    continue;

                DeliverAccountItem(account, player, entry, itemData.Amount);
            }

            // Notify the client that the transaction is complete.
            session.EnqueueMessageEncrypted(new ServerStoreFinalise());
        }

        private void DeliverAccountItem(IAccount account, IPlayer player, AccountItemEntry entry, uint amount)
        {
            // Grant in-world item to the player's inventory if they are online.
            if (entry.Item2Id != 0)
            {
                if (player != null)
                    player.Inventory.ItemCreate(InventoryLocation.Inventory, entry.Item2Id, Math.Max(amount, 1u));
                else
                    log.LogWarning("Store item delivery skipped for Item2Id {Item2Id}: player not in world.", entry.Item2Id);
            }

            // Grant account entitlement (e.g. extra character slots, signature status).
            if (entry.EntitlementId != 0)
            {
                account.EntitlementManager.UpdateEntitlement(
                    (EntitlementType)entry.EntitlementId,
                    (int)Math.Max(entry.EntitlementCount, 1u));
            }

            // Grant account currency (e.g. CREDD, Omnibits). Multiply by amount so
            // multi-pack offers deliver the correct quantity.
            if (entry.AccountCurrencyEnum != 0 && entry.AccountCurrencyAmount != 0)
            {
                account.CurrencyManager.CurrencyAddAmount(
                    (AccountCurrencyType)entry.AccountCurrencyEnum,
                    entry.AccountCurrencyAmount * Math.Max(amount, 1u));
            }

            // Deliver cosmetic unlocks (dyes, etc.) contained in the set.
            if (entry.GenericUnlockSetId != 0)
                account.GenericUnlockManager.UnlockSet(entry.GenericUnlockSetId);
        }
    }
}
