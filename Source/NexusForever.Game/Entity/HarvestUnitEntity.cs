using System.Linq;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;
using NLog;

namespace NexusForever.Game.Entity
{
    public class HarvestUnitEntity : WorldEntity, IHarvestUnitEntity
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public override EntityType Type => EntityType.HarvestUnit;

        /// <summary>
        /// Whether this harvest unit has been harvested and is awaiting removal.
        /// </summary>
        public bool IsHarvested { get; private set; }

        #region Dependency Injection

        public HarvestUnitEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        #endregion

        public override void OnActivate(IPlayer activator)
        {
            base.OnActivate(activator);

            // Prevent harvesting the same node multiple times.
            if (IsHarvested)
                return;

            if (activator == null)
                return;

            activator.QuestManager.ObjectiveUpdate(QuestObjectiveType.GatheResource, CreatureId, 1u);
            TryGiveHarvestMaterials(activator);

            IsHarvested = true;

            // If this entity was spawned from a DB record, remove it from the map and
            // schedule a fresh respawn using the creature's RescanCooldown (default 5 min).
            if (SpawnModel != null)
            {
                uint respawnTime = CreatureEntry?.RescanCooldown ?? 300u;
                Map?.ScheduleRespawn(SpawnModel, respawnTime);
                Map?.EnqueueRemove(this);
            }
        }

        private void TryGiveHarvestMaterials(IPlayer player)
        {
            try
            {
                uint harvestingInfoId = CreatureEntry?.TradeskillHarvestingInfoId ?? 0;
                if (harvestingInfoId == 0)
                    return;

                TradeskillHarvestingInfoEntry harvestingInfo = GameTableManager.Instance.TradeskillHarvestingInfo.GetEntry(harvestingInfoId);
                if (harvestingInfo == null)
                    return;

                TradeskillTierEntry tierEntry = GameTableManager.Instance.TradeskillTier.GetEntry(harvestingInfo.TradeSkillTierId);
                if (tierEntry == null)
                    return;

                uint tradeskillId = tierEntry.TradeSkillId;
                uint[] expectedCategories = GetExpectedCategories(tradeskillId);

                var materials = GameTableManager.Instance.TradeskillMaterial.Entries
                    .Where(m => expectedCategories.Contains(m.TradeskillMaterialCategoryId))
                    .ToList();

                if (materials.Count == 0)
                    return;

                var random = new Random();
                var materialEntry = materials[random.Next(materials.Count)];

                player.SupplySatchelManager.AddAmount((ushort)materialEntry.Id, 1);
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Failed to give harvest materials to player from entity {CreatureId}.");
            }
        }

        private static uint[] GetExpectedCategories(uint tradeskillId)
        {
            return tradeskillId switch
            {
                1 => new[] { 1u },  // Mining -> Ore
                2 => new[] { 2u },  // Harvesting -> Plants
                3 => new[] { 3u },  // Survival -> Food
                4 => new[] { 4u },  // Archaeology -> Relics
                _ => new[] { 1u, 2u, 3u, 4u }
            };
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new HarvestUnitEntityModel
            {
                CreatureId = CreatureId
            };
        }
    }
}
