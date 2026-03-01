using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Entity;
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

            SendEssenceUpdate(essenceId, essences[essenceId]);
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

            // Notify client of updated essence amounts for each colour that had a cost.
            if (nodeEntry.CostRedEssence    > 0) SendEssenceUpdate(EssenceIdRed,    GetEssenceAmount(EssenceIdRed));
            if (nodeEntry.CostBlueEssence   > 0) SendEssenceUpdate(EssenceIdBlue,   GetEssenceAmount(EssenceIdBlue));
            if (nodeEntry.CostGreenEssence  > 0) SendEssenceUpdate(EssenceIdGreen,  GetEssenceAmount(EssenceIdGreen));
            if (nodeEntry.CostPurpleEssence > 0) SendEssenceUpdate(EssenceIdPurple, GetEssenceAmount(EssenceIdPurple));

            // Notify client of the node allocation.
            SendNodeUpdate(nodeId, nodeAllocations[nodeId]);

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

            DispatchRewardSlot(reward.PrimalMatrixRewardTypeEnum0, reward.ObjectId0, reward.SubObjectId0, reward.Value0, nodeEntry.Id, 0);
            DispatchRewardSlot(reward.PrimalMatrixRewardTypeEnum1, reward.ObjectId1, reward.SubObjectId1, reward.Value1, nodeEntry.Id, 1);
            DispatchRewardSlot(reward.PrimalMatrixRewardTypeEnum2, reward.ObjectId2, reward.SubObjectId2, reward.Value2, nodeEntry.Id, 2);
            DispatchRewardSlot(reward.PrimalMatrixRewardTypeEnum3, reward.ObjectId3, reward.SubObjectId3, reward.Value3, nodeEntry.Id, 3);
        }

        private void DispatchRewardSlot(uint rewardType, uint objectId, uint subObjectId, float value, uint nodeId, int slotIndex)
        {
            if (objectId == 0u)
                return;

            uint amount = ResolveRewardAmount(subObjectId, value);

            bool handled = rewardType switch
            {
                0u => TryDispatchByHeuristic(objectId, amount),
                1u => DispatchItem(objectId, amount),
                2u => DispatchSpell(objectId),
                3u => DispatchCurrency(objectId, amount),
                4u => DispatchTitle(objectId),
                _  => false
            };

            if (handled)
                return;

            if (TryDispatchByHeuristic(objectId, amount))
                return;

            log.Warn($"Unhandled primal matrix reward slot: node={nodeId}, slot={slotIndex}, type={rewardType}, objectId={objectId}, subObjectId={subObjectId}, value={value}.");
        }

        private static uint ResolveRewardAmount(uint subObjectId, float value)
        {
            if (subObjectId > 0u)
                return subObjectId;

            if (value > 0f)
                return Math.Max(1u, (uint)MathF.Round(value));

            return 1u;
        }

        private bool TryDispatchByHeuristic(uint objectId, uint amount)
        {
            if (DispatchSpell(objectId))
                return true;

            if (DispatchItem(objectId, amount))
                return true;

            if (DispatchCurrency(objectId, amount))
                return true;

            if (DispatchTitle(objectId))
                return true;

            return false;
        }

        private bool DispatchSpell(uint objectId)
        {
            Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(objectId);
            if (spell4Entry != null)
            {
                player.SpellManager.AddSpell(spell4Entry.Spell4BaseIdBaseSpell);
                return true;
            }

            Spell4BaseEntry baseEntry = GameTableManager.Instance.Spell4Base.GetEntry(objectId);
            if (baseEntry != null)
            {
                player.SpellManager.AddSpell(baseEntry.Id);
                return true;
            }

            return false;
        }

        private bool DispatchItem(uint objectId, uint amount)
        {
            if (GameTableManager.Instance.Item.GetEntry(objectId) == null)
                return false;

            player.Inventory.ItemCreate(InventoryLocation.Inventory, objectId, amount, ItemUpdateReason.PrimalMatrix);
            return true;
        }

        private bool DispatchCurrency(uint objectId, uint amount)
        {
            if (GameTableManager.Instance.CurrencyType.GetEntry(objectId) == null)
                return false;

            player.CurrencyManager.CurrencyAddAmount((CurrencyType)objectId, amount);
            return true;
        }

        private bool DispatchTitle(uint objectId)
        {
            if (GameTableManager.Instance.CharacterTitle.GetEntry(objectId) == null)
                return false;

            player.TitleManager.AddTitle((ushort)objectId);
            return true;
        }

        private void CheckUnlockThresholds()
        {
            // Future: notify client UI about newly affordable nodes.
        }

        private void SendEssenceUpdate(uint essenceId, uint amount)
        {
            player.Session.EnqueueMessageEncrypted(new ServerPrimalMatrixEssence
            {
                EssenceId = essenceId,
                Amount    = amount
            });
        }

        private void SendNodeUpdate(uint nodeId, uint allocations)
        {
            player.Session.EnqueueMessageEncrypted(new ServerPrimalMatrixNode
            {
                EntityId  = player.Guid,
                NodeId    = nodeId,
                EssenceId = 0u,
                Amount    = allocations
            });
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
            // Send current essence amounts for all four colours (even if zero).
            SendEssenceUpdate(EssenceIdRed,    GetEssenceAmount(EssenceIdRed));
            SendEssenceUpdate(EssenceIdBlue,   GetEssenceAmount(EssenceIdBlue));
            SendEssenceUpdate(EssenceIdGreen,  GetEssenceAmount(EssenceIdGreen));
            SendEssenceUpdate(EssenceIdPurple, GetEssenceAmount(EssenceIdPurple));

            // Send all activated node allocations.
            foreach ((uint nodeId, uint alloc) in nodeAllocations)
                SendNodeUpdate(nodeId, alloc);
        }
    }
}
