using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;
using NLog;

namespace NexusForever.Game.PrimalMatrix
{
    /// <summary>
    /// Manages the player's Primal Matrix - a progression system for earning essence rewards.
    /// </summary>
    public class PrimalMatrixManager : IPrimalMatrixManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private readonly IPlayer player;
        private readonly Dictionary<uint, uint> essences = new();
        private readonly HashSet<uint> activatedNodes = new();
        private readonly HashSet<uint> modifiedEssences = new();
        private bool loaded = false;

        /// <summary>
        /// Create a new <see cref="IPrimalMatrixManager"/>.
        /// </summary>
        public PrimalMatrixManager(IPlayer player)
        {
            this.player = player;
        }

        /// <summary>
        /// Load Primal Matrix data from database model.
        /// </summary>
        public void Load(CharacterModel model)
        {
            if (loaded)
                return;

            foreach (CharacterPrimalMatrixModel primalMatrixModel in model.PrimalMatrix)
            {
                essences[primalMatrixModel.EssenceId] = primalMatrixModel.Amount;
            }

            loaded = true;
        }

        /// <summary>
        /// Add essence to player's Primal Matrix.
        /// </summary>
        public void AddEssence(uint essenceId, uint amount)
        {
            if (essenceId == 0 || amount == 0)
                return;

            // Validate the essence ID against game tables
            PrimalMatrixRewardEntry entry = GameTableManager.Instance.PrimalMatrixReward.GetEntry(essenceId);
            if (entry == null)
            {
                log.Warn($"Player {player.CharacterId} received invalid essence {essenceId}");
                return;
            }

            essences.TryGetValue(essenceId, out uint currentAmount);
            essences[essenceId] = currentAmount + amount;
            modifiedEssences.Add(essenceId);

            log.Trace($"Player {player.CharacterId} received {amount} essence {essenceId}, total now {essences[essenceId]}");

            // Check for any unlock thresholds
            CheckUnlockThresholds();
        }

        /// <summary>
        /// Get the amount of a specific essence in the matrix.
        /// </summary>
        public uint GetEssenceAmount(uint essenceId)
        {
            return essences.TryGetValue(essenceId, out uint val) ? val : 0u;
        }

        /// <summary>
        /// Activate a primal matrix node for the player.
        /// </summary>
        public bool ActivateNode(uint nodeId)
        {
            if (activatedNodes.Contains(nodeId))
                return false;

            PrimalMatrixNodeEntry nodeEntry = GameTableManager.Instance.PrimalMatrixNode.GetEntry(nodeId);
            if (nodeEntry == null)
            {
                log.Warn($"Player {player.CharacterId} attempted to activate invalid node {nodeId}");
                return false;
            }

            // Check if player has enough essence to unlock this node
            // Different classes have different essence costs
            Class playerClass = player.Class;
            uint essenceCost = GetEssenceCostForClass(nodeEntry, playerClass);

            uint totalPurpleEssence = GetEssenceAmount(0); // Purple essence is typically index 0
            if (totalPurpleEssence < essenceCost)
            {
                log.Warn($"Player {player.CharacterId} does not have enough essence to activate node {nodeId}");
                return false;
            }

            // Deduct essence cost
            essences[0] = totalPurpleEssence - essenceCost;

            // Add the node
            activatedNodes.Add(nodeId);

            log.Trace($"Player {player.CharacterId} activated node {nodeId} for class {playerClass}");

            // Grant the reward for this node
            GrantNodeReward(nodeEntry, playerClass);

            return true;
        }

        /// <summary>
        /// Get the essence cost for a specific class.
        /// </summary>
        private uint GetEssenceCostForClass(PrimalMatrixNodeEntry nodeEntry, Class playerClass)
        {
            return nodeEntry.CostPurpleEssence;
        }

        /// <summary>
        /// Grant the reward for an activated node.
        /// </summary>
        private void GrantNodeReward(PrimalMatrixNodeEntry nodeEntry, Class playerClass)
        {
            uint rewardId = playerClass switch
            {
                Class.Warrior => nodeEntry.PrimalMatrixRewardIdWarrior,
                Class.Engineer => nodeEntry.PrimalMatrixRewardIdEngineer,
                Class.Esper => nodeEntry.PrimalMatrixRewardIdEsper,
                Class.Medic => nodeEntry.PrimalMatrixRewardIdMedic,
                Class.Stalker => nodeEntry.PrimalMatrixRewardIdStalker,
                Class.Spellslinger => nodeEntry.PrimalMatrixRewardIdSpellslinger,
                _ => 0u
            };

            if (rewardId == 0)
                return;

            PrimalMatrixRewardEntry rewardEntry = GameTableManager.Instance.PrimalMatrixReward.GetEntry(rewardId);
            if (rewardEntry == null)
                return;

            // Grant item reward if present
            if (rewardEntry.ObjectId0 > 0)
            {
                player.Inventory.ItemCreate(InventoryLocation.Inventory, rewardEntry.ObjectId0, 1, ItemUpdateReason.PrimalMatrix);
            }

            // Grant spell reward if present - ObjectId1 contains spellId based on reward type
            if (rewardEntry.ObjectId1 > 0)
            {
                player.SpellManager.AddSpell(rewardEntry.ObjectId1);
            }
        }

        /// <summary>
        /// Check if any nodes can be unlocked based on current essence amounts.
        /// </summary>
        private void CheckUnlockThresholds()
        {
            // This could be expanded to notify the client about available unlocks
        }

        public void Save(CharacterContext context)
        {
            // Save only modified essence amounts
            foreach (uint essenceId in modifiedEssences)
            {
                if (!essences.TryGetValue(essenceId, out uint amount))
                    continue;

                var model = new CharacterPrimalMatrixModel
                {
                    Id = player.CharacterId,
                    EssenceId = essenceId,
                    Amount = amount
                };

                // Check if this essence already exists in the database
                bool exists = context.CharacterPrimalMatrix.Local.Any(e => e.Id == player.CharacterId && e.EssenceId == essenceId);
                if (!exists)
                {
                    // Try to find in database
                    exists = context.CharacterPrimalMatrix.Any(e => e.Id == player.CharacterId && e.EssenceId == essenceId);
                }

                if (!exists)
                {
                    // New entry - add to database
                    context.Add(model);
                }
                else
                {
                    // Existing entry - update
                    EntityEntry<CharacterPrimalMatrixModel> entity = context.Attach(model);
                    entity.Property(p => p.Amount).IsModified = true;
                }
            }

            modifiedEssences.Clear();
        }

        public void SendInitialPackets()
        {
            // TODO: Add network messages when protocol is reverse-engineered
            // For now, the primal matrix data is sent as part of the character data
            log.Trace($"Sending initial primal matrix data for player {player.CharacterId}");
        }
    }
}
