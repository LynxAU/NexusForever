using NexusForever.Game.Static.Crafting;

namespace NexusForever.Game.Abstract.Entity
{
    public interface ITradeskillManager
    {
        /// <summary>
        /// Craft an item using the specified schematic.
        /// </summary>
        CraftingResult CraftItem(uint schematicId, uint count = 1);

        /// <summary>
        /// Grant XP to a tradeskill.
        /// </summary>
        void GrantTradeskillXp(uint tradeskillId, uint amount);

        /// <summary>
        /// Try to learn a schematic.
        /// </summary>
        void TryLearnSchematic(uint schematicId);
    }
}
