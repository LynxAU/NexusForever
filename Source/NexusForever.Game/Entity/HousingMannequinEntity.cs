using System;
using System.Collections.Generic;
using System.Linq;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;

namespace NexusForever.Game.Entity
{
    internal class HousingMannequinEntity : WorldEntity, IHousingMannequinEntity
    {
        public override EntityType Type => EntityType.HousingMannequin;

        #region Dependency Injection

        public HousingMannequinEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        #endregion

        public void ApplyCostume(IEnumerable<IItemVisual> visuals, uint mask)
        {
            foreach (ItemSlot slot in Enum.GetValues<ItemSlot>())
                RemoveVisual(slot);

            foreach (IItemVisual visual in visuals.Where(v => v != null))
            {
                int costumeSlotIndex = visual.Slot switch
                {
                    ItemSlot.ArmorChest    => 0,
                    ItemSlot.ArmorLegs     => 1,
                    ItemSlot.ArmorHead     => 2,
                    ItemSlot.ArmorShoulder => 3,
                    ItemSlot.ArmorFeet     => 4,
                    ItemSlot.ArmorHands    => 5,
                    ItemSlot.WeaponPrimary => 6,
                    _                      => -1
                };

                if (costumeSlotIndex >= 0 && (mask & (1u << costumeSlotIndex)) == 0)
                    continue;

                AddVisual(visual);
            }
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new HousingMannequinEntityModel
            {
                CreatureId = CreatureId
            };
        }
    }
}
