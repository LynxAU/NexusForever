using System.Collections.Generic;

namespace NexusForever.Game.Abstract.Entity
{
    public interface IHousingMannequinEntity : IWorldEntity
    {
        /// <summary>
        /// Apply costume visuals to this mannequin and emit a visual update.
        /// </summary>
        void ApplyCostume(IEnumerable<IItemVisual> visuals, uint mask);
    }
}
