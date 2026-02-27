using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;
using NLog;

namespace NexusForever.Game.Tradeskill
{
    /// <summary>
    /// Manages tradeskill crafting operations for a player.
    /// </summary>
    public class TradeskillManager : ITradeskillManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private readonly IPlayer player;

        /// <summary>
        /// Creates a new TradeskillManager for the specified player.
        /// </summary>
        public TradeskillManager(IPlayer player)
        {
            this.player = player;
        }

        /// <summary>
        /// Craft an item using the specified schematic.
        /// </summary>
        /// <param name="schematicId">The TradeskillSchematic2Id to craft.</param>
        /// <param name="count">Number of items to craft.</param>
        /// <returns>Result of the crafting operation.</returns>
        public CraftingResult CraftItem(uint schematicId, uint count = 1)
        {
            // Validate schematic exists and player has learned it
            TradeskillSchematic2Entry schematic = GameTableManager.Instance.TradeskillSchematic2.GetEntry(schematicId);
            if (schematic == null)
                return CraftingResult.InvalidSchematic;

            if (!player.HasLearnedSchematic(schematicId))
                return CraftingResult.SchematicNotLearned;

            // Validate tradeskill is learned
            if (!player.GetLearnedTradeskills().Contains((TradeskillType)schematic.TradeSkillId))
                return CraftingResult.TradeskillNotLearned;

            // Check if player has required materials
            if (!HasMaterials(schematic, count))
                return CraftingResult.InsufficientMaterials;

            // Consume materials
            ConsumeMaterials(schematic, count);

            // Create output item
            return CreateOutput(schematic, count);
        }

        /// <summary>
        /// Check if player has required materials for the schematic.
        /// </summary>
        private bool HasMaterials(TradeskillSchematic2Entry schematic, uint count)
        {
            for (int i = 0; i < 5; i++)
            {
                ushort materialId = (ushort)GetMaterialId(schematic, i);
                uint materialCost = GetMaterialCost(schematic, i);

                if (materialId == 0 || materialCost == 0)
                    continue;

                uint totalRequired = materialCost * count;
                if (!player.SupplySatchelManager.CanAfford(materialId, totalRequired))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Consume materials from supply satchel for crafting.
        /// </summary>
        private void ConsumeMaterials(TradeskillSchematic2Entry schematic, uint count)
        {
            for (int i = 0; i < 5; i++)
            {
                ushort materialId = (ushort)GetMaterialId(schematic, i);
                uint materialCost = GetMaterialCost(schematic, i);

                if (materialId == 0 || materialCost == 0)
                    continue;

                uint totalRequired = materialCost * count;
                player.SupplySatchelManager.RemoveAmount((uint)materialId, totalRequired);
            }
        }

        /// <summary>
        /// Create the output item from crafting.
        /// </summary>
        private CraftingResult CreateOutput(TradeskillSchematic2Entry schematic, uint count)
        {
            uint outputItemId = schematic.Item2IdOutput;
            if (outputItemId == 0)
                return CraftingResult.InvalidOutput;

            uint outputCount = schematic.OutputCount * count;

            // Get item info
            var itemInfo = ItemManager.Instance.GetItemInfo(outputItemId);
            if (itemInfo == null)
                return CraftingResult.InvalidOutput;

            // Create the item
            var item = new Item(player.CharacterId, itemInfo, outputCount);
            
            // Add to inventory
            try
            {
                player.Inventory.AddItem(item, InventoryLocation.Inventory, ItemUpdateReason.Crafting);
            }
            catch (Exception)
            {
                return CraftingResult.InventoryFull;
            }

            // Award XP for crafting (if tradeskill has XP table)
            // This would need to be implemented based on game tables

            return CraftingResult.Success;
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

        /// <summary>
        /// Grant XP to a tradeskill.
        /// </summary>
        public void GrantTradeskillXp(uint tradeskillId, uint amount)
        {
            // TODO: Implement tradeskill XP tracking
            // For now, this is a placeholder that logs the XP grant
            // Full implementation would need:
            // - Tradeskill level tracking
            // - XP persistence to database
            // - Level up logic
            log.Trace($"Granting {amount} XP to tradeskill {tradeskillId} for player {player.CharacterId}");
        }

        /// <summary>
        /// Try to learn a schematic.
        /// </summary>
        public void TryLearnSchematic(uint schematicId)
        {
            // Validate schematic exists
            TradeskillSchematic2Entry schematic = GameTableManager.Instance.TradeskillSchematic2.GetEntry(schematicId);
            if (schematic == null)
            {
                log.Warn($"Player {player.CharacterId} tried to learn invalid schematic {schematicId}");
                return;
            }

            // Grant the schematic to the player using existing method
            player.TryLearnSchematic(schematicId);
            log.Trace($"Player {player.CharacterId} learned schematic {schematicId}");
        }
    }
}
