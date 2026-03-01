using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Configuration;
using NLog;

namespace NexusForever.Game.Entity
{
    public class CostumeManager : ICostumeManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        public byte CostumeCap => (byte)(player.Account.RewardPropertyManager.GetRewardProperty(RewardPropertyType.CostumeSlots)?.GetValue(0) ?? 4u);

        public byte? CostumeIndex
        {
            get => costumeIndex;
            set
            {
                costumeIndex = value;
                isDirty = true;
            }
        }
        private byte? costumeIndex;

        private bool isDirty;

        // hard limit, array storing costumes at client is 12 in size
        private const byte MaxCostumes = 12;
        private const double CostumeSwapCooldown = 15d;

        private readonly IPlayer player;
        private readonly Dictionary<byte, ICostume> costumes = new();
        private double costumeSwapCooldown;

        /// <summary>
        /// Create a new <see cref="ICostumeManager"/> from existing <see cref="CharacterModel"/> database model.
        /// </summary>
        public CostumeManager(IPlayer owner, CharacterModel characterModel)
        {
            player = owner;

            costumeIndex = characterModel.ActiveCostumeIndex >= 0 ? (byte)characterModel.ActiveCostumeIndex : null;

            foreach (CharacterCostumeModel costumeModel in characterModel.Costume)
                costumes.Add(costumeModel.Index, new Costume(costumeModel));
        }

        public void Save(CharacterContext context)
        {
            foreach (ICostume costume in costumes.Values)
                costume.Save(context);

            if (isDirty)
            {
                // character is attached in Player::Save, this will only be local lookup
                CharacterModel character = context.Character.Find(player.CharacterId);
                EntityEntry<CharacterModel> entity = context.Entry(character);

                character.ActiveCostumeIndex = (sbyte)(CostumeIndex ?? -1);
                entity.Property(p => p.ActiveCostumeIndex).IsModified = true;

                isDirty = false;
            }
        }

        public void Update(double lastTick)
        {
            if (costumeSwapCooldown == 0d)
                return;

            costumeSwapCooldown -= lastTick;
            if (costumeSwapCooldown < 0d)
            {
                costumeSwapCooldown = 0d;
                log.Trace("Costume change cooldown has reset!");
            }
        }

        /// <summary>
        /// Return <see cref="ICostume"/> at supplied index.
        /// </summary>
        public ICostume GetCostume(byte index)
        {
            costumes.TryGetValue(index, out ICostume costume);
            return costume;
        }

        /// <summary>
        /// Return <see cref="IItemVisual"/> for <see cref="ICostume"/> at suppled index and <see cref="ItemSlot"/>.
        /// </summary>
        public IItemVisual GetItemVisual(byte costumeIndex, ItemSlot slot)
        {
            ICostume costume = GetCostume(costumeIndex);
            return costume?.GetItemVisual(slot);
        }

        /// <summary>
        /// Return a collection of <see cref="IItemVisual"/> for <see cref="ICostume"/> at supplied index.
        /// </summary>
        public IEnumerable<IItemVisual> GetItemVisuals(byte costumeIndex)
        {
            ICostume costume = GetCostume(costumeIndex);
            return costume != null ? costume.GetItemVisuals() : Enumerable.Empty<IItemVisual>();
        }

        /// <summary>
        /// Validate then save or update <see cref="ICostume"/> from <see cref="ClientCostumeSave"/> packet.
        /// </summary>
        public void SaveCostume(ClientCostumeSave costumeSave)
        {
            if (costumeSave.Index < 0 || costumeSave.Index >= MaxCostumes)
            {
                SendCostumeSaveResult(CostumeSaveResult.InvalidCostumeIndex);
                return;
            }

            if (costumeSave.Index >= CostumeCap)
            {
                SendCostumeSaveResult(CostumeSaveResult.CostumeIndexNotUnlocked);
                return;
            }

            for (int i = 0; i < costumeSave.Items.Count; i++)
            {
                ClientCostumeSave.CostumeItem costumeItem = costumeSave.Items[i];
                if (costumeItem.ItemId == 0)
                    continue;

                IItemInfo itemEntry = ItemManager.Instance.GetItemInfo(costumeItem.ItemId);
                if (itemEntry == null)
                {
                    SendCostumeSaveResult(CostumeSaveResult.InvalidItem);
                    return;
                }

                if (!IsValidCostumeItemForSlot(itemEntry, (CostumeItemSlot)i))
                {
                    SendCostumeSaveResult(CostumeSaveResult.UnusableItem);
                    return;
                }

                if (!player.Account.CostumeManager.HasItemUnlock(costumeItem.ItemId))
                {
                    SendCostumeSaveResult(CostumeSaveResult.ItemNotUnlocked);
                    return;
                }

                ItemDisplayEntry itemDisplayEntry = GameTableManager.Instance.ItemDisplay.GetEntry(itemEntry.GetDisplayId());
                for (int channel = 0; channel < costumeItem.Dyes.Length; channel++)
                {
                    if (costumeItem.Dyes[channel] == 0u)
                        continue;

                    if (itemDisplayEntry == null)
                    {
                        SendCostumeSaveResult(CostumeSaveResult.InvalidDye);
                        return;
                    }

                    uint dyeChannelFlag = 1u << channel;
                    if ((itemDisplayEntry.DyeChannelFlags & dyeChannelFlag) == 0)
                    {
                        SendCostumeSaveResult(CostumeSaveResult.InvalidDye);
                        return;
                    }

                    if (!player.Account.GenericUnlockManager.IsDyeUnlocked(costumeItem.Dyes[channel]))
                    {
                        SendCostumeSaveResult(CostumeSaveResult.DyeNotUnlocked);
                        return;
                    }
                }
            }

            if (costumeSave.MannequinIndex != 0)
            {
                IHousingMannequinEntity mannequin = player.ResidenceManager.GetMannequin(costumeSave.MannequinIndex);
                if (mannequin == null)
                {
                    SendCostumeSaveResult(CostumeSaveResult.InvalidMannequinIndex, costumeSave.Index, costumeSave.MannequinIndex);
                    return;
                }

                ICostume mannequinCostume = new Costume(player, costumeSave);
                mannequin.ApplyCostume(mannequinCostume.GetItemVisuals(), mannequinCostume.Mask);

                SendCostumeSaveResult(CostumeSaveResult.Saved, costumeSave.Index, costumeSave.MannequinIndex);
                return;
            }

            if (!TryChargeCostumeSave(costumeSave, out CostumeSaveResult chargeFailureResult))
            {
                SendCostumeSaveResult(chargeFailureResult, costumeSave.Index, costumeSave.MannequinIndex);
                return;
            }

            if (costumes.TryGetValue((byte)costumeSave.Index, out ICostume costume))
                costume.Update(costumeSave);
            else
            {
                costume = new Costume(player, costumeSave);
                costumes.Add(costume.Index, costume);
            }

            if (costumeSave.Index == CostumeIndex)
                foreach (IItemVisual item in player.Inventory.GetItemVisuals())
                    player.AddVisual(item);

            SendCostume(costume);
            SendCostumeSaveResult(CostumeSaveResult.Saved, costumeSave.Index, costumeSave.MannequinIndex);
        }

        private bool TryChargeCostumeSave(ClientCostumeSave costumeSave, out CostumeSaveResult failureResult)
        {
            failureResult = CostumeSaveResult.UnknownError;

            CurrencyType currencyType = CurrencyType.Credits;
            ulong cost = GetConfiguredFlatFeeCredits();

            // Prefer AccountItem metadata when available; fall back to configured flat fee.
            foreach (ClientCostumeSave.CostumeItem item in costumeSave.Items.Where(i => i.ItemId != 0u))
            {
                AccountItemEntry accountItem = GameTableManager.Instance.AccountItem.Entries
                    .FirstOrDefault(e => e != null && e.Item2Id == item.ItemId && e.AccountCurrencyAmount > 0ul);
                if (accountItem == null)
                    continue;

                if (Enum.IsDefined(typeof(CurrencyType), (int)accountItem.AccountCurrencyEnum))
                    currencyType = (CurrencyType)accountItem.AccountCurrencyEnum;

                cost = accountItem.AccountCurrencyAmount;
                break;
            }

            if (cost == 0ul)
                return true;

            if (!player.CurrencyManager.CanAfford(currencyType, cost))
            {
                failureResult = CostumeSaveResult.InsufficientFunds;
                return false;
            }

            player.CurrencyManager.CurrencySubtractAmount(currencyType, cost);
            return true;
        }

        private ulong GetConfiguredFlatFeeCredits()
        {
            RealmConfig realmConfig = SharedConfiguration.Instance.Get<RealmConfig>();
            return realmConfig?.CostumeSaveFlatFeeCredits ?? 1000ul;
        }

        private static bool IsValidCostumeItemForSlot(IItemInfo itemInfo, CostumeItemSlot costumeSlot)
        {
            if (itemInfo == null || !itemInfo.IsEquippable())
                return false;

            ItemSlot requiredSlot = costumeSlot switch
            {
                CostumeItemSlot.Chest    => ItemSlot.ArmorChest,
                CostumeItemSlot.Legs     => ItemSlot.ArmorLegs,
                CostumeItemSlot.Head     => ItemSlot.ArmorHead,
                CostumeItemSlot.Shoulder => ItemSlot.ArmorShoulder,
                CostumeItemSlot.Feet     => ItemSlot.ArmorFeet,
                CostumeItemSlot.Hands    => ItemSlot.ArmorHands,
                CostumeItemSlot.Weapon   => ItemSlot.WeaponPrimary,
                _                        => throw new ArgumentOutOfRangeException(nameof(costumeSlot))
            };

            IEnumerable<EquippedItem> equippedSlots = ItemManager.Instance.GetEquippedBagIndexes(requiredSlot);
            return equippedSlots.Any(itemInfo.IsEquippableIntoSlot);
        }

        /// <summary>
        /// Equip <see cref="ICostume"/> at supplied index.
        /// </summary>
        public void SetCostume(int index)
        {
            // Retail client applies the swap immediately and drives the cooldown timer client-side.
            // Server only needs to validate and apply visuals.
            if (index < -1 || index >= MaxCostumes)
                throw new ArgumentOutOfRangeException();

            if (index >= CostumeCap)
                throw new ArgumentOutOfRangeException();

            if (costumeSwapCooldown > 0d)
                throw new InvalidOperationException();

            CostumeIndex = index >= 0 ? (byte)index : null;

            foreach (IItemVisual item in player.Inventory.GetItemVisuals())
                player.AddVisual(item);

            // 15 second cooldown for changing costumes, hardcoded in binary
            costumeSwapCooldown = CostumeSwapCooldown;

            log.Trace($"Set costume to index {index}");
        }

        public void SendInitialPackets()
        {
            player.Session.EnqueueMessageEncrypted(new ServerCostumeList
            {
                Costumes = costumes.Values.Select(c => c.Build()).ToList()
            });
        }

        /// <summary>
        /// Send <see cref="ServerCostume"/> with supplied <see cref="ICostume"/>.
        /// </summary>
        private void SendCostume(ICostume costume)
        {
            player.Session.EnqueueMessageEncrypted(new ServerCostume
            {
                Costume = costume.Build()
            });
        }

        /// <summary>
        /// Send <see cref="ServerCostumeSave"/> with supplied <see cref="CostumeSaveResult"/> and optional index and mannequin index.
        /// </summary>
        private void SendCostumeSaveResult(CostumeSaveResult result, int index = 0, byte mannequinIndex = 0)
        {
            player.Session.EnqueueMessageEncrypted(new ServerCostumeSave
            {
                Index          = index,
                Result         = result,
                MannequinIndex = mannequinIndex
            });
        }
    }
}
