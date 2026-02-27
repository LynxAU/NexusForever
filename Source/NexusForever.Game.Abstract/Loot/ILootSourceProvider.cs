using NexusForever.Game.Static.Loot;

namespace NexusForever.Game.Abstract.Loot
{
    public interface ILootSourceProvider
    {
        void Initialise();

        /// <summary>
        /// Roll loot for supplied creature id and context.
        /// </summary>
        IEnumerable<LootDrop> RollCreatureLoot(uint creatureId, LootContext context = LootContext.Any);
    }
}
