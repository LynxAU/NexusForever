using System;
using System.Linq;
using System.Threading.Tasks;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;

namespace NexusForever.Game.Entity
{
    public class HarvestUnitEntity : WorldEntity, IHarvestUnitEntity
    {
        public override EntityType Type => EntityType.HarvestUnit;

        /// <summary>
        /// Whether this harvest unit has been harvested and should be removed.
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

            // Prevent harvesting the same node multiple times
            if (IsHarvested)
                return;

            // Update GatherResource quest objectives when harvesting
            if (activator != null)
            {
                activator.QuestManager.ObjectiveUpdate(QuestObjectiveType.GatheResource, CreatureId, 1u);
                TryGiveHarvestMaterials(activator);

                // Mark as harvested so it can't be harvested again
                IsHarvested = true;

                // Schedule respawn using the creature's RescanCooldown (default 5 minutes if not set)
                ScheduleRespawn();
            }
        }

        private void ScheduleRespawn()
        {
            // Get respawn time from creature entry (in seconds)
            uint respawnTime = CreatureEntry?.RescanCooldown ?? 300u;
            
            // Schedule the respawn
            Task.Delay(TimeSpan.FromSeconds(respawnTime)).ContinueWith(_ =>
            {
                IsHarvested = false;
            });
        }

        private void TryGiveHarvestMaterials(IPlayer player)
        {
            try
            {
                // Get the harvesting info from the creature entry
                uint harvestingInfoId = CreatureEntry?.TradeskillHarvestingInfoId ?? 0;
                if (harvestingInfoId == 0)
                    return;

                TradeskillHarvestingInfoEntry harvestingInfo = GameTableManager.Instance.TradeskillHarvestingInfo.GetEntry(harvestingInfoId);
                if (harvestingInfo == null)
                    return;

                // Get the tradeskill tier info
                TradeskillTierEntry tierEntry = GameTableManager.Instance.TradeskillTier.GetEntry(harvestingInfo.TradeSkillTierId);
                if (tierEntry == null)
                    return;

                // Find materials that match this tier's tradeskill
                // The TradeskillMaterial doesn't directly link to tier, so we filter by category
                // based on what tradeskill type this is
                uint tradeskillId = tierEntry.TradeSkillId;

                // Different tradeskills have different material categories:
                // 1 = Mining, 2 = Harvesting, 3 = Survival, etc.
                // We map tradeskill to expected category(ies)
                uint[] expectedCategories = GetExpectedCategories(tradeskillId);

                var materials = GameTableManager.Instance.TradeskillMaterial.Entries
                    .Where(m => expectedCategories.Contains(m.TradeskillMaterialCategoryId))
                    .ToList();

                if (materials.Count == 0)
                    return;

                // Pick a random material from the appropriate category
                var random = new Random();
                var materialEntry = materials[random.Next(materials.Count)];

                // Add the material to player's supply satchel
                player.SupplySatchelManager.AddAmount((ushort)materialEntry.Id, 1);
            }
            catch (Exception ex)
            {
                // Log error but don't crash - harvesting is optional
                // TODO: Add proper logging
            }
        }

        private uint[] GetExpectedCategories(uint tradeskillId)
        {
            // Map tradeskill IDs to their material categories
            // This would need to be verified against actual game data
            return tradeskillId switch
            {
                1 => new[] { 1u },  // Mining -> Ore
                2 => new[] { 2u },  // Harvesting -> Plants
                3 => new[] { 3u },  // Survival -> Food
                4 => new[] { 4u },  // Archaeology -> Relics
                _ => new[] { 1u, 2u, 3u, 4u } // Default: any material
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
