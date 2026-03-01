using System.Collections.Generic;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Static.Crafting;

namespace NexusForever.Game.Abstract.Entity
{
    public interface ITradeskillManager
    {
        /// <summary>
        /// Load tradeskill XP data from existing database models.
        /// </summary>
        void Load(IEnumerable<CharacterTradeskillModel> models);

        /// <summary>
        /// Save tradeskill XP changes to the database.
        /// </summary>
        void Save(CharacterContext context);

        /// <summary>
        /// Craft an item using the specified schematic.
        /// </summary>
        CraftingResult CraftItem(uint schematicId, uint count = 1);

        /// <summary>
        /// Grant XP to a tradeskill, advancing tiers as needed.
        /// </summary>
        void GrantTradeskillXp(uint tradeskillId, uint amount);

        /// <summary>
        /// Try to learn a schematic.
        /// </summary>
        void TryLearnSchematic(uint schematicId);

        /// <summary>
        /// Create a fresh XP tracking entry for a newly learned tradeskill.
        /// </summary>
        void InitializeTradeskill(uint tradeskillId);

        /// <summary>
        /// Mark a tradeskill entry for deletion when the tradeskill is dropped.
        /// </summary>
        void RemoveTradeskill(uint tradeskillId);

        /// <summary>
        /// Return the current accumulated XP for a tradeskill (0 if not tracked).
        /// </summary>
        uint GetTradeskillXp(uint tradeskillId);

        /// <summary>
        /// Return the XP that would be awarded for crafting the specified schematic at the player's current tier.
        /// </summary>
        uint GetCraftRewardXp(uint schematicId);

        /// <summary>
        /// Craft an item with optional crit, returning the crafted item id and XP earned.
        /// Used by the complex craft handler to select between normal and crit output.
        /// </summary>
        CraftingResult CraftItemWithResult(uint schematicId, bool isCrit, out uint craftedItemId, out uint earnedXp);
    }
}
