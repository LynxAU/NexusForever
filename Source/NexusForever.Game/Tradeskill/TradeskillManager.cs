using System;
using System.Collections.Generic;
using System.Linq;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NLog;

namespace NexusForever.Game.Tradeskill
{
    /// <summary>
    /// Manages tradeskill crafting operations and XP tracking for a player.
    /// </summary>
    public class TradeskillManager : ITradeskillManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private sealed class TradeskillXpState
        {
            public uint CurrentXp { get; set; }
            public uint CurrentTier { get; set; }
            public bool IsNew { get; set; }
            public bool IsDirty { get; set; }
            public bool IsDeleted { get; set; }
        }

        private readonly IPlayer player;
        private readonly Dictionary<uint, TradeskillXpState> tradeskillXp = new();

        public TradeskillManager(IPlayer player)
        {
            this.player = player;
        }

        /// <summary>
        /// Load tradeskill XP data from existing database models and populate the learned tradeskill set.
        /// </summary>
        public void Load(IEnumerable<CharacterTradeskillModel> models)
        {
            foreach (CharacterTradeskillModel model in models)
            {
                // Populate the player's learned tradeskill set; this will call InitializeTradeskill
                // which creates a fresh state — we then overwrite it with the persisted values.
                player.TryLearnTradeskill((TradeskillType)model.TradeskillId);

                // Override the state created by InitializeTradeskill with actual DB values.
                tradeskillXp[model.TradeskillId] = new TradeskillXpState
                {
                    CurrentXp   = model.CurrentXp,
                    CurrentTier = model.CurrentTier,
                    IsNew       = false,
                    IsDirty     = false
                };
            }
        }

        /// <summary>
        /// Save tradeskill XP changes to the database.
        /// </summary>
        public void Save(CharacterContext context)
        {
            var toRemove = new List<uint>();

            foreach (var (tradeskillId, state) in tradeskillXp)
            {
                if (state.IsDeleted)
                {
                    if (!state.IsNew)
                    {
                        context.CharacterTradeskill.Remove(new CharacterTradeskillModel
                        {
                            Id           = player.CharacterId,
                            TradeskillId = tradeskillId
                        });
                    }
                    toRemove.Add(tradeskillId);
                }
                else if (state.IsNew)
                {
                    context.CharacterTradeskill.Add(new CharacterTradeskillModel
                    {
                        Id           = player.CharacterId,
                        TradeskillId = tradeskillId,
                        CurrentXp    = state.CurrentXp,
                        CurrentTier  = state.CurrentTier
                    });
                    state.IsNew    = false;
                    state.IsDirty  = false;
                }
                else if (state.IsDirty)
                {
                    var model = new CharacterTradeskillModel
                    {
                        Id           = player.CharacterId,
                        TradeskillId = tradeskillId
                    };
                    var entry = context.Attach(model);
                    model.CurrentXp   = state.CurrentXp;
                    model.CurrentTier = state.CurrentTier;
                    entry.Property(p => p.CurrentXp).IsModified   = true;
                    entry.Property(p => p.CurrentTier).IsModified = true;
                    state.IsDirty = false;
                }
            }

            foreach (uint id in toRemove)
                tradeskillXp.Remove(id);
        }

        /// <summary>
        /// Create a fresh XP tracking entry for a newly learned tradeskill.
        /// Called from Player.TryLearnTradeskill.
        /// </summary>
        public void InitializeTradeskill(uint tradeskillId)
        {
            tradeskillXp[tradeskillId] = new TradeskillXpState
            {
                CurrentXp   = 0,
                CurrentTier = 0,
                IsNew       = true,
                IsDirty     = false
            };
        }

        /// <summary>
        /// Mark a tradeskill entry for deletion when the tradeskill is dropped.
        /// </summary>
        public void RemoveTradeskill(uint tradeskillId)
        {
            if (tradeskillXp.TryGetValue(tradeskillId, out TradeskillXpState state))
                state.IsDeleted = true;
        }

        /// <summary>
        /// Return the current accumulated XP for a tradeskill (0 if not tracked).
        /// </summary>
        public uint GetTradeskillXp(uint tradeskillId)
        {
            return tradeskillXp.TryGetValue(tradeskillId, out TradeskillXpState state) && !state.IsDeleted
                ? state.CurrentXp
                : 0u;
        }

        /// <summary>
        /// Grant XP to a tradeskill, advancing tiers and sending a client update.
        /// </summary>
        public void GrantTradeskillXp(uint tradeskillId, uint amount)
        {
            if (amount == 0)
                return;

            if (!tradeskillXp.TryGetValue(tradeskillId, out TradeskillXpState state) || state.IsDeleted)
            {
                log.Trace($"GrantTradeskillXp: tradeskill {tradeskillId} not found for player {player.CharacterId}");
                return;
            }

            state.CurrentXp += amount;

            // Advance tiers while XP meets the next tier's requirement.
            while (true)
            {
                TradeskillTierEntry nextTier = GameTableManager.Instance.TradeskillTier.Entries
                    .FirstOrDefault(e => e != null && e.TradeSkillId == tradeskillId && e.Tier == state.CurrentTier + 1);
                if (nextTier == null || state.CurrentXp < nextTier.RequiredXp)
                    break;
                state.CurrentTier++;
                log.Trace($"Player {player.CharacterId} advanced tradeskill {tradeskillId} to tier {state.CurrentTier}");
            }

            state.IsDirty = true;

            player.Session.EnqueueMessageEncrypted(new ServerProfessionUpdate
            {
                Tradeskill = new TradeskillInfo
                {
                    TradeskillId = (TradeskillType)tradeskillId,
                    IsActive     = 1u,
                    TradeskillXp = state.CurrentXp
                }
            });
        }

        /// <summary>
        /// Craft an item using the specified schematic.
        /// </summary>
        public CraftingResult CraftItem(uint schematicId, uint count = 1)
        {
            TradeskillSchematic2Entry schematic = GameTableManager.Instance.TradeskillSchematic2.GetEntry(schematicId);
            if (schematic == null)
                return CraftingResult.InvalidSchematic;

            if (!player.HasLearnedSchematic(schematicId))
                return CraftingResult.SchematicNotLearned;

            if (!player.GetLearnedTradeskills().Contains((TradeskillType)schematic.TradeSkillId))
                return CraftingResult.TradeskillNotLearned;

            if (!HasMaterials(schematic, count))
                return CraftingResult.InsufficientMaterials;

            ConsumeMaterials(schematic, count);

            CraftingResult result = CreateOutput(schematic, count);
            if (result == CraftingResult.Success)
                AwardCraftXp(schematic, count);

            return result;
        }

        private bool HasMaterials(TradeskillSchematic2Entry schematic, uint count)
        {
            for (int i = 0; i < 5; i++)
            {
                uint materialId   = GetMaterialId(schematic, i);
                uint materialCost = GetMaterialCost(schematic, i);
                if (materialId == 0 || materialCost == 0)
                    continue;
                if (!player.SupplySatchelManager.CanAfford((ushort)materialId, materialCost * count))
                    return false;
            }
            return true;
        }

        private void ConsumeMaterials(TradeskillSchematic2Entry schematic, uint count)
        {
            for (int i = 0; i < 5; i++)
            {
                uint materialId   = GetMaterialId(schematic, i);
                uint materialCost = GetMaterialCost(schematic, i);
                if (materialId == 0 || materialCost == 0)
                    continue;
                player.SupplySatchelManager.RemoveAmount(materialId, materialCost * count);
            }
        }

        private CraftingResult CreateOutput(TradeskillSchematic2Entry schematic, uint count)
        {
            uint outputItemId = schematic.Item2IdOutput;
            if (outputItemId == 0)
                return CraftingResult.InvalidOutput;

            uint outputCount = schematic.OutputCount * count;

            var itemInfo = ItemManager.Instance.GetItemInfo(outputItemId);
            if (itemInfo == null)
                return CraftingResult.InvalidOutput;

            var item = new NexusForever.Game.Entity.Item(player.CharacterId, itemInfo, outputCount);
            try
            {
                player.Inventory.AddItem(item, InventoryLocation.Inventory, ItemUpdateReason.Crafting);
            }
            catch (Exception)
            {
                return CraftingResult.InventoryFull;
            }

            return CraftingResult.Success;
        }

        private void AwardCraftXp(TradeskillSchematic2Entry schematic, uint count)
        {
            uint tradeskillId = schematic.TradeSkillId;
            if (!tradeskillXp.TryGetValue(tradeskillId, out TradeskillXpState state) || state.IsDeleted)
                return;

            TradeskillTierEntry tierEntry = GameTableManager.Instance.TradeskillTier.Entries
                .FirstOrDefault(e => e != null && e.TradeSkillId == tradeskillId && e.Tier == state.CurrentTier);
            uint craftXp = tierEntry?.CraftXp ?? 0;
            if (craftXp == 0)
                return;

            GrantTradeskillXp(tradeskillId, craftXp * count);
        }

        /// <summary>
        /// Try to learn a schematic.
        /// </summary>
        public void TryLearnSchematic(uint schematicId)
        {
            TradeskillSchematic2Entry schematic = GameTableManager.Instance.TradeskillSchematic2.GetEntry(schematicId);
            if (schematic == null)
            {
                log.Warn($"Player {player.CharacterId} tried to learn invalid schematic {schematicId}");
                return;
            }

            player.TryLearnSchematic(schematicId);
            log.Trace($"Player {player.CharacterId} learned schematic {schematicId}");
        }

        private static uint GetMaterialId(TradeskillSchematic2Entry schematic, int index)
        {
            return index switch
            {
                0 => schematic.Item2IdMaterial00,
                1 => schematic.Item2IdMaterial01,
                2 => schematic.Item2IdMaterial02,
                3 => schematic.Item2IdMaterial03,
                4 => schematic.Item2IdMaterial04,
                _ => 0
            };
        }

        private static uint GetMaterialCost(TradeskillSchematic2Entry schematic, int index)
        {
            return index switch
            {
                0 => schematic.MaterialCost00,
                1 => schematic.MaterialCost01,
                2 => schematic.MaterialCost02,
                3 => schematic.MaterialCost03,
                4 => schematic.MaterialCost04,
                _ => 0
            };
        }
    }
}
