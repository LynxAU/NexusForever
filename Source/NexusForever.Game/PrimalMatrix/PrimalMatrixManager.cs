using System.Collections.Generic;
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
    /// Manages the player's Primal Matrix — a hex-grid progression system for spending
    /// coloured essence crystals to unlock stat/spell rewards.
    /// </summary>
    public class PrimalMatrixManager : IPrimalMatrixManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        // Essence type IDs as used in SpellEffect.DataBits00 / QuestReward.ObjectId.
        // TODO: verify these against retail spell data once a packet dump is available.
        private const uint EssenceIdRed    = 1u;
        private const uint EssenceIdBlue   = 2u;
        private const uint EssenceIdGreen  = 3u;
        private const uint EssenceIdPurple = 4u;

        private readonly IPlayer player;

        // essence accumulation: essenceId → amount
        private readonly Dictionary<uint, uint> essences    = new();
        private readonly HashSet<uint> loadedEssenceIds     = new(); // know which rows exist in DB
        private readonly HashSet<uint> modifiedEssences     = new(); // dirty flag for save

        // node allocations: nodeId → allocation count (capped at PrimalMatrixNodeEntry.MaxAllocations)
        private readonly Dictionary<uint, uint> nodeAllocations = new();
        private readonly HashSet<uint> loadedNodeIds        = new(); // know which rows exist in DB
        private readonly HashSet<uint> modifiedNodes        = new(); // dirty flag for save

        private bool loaded = false;

        /// <summary>
        /// Create a new <see cref="IPrimalMatrixManager"/>.
        /// </summary>
        public PrimalMatrixManager(IPlayer player)
        {
            this.player = player;
        }

        /// <summary>
        /// Load Primal Matrix data from the character database model.
        /// </summary>
        public void Load(CharacterModel model)
        {
            if (loaded)
                return;

            foreach (CharacterPrimalMatrixModel row in model.PrimalMatrix)
            {
                essences[row.EssenceId] = row.Amount;
                loadedEssenceIds.Add(row.EssenceId);
            }

            foreach (CharacterPrimalMatrixNodeModel row in model.PrimalMatrixNodes)
            {
                nodeAllocations[row.NodeId] = row.Allocations;
                loadedNodeIds.Add(row.NodeId);
            }

            loaded = true;
        }

        /// <summary>
        /// Add essence of the specified type to the player's Primal Matrix pool.
        /// </summary>
        public void AddEssence(uint essenceId, uint amount)
        {
            if (essenceId == 0 || amount == 0)
                return;

            essences.TryGetValue(essenceId, out uint current);
            essences[essenceId] = current + amount;
            modifiedEssences.Add(essenceId);

            log.Trace($"Player {player.CharacterId} earned {amount}x essence[{essenceId}], total={essences[essenceId]}");

            CheckUnlockThresholds();
        }

        /// <summary>
        /// Get the amount of a specific essence type held by this player.
        /// </summary>
        public uint GetEssenceAmount(uint essenceId)
        {
            return essences.TryGetValue(essenceId, out uint val) ? val : 0u;
        }

        /// <summary>
        /// Attempt to activate (or add an allocation to) a Primal Matrix node.
        /// Returns <c>false</c> if the node is fully allocated, does not exist,
        /// or if the player lacks sufficient essence.
        /// </summary>
        public bool ActivateNode(uint nodeId)
        {
            PrimalMatrixNodeEntry nodeEntry = GameTableManager.Instance.PrimalMatrixNode.GetEntry(nodeId);
            if (nodeEntry == null)
            {
                log.Warn($"Player {player.CharacterId} tried to activate unknown node {nodeId}");
                return false;
            }

            // MaxAllocations == 0 means the field is unset in game data; treat as 1.
            uint maxAlloc = nodeEntry.MaxAllocations > 0 ? nodeEntry.MaxAllocations : 1u;

            nodeAllocations.TryGetValue(nodeId, out uint currentAlloc);
            if (currentAlloc >= maxAlloc)
            {
                log.Warn($"Player {player.CharacterId} tried to over-allocate node {nodeId} ({currentAlloc}/{maxAlloc})");
                return false;
            }

            // Verify player holds enough of each essence colour.
            if (!HasEnoughEssence(nodeEntry))
            {
                log.Warn($"Player {player.CharacterId} lacks essence to activate node {nodeId}");
                return false;
            }

            // Deduct all four essence colours.
            DeductEssence(EssenceIdRed,    nodeEntry.CostRedEssence);
            DeductEssence(EssenceIdBlue,   nodeEntry.CostBlueEssence);
            DeductEssence(EssenceIdGreen,  nodeEntry.CostGreenEssence);
            DeductEssence(EssenceIdPurple, nodeEntry.CostPurpleEssence);

            nodeAllocations[nodeId] = currentAlloc + 1;
            modifiedNodes.Add(nodeId);

            log.Trace($"Player {player.CharacterId} activated node {nodeId} ({currentAlloc + 1}/{maxAlloc})");

            GrantNodeReward(nodeEntry, player.Class);
            return true;
        }

        // ── Private helpers ────────────────────────────────────────────────────────

        private bool HasEnoughEssence(PrimalMatrixNodeEntry entry)
        {
            return GetEssenceAmount(EssenceIdRed)    >= entry.CostRedEssence
                && GetEssenceAmount(EssenceIdBlue)   >= entry.CostBlueEssence
                && GetEssenceAmount(EssenceIdGreen)  >= entry.CostGreenEssence
                && GetEssenceAmount(EssenceIdPurple) >= entry.CostPurpleEssence;
        }

        private void DeductEssence(uint essenceId, uint cost)
        {
            if (cost == 0)
                return;

            uint current = GetEssenceAmount(essenceId);
            essences[essenceId] = current > cost ? current - cost : 0u;
            modifiedEssences.Add(essenceId);
        }

        private void GrantNodeReward(PrimalMatrixNodeEntry nodeEntry, Class playerClass)
        {
            uint rewardId = playerClass switch
            {
                Class.Warrior      => nodeEntry.PrimalMatrixRewardIdWarrior,
                Class.Engineer     => nodeEntry.PrimalMatrixRewardIdEngineer,
                Class.Esper        => nodeEntry.PrimalMatrixRewardIdEsper,
                Class.Medic        => nodeEntry.PrimalMatrixRewardIdMedic,
                Class.Stalker      => nodeEntry.PrimalMatrixRewardIdStalker,
                Class.Spellslinger => nodeEntry.PrimalMatrixRewardIdSpellslinger,
                _                  => 0u
            };

            if (rewardId == 0)
                return;

            PrimalMatrixRewardEntry reward = GameTableManager.Instance.PrimalMatrixReward.GetEntry(rewardId);
            if (reward == null)
            {
                log.Warn($"PrimalMatrixReward {rewardId} not found (node {nodeEntry.Id}, class {playerClass})");
                return;
            }

            // Dispatch reward slots.
            // TODO: implement full per-slot type dispatch once PrimalMatrixRewardTypeEnum values are known.
            // Current best-guess: slot 0 = item, slot 1 = spell.
            if (reward.ObjectId0 > 0)
                player.Inventory.ItemCreate(InventoryLocation.Inventory, reward.ObjectId0, 1, ItemUpdateReason.PrimalMatrix);

            if (reward.ObjectId1 > 0)
                player.SpellManager.AddSpell(reward.ObjectId1);
        }

        private void CheckUnlockThresholds()
        {
            // Future: notify client UI about newly affordable nodes.
        }

        // ── Save ──────────────────────────────────────────────────────────────────

        public void Save(CharacterContext context)
        {
            SaveEssences(context);
            SaveNodes(context);
        }

        private void SaveEssences(CharacterContext context)
        {
            foreach (uint essenceId in modifiedEssences)
            {
                if (!essences.TryGetValue(essenceId, out uint amount))
                    continue;

                if (loadedEssenceIds.Contains(essenceId))
                {
                    // Row existed at load time — update via attach.
                    var model = new CharacterPrimalMatrixModel
                    {
                        Id        = player.CharacterId,
                        EssenceId = essenceId,
                        Amount    = amount
                    };
                    EntityEntry<CharacterPrimalMatrixModel> entry = context.Attach(model);
                    entry.Property(p => p.Amount).IsModified = true;
                }
                else
                {
                    // New essence type for this player — insert.
                    context.Add(new CharacterPrimalMatrixModel
                    {
                        Id        = player.CharacterId,
                        EssenceId = essenceId,
                        Amount    = amount
                    });
                    loadedEssenceIds.Add(essenceId);
                }
            }

            modifiedEssences.Clear();
        }

        private void SaveNodes(CharacterContext context)
        {
            foreach (uint nodeId in modifiedNodes)
            {
                if (!nodeAllocations.TryGetValue(nodeId, out uint alloc))
                    continue;

                if (loadedNodeIds.Contains(nodeId))
                {
                    // Row existed at load time — update via attach.
                    var model = new CharacterPrimalMatrixNodeModel
                    {
                        Id          = player.CharacterId,
                        NodeId      = nodeId,
                        Allocations = alloc
                    };
                    EntityEntry<CharacterPrimalMatrixNodeModel> entry = context.Attach(model);
                    entry.Property(p => p.Allocations).IsModified = true;
                }
                else
                {
                    // First activation of this node — insert.
                    context.Add(new CharacterPrimalMatrixNodeModel
                    {
                        Id          = player.CharacterId,
                        NodeId      = nodeId,
                        Allocations = alloc
                    });
                    loadedNodeIds.Add(nodeId);
                }
            }

            modifiedNodes.Clear();
        }

        public void SendInitialPackets()
        {
            // TODO: send ServerPrimalMatrixEssence / ServerPrimalMatrixNodeUnlock packets
            // once the network protocol for this system is reverse-engineered.
            log.Trace($"Sending initial primal matrix data for player {player.CharacterId}");
        }
    }
}
