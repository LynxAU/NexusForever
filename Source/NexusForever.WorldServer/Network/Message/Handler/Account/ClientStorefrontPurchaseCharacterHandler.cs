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
    public class ClientStorefrontPurchaseCharacterHandler : IMessageHandler<IWorldSession, ClientStorefrontPurchaseCharacter>
    {
        #region Dependency Injection

        private readonly ILogger<ClientStorefrontPurchaseCharacterHandler> log;
        private readonly IGlobalStorefrontManager globalStorefrontManager;

        public ClientStorefrontPurchaseCharacterHandler(
            ILogger<ClientStorefrontPurchaseCharacterHandler> log,
            IGlobalStorefrontManager globalStorefrontManager)
        {
            this.log                    = log;
            this.globalStorefrontManager = globalStorefrontManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientStorefrontPurchaseCharacter purchase)
        {
            IAccount account = session.Account;

            // Resolve the offer item from the global catalogue.
            IOfferItem offerItem = globalStorefrontManager.GetStoreOfferItem(purchase.OfferId);
            if (offerItem == null)
            {
                log.LogWarning("Player {CharacterId} attempted to purchase unknown offer id {OfferId}.",
                    session.Player?.CharacterId, purchase.OfferId);
                return;
            }

            // Resolve price for the requested currency.
            var currencyType = (AccountCurrencyType)purchase.CurrencyId;
            IOfferItemPrice price = offerItem.GetPriceDataForCurrency(currencyType);
            if (price == null)
            {
                log.LogWarning("Offer {OfferId} has no price entry for currency {CurrencyId}.",
                    purchase.OfferId, purchase.CurrencyId);
                return;
            }

            ulong cost = (ulong)price.GetCurrencyValue();
            if (!account.CurrencyManager.CanAfford(currencyType, cost))
            {
                log.LogWarning("Player {CharacterId} cannot afford offer {OfferId} (cost {Cost} {Currency}).",
                    session.Player?.CharacterId, purchase.OfferId, cost, currencyType);
                return;
            }

            // Deliver all items in the offer.
            IPlayer player = session.Player;
            foreach (IOfferItemData itemData in offerItem.GetItemData())
            {
                AccountItemEntry entry = itemData.Entry;
                if (entry == null)
                    continue;

                DeliverAccountItem(account, player, entry, itemData.Amount);
            }

            // Deduct currency — this also sends ServerAccountCurrencyGrant to the client.
            account.CurrencyManager.CurrencySubtractAmount(currencyType, cost);

            // Send the finalise packet so the client knows the transaction is complete.
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

            // Grant account currency (e.g. bonus Omnibits).
            if (entry.AccountCurrencyEnum != 0 && entry.AccountCurrencyAmount != 0)
            {
                account.CurrencyManager.CurrencyAddAmount(
                    (AccountCurrencyType)entry.AccountCurrencyEnum,
                    entry.AccountCurrencyAmount);
            }

            // GenericUnlockSetId delivery (cosmetic unlocks) — requires resolving the set table,
            // which is not yet exposed via IGlobalStorefrontManager. Logged for future implementation.
            if (entry.GenericUnlockSetId != 0)
            {
                log.LogWarning("Store item has GenericUnlockSetId {SetId} — delivery not yet implemented.",
                    entry.GenericUnlockSetId);
            }
        }
    }
}
