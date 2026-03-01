using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Entity;
using NexusForever.Network.World.Message.Static;
using NLog;

namespace NexusForever.Game.PrimalMatrix
{
    /// <summary>
    /// Manages the player's Primal Matrix progression and reward dispatch.
    /// </summary>
    public class PrimalMatrixManager : IPrimalMatrixManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private const uint EssenceIdRed = 1u;
        private const uint EssenceIdBlue = 2u;
        private const uint EssenceIdGreen = 3u;
        private const uint EssenceIdPurple = 4u;

        private const uint RewardTypeNone = 0u;
        private const uint RewardTypeSpecialA = 2u;
        private const uint RewardTypeSpecialB = 3u;
        private const uint RewardTypeSpellUnlock = 4u;
        private const uint RewardTypeProperty = 5u;

        // Data-backed root/start nodes have this bit set in PrimalMatrixNode.Flags.
        private const uint NodeFlagStarter = 0x1u;

        private static readonly (int X, int Y)[] HexNeighbors =
        {
            (0, -1),
            (1, -1),
            (1, 0),
            (0, 1),
            (-1, 1),
            (-1, 0)
        };

        private readonly IPlayer player;

        // essence accumulation: essenceId -> amount
        private readonly Dictionary<uint, uint> essences = new();
        private readonly HashSet<uint> loadedEssenceIds = new();
        private readonly HashSet<uint> modifiedEssences = new();

        // node allocations: nodeId -> allocation count
        private readonly Dictionary<uint, uint> nodeAllocations = new();
        private readonly HashSet<uint> loadedNodeIds = new();
        private readonly HashSet<uint> modifiedNodes = new();

        // absolute primal-matrix property contribution currently applied to player
        private readonly Dictionary<Property, float> appliedPropertyBonuses = new();

        // coordinate lookup for adjacency checks
        private readonly Dictionary<(int X, int Y), uint> nodeByPosition = new();
        private bool nodeByPositionBuilt;

        private readonly HashSet<uint> loggedUnsupportedRewardTypes = new();

        private bool loaded;

        public PrimalMatrixManager(IPlayer player)
        {
            this.player = player;
        }

        /// <summary>
        /// Load Primal Matrix data from character model and rebuild persistent property rewards.
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

            // Property rewards are derived from node allocations, so rebuild them on login.
            RebuildPropertyBonusesFromAllocations();
        }

        /// <summary>
        /// Add essence of the specified type to the player's Primal Matrix pool.
        /// </summary>
        public void AddEssence(uint essenceId, uint amount)
        {
            if (essenceId == 0u || amount == 0u)
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
        /// Attempt to activate (or allocate) a Primal Matrix node.
        /// </summary>
        public bool ActivateNode(uint nodeId)
        {
            PrimalMatrixNodeEntry nodeEntry = GameTableManager.Instance.PrimalMatrixNode.GetEntry(nodeId);
            if (nodeEntry == null)
            {
                log.Warn($"Player {player.CharacterId} tried to activate unknown node {nodeId}");
                return false;
            }

            uint maxAlloc = nodeEntry.MaxAllocations > 0u ? nodeEntry.MaxAllocations : 1u;

            nodeAllocations.TryGetValue(nodeId, out uint currentAlloc);
            if (currentAlloc >= maxAlloc)
            {
                log.Warn($"Player {player.CharacterId} tried to over-allocate node {nodeId} ({currentAlloc}/{maxAlloc})");
                return false;
            }

            if (!CanActivateNode(nodeEntry, currentAlloc))
            {
                log.Warn($"Player {player.CharacterId} cannot activate non-adjacent node {nodeId}");
                return false;
            }

            if (!HasEnoughEssence(nodeEntry))
            {
                log.Warn($"Player {player.CharacterId} lacks essence to activate node {nodeId}");
                return false;
            }

            DeductEssence(EssenceIdRed, nodeEntry.CostRedEssence);
            DeductEssence(EssenceIdBlue, nodeEntry.CostBlueEssence);
            DeductEssence(EssenceIdGreen, nodeEntry.CostGreenEssence);
            DeductEssence(EssenceIdPurple, nodeEntry.CostPurpleEssence);

            nodeAllocations[nodeId] = currentAlloc + 1u;
            modifiedNodes.Add(nodeId);

            log.Trace($"Player {player.CharacterId} activated node {nodeId} ({currentAlloc + 1u}/{maxAlloc})");

            if (nodeEntry.CostRedEssence > 0u)
                SendEssenceUpdate(EssenceIdRed, GetEssenceAmount(EssenceIdRed));
            if (nodeEntry.CostBlueEssence > 0u)
                SendEssenceUpdate(EssenceIdBlue, GetEssenceAmount(EssenceIdBlue));
            if (nodeEntry.CostGreenEssence > 0u)
                SendEssenceUpdate(EssenceIdGreen, GetEssenceAmount(EssenceIdGreen));
            if (nodeEntry.CostPurpleEssence > 0u)
                SendEssenceUpdate(EssenceIdPurple, GetEssenceAmount(EssenceIdPurple));

            SendNodeUpdate(nodeId, nodeAllocations[nodeId]);

            GrantNodeNonPersistentRewards(nodeEntry, player.Class);
            RebuildPropertyBonusesFromAllocations();

            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.BeginMatrix, 0u, 1u);
            return true;
        }

        private bool HasEnoughEssence(PrimalMatrixNodeEntry entry)
        {
            return GetEssenceAmount(EssenceIdRed) >= entry.CostRedEssence
                && GetEssenceAmount(EssenceIdBlue) >= entry.CostBlueEssence
                && GetEssenceAmount(EssenceIdGreen) >= entry.CostGreenEssence
                && GetEssenceAmount(EssenceIdPurple) >= entry.CostPurpleEssence;
        }

        private void DeductEssence(uint essenceId, uint cost)
        {
            if (cost == 0u)
                return;

            uint current = GetEssenceAmount(essenceId);
            essences[essenceId] = current > cost ? current - cost : 0u;
            modifiedEssences.Add(essenceId);
        }

        private bool CanActivateNode(PrimalMatrixNodeEntry nodeEntry, uint currentAlloc)
        {
            // Additional allocations on an already-unlocked node do not require adjacency checks.
            if (currentAlloc > 0u)
                return true;

            if ((nodeEntry.Flags & NodeFlagStarter) != 0u)
                return true;

            return HasAllocatedAdjacentNode(nodeEntry);
        }

        private bool HasAllocatedAdjacentNode(PrimalMatrixNodeEntry nodeEntry)
        {
            EnsureNodeLookup();

            int x = unchecked((int)nodeEntry.PositionX);
            int y = unchecked((int)nodeEntry.PositionY);

            foreach ((int offsetX, int offsetY) in HexNeighbors)
            {
                (int X, int Y) neighborPos = (x + offsetX, y + offsetY);
                if (!nodeByPosition.TryGetValue(neighborPos, out uint neighborNodeId))
                    continue;

                if (nodeAllocations.TryGetValue(neighborNodeId, out uint alloc) && alloc > 0u)
                    return true;
            }

            return false;
        }

        private void EnsureNodeLookup()
        {
            if (nodeByPositionBuilt)
                return;

            foreach (PrimalMatrixNodeEntry entry in GameTableManager.Instance.PrimalMatrixNode.Entries)
            {
                (int X, int Y) pos = (unchecked((int)entry.PositionX), unchecked((int)entry.PositionY));
                if (!nodeByPosition.ContainsKey(pos))
                    nodeByPosition[pos] = entry.Id;
            }

            nodeByPositionBuilt = true;
        }

        private void GrantNodeNonPersistentRewards(PrimalMatrixNodeEntry nodeEntry, Class playerClass)
        {
            uint rewardId = GetRewardIdForClass(nodeEntry, playerClass);
            if (rewardId == 0u)
                return;

            PrimalMatrixRewardEntry reward = GameTableManager.Instance.PrimalMatrixReward.GetEntry(rewardId);
            if (reward == null)
            {
                log.Warn($"PrimalMatrixReward {rewardId} not found (node {nodeEntry.Id}, class {playerClass})");
                return;
            }

            DispatchNonPersistentRewardSlot(reward.PrimalMatrixRewardTypeEnum0, reward.ObjectId0, reward.SubObjectId0, reward.Value0, nodeEntry.Id, 0);
            DispatchNonPersistentRewardSlot(reward.PrimalMatrixRewardTypeEnum1, reward.ObjectId1, reward.SubObjectId1, reward.Value1, nodeEntry.Id, 1);
            DispatchNonPersistentRewardSlot(reward.PrimalMatrixRewardTypeEnum2, reward.ObjectId2, reward.SubObjectId2, reward.Value2, nodeEntry.Id, 2);
            DispatchNonPersistentRewardSlot(reward.PrimalMatrixRewardTypeEnum3, reward.ObjectId3, reward.SubObjectId3, reward.Value3, nodeEntry.Id, 3);
        }

        private uint GetRewardIdForClass(PrimalMatrixNodeEntry nodeEntry, Class playerClass)
        {
            return playerClass switch
            {
                Class.Warrior => nodeEntry.PrimalMatrixRewardIdWarrior,
                Class.Engineer => nodeEntry.PrimalMatrixRewardIdEngineer,
                Class.Esper => nodeEntry.PrimalMatrixRewardIdEsper,
                Class.Medic => nodeEntry.PrimalMatrixRewardIdMedic,
                Class.Stalker => nodeEntry.PrimalMatrixRewardIdStalker,
                Class.Spellslinger => nodeEntry.PrimalMatrixRewardIdSpellslinger,
                _ => 0u
            };
        }

        private void DispatchNonPersistentRewardSlot(uint rewardType, uint objectId, uint subObjectId, float value, uint nodeId, int slotIndex)
        {
            switch (rewardType)
            {
                case RewardTypeNone:
                    return;
                case RewardTypeSpellUnlock:
                    DispatchSpellUnlock(objectId, nodeId, slotIndex);
                    return;
                case RewardTypeProperty:
                    // Property rewards are rebuilt from allocations as persistent state.
                    return;
                case RewardTypeSpecialA:
                case RewardTypeSpecialB:
                    // TODO: implement special matrix reward payloads once enum semantics are confirmed.
                    LogUnsupportedSpecialReward(rewardType, objectId, subObjectId, value, nodeId, slotIndex);
                    return;
                default:
                    LogUnsupportedSpecialReward(rewardType, objectId, subObjectId, value, nodeId, slotIndex);
                    return;
            }
        }

        private void DispatchSpellUnlock(uint objectId, uint nodeId, int slotIndex)
        {
            if (objectId == 0u)
                return;

            Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(objectId);
            if (spell4Entry != null)
            {
                AddSpellIfMissing(spell4Entry.Spell4BaseIdBaseSpell);
                return;
            }

            Spell4BaseEntry baseEntry = GameTableManager.Instance.Spell4Base.GetEntry(objectId);
            if (baseEntry != null)
            {
                AddSpellIfMissing(baseEntry.Id);
                return;
            }

            log.Warn($"Primal Matrix spell reward could not resolve spell objectId={objectId} (node={nodeId}, slot={slotIndex}).");
        }

        private void AddSpellIfMissing(uint spell4BaseId)
        {
            if (spell4BaseId == 0u)
                return;

            if (player.SpellManager.GetSpell(spell4BaseId) != null)
                return;

            player.SpellManager.AddSpell(spell4BaseId);
        }

        private void LogUnsupportedSpecialReward(uint rewardType, uint objectId, uint subObjectId, float value, uint nodeId, int slotIndex)
        {
            if (loggedUnsupportedRewardTypes.Add(rewardType))
            {
                log.Warn($"Unsupported Primal Matrix reward type {rewardType} encountered. First occurrence: node={nodeId}, slot={slotIndex}, objectId={objectId}, subObjectId={subObjectId}, value={value}.");
                return;
            }

            log.Trace($"Skipped unsupported Primal Matrix reward: type={rewardType}, node={nodeId}, slot={slotIndex}, objectId={objectId}, subObjectId={subObjectId}, value={value}.");
        }

        private void RebuildPropertyBonusesFromAllocations()
        {
            var rebuilt = new Dictionary<Property, float>();

            foreach ((uint nodeId, uint allocations) in nodeAllocations)
            {
                if (allocations == 0u)
                    continue;

                PrimalMatrixNodeEntry nodeEntry = GameTableManager.Instance.PrimalMatrixNode.GetEntry(nodeId);
                if (nodeEntry == null)
                    continue;

                uint rewardId = GetRewardIdForClass(nodeEntry, player.Class);
                if (rewardId == 0u)
                    continue;

                PrimalMatrixRewardEntry reward = GameTableManager.Instance.PrimalMatrixReward.GetEntry(rewardId);
                if (reward == null)
                    continue;

                AccumulatePropertyRewardSlot(reward.PrimalMatrixRewardTypeEnum0, reward.ObjectId0, reward.SubObjectId0, reward.Value0, allocations, rebuilt);
                AccumulatePropertyRewardSlot(reward.PrimalMatrixRewardTypeEnum1, reward.ObjectId1, reward.SubObjectId1, reward.Value1, allocations, rebuilt);
                AccumulatePropertyRewardSlot(reward.PrimalMatrixRewardTypeEnum2, reward.ObjectId2, reward.SubObjectId2, reward.Value2, allocations, rebuilt);
                AccumulatePropertyRewardSlot(reward.PrimalMatrixRewardTypeEnum3, reward.ObjectId3, reward.SubObjectId3, reward.Value3, allocations, rebuilt);
            }

            ApplyPropertyBonuses(rebuilt);
        }

        private static void AccumulatePropertyRewardSlot(uint rewardType, uint objectId, uint subObjectId, float value, uint allocations, Dictionary<Property, float> rebuilt)
        {
            if (rewardType != RewardTypeProperty || objectId == 0u || allocations == 0u)
                return;

            if (!Enum.IsDefined(typeof(Property), (int)objectId))
                return;

            float amount = ResolvePropertyRewardAmount(subObjectId, value);
            if (Math.Abs(amount) < 0.0001f)
                return;

            Property property = (Property)objectId;
            rebuilt.TryGetValue(property, out float current);
            rebuilt[property] = current + (amount * allocations);
        }

        private static float ResolvePropertyRewardAmount(uint subObjectId, float value)
        {
            if (Math.Abs(value) > 0.0001f)
                return value;

            if (subObjectId > 0u)
                return subObjectId;

            return 0f;
        }

        private void ApplyPropertyBonuses(Dictionary<Property, float> rebuilt)
        {
            var toClear = new List<Property>();
            foreach (Property property in appliedPropertyBonuses.Keys)
            {
                if (!rebuilt.ContainsKey(property))
                    toClear.Add(property);
            }

            foreach (Property property in toClear)
                player.SetPrimalMatrixProperty(property, 0f);

            foreach ((Property property, float amount) in rebuilt)
                player.SetPrimalMatrixProperty(property, amount);

            appliedPropertyBonuses.Clear();
            foreach ((Property property, float amount) in rebuilt)
                appliedPropertyBonuses[property] = amount;
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
                Amount = amount
            });
        }

        private void SendNodeUpdate(uint nodeId, uint allocations)
        {
            player.Session.EnqueueMessageEncrypted(new ServerPrimalMatrixNode
            {
                EntityId = player.Guid,
                NodeId = nodeId,
                EssenceId = 0u,
                Amount = allocations
            });
        }

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
                    var model = new CharacterPrimalMatrixModel
                    {
                        Id = player.CharacterId,
                        EssenceId = essenceId,
                        Amount = amount
                    };

                    EntityEntry<CharacterPrimalMatrixModel> entry = context.Attach(model);
                    entry.Property(p => p.Amount).IsModified = true;
                }
                else
                {
                    context.Add(new CharacterPrimalMatrixModel
                    {
                        Id = player.CharacterId,
                        EssenceId = essenceId,
                        Amount = amount
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
                if (!nodeAllocations.TryGetValue(nodeId, out uint allocations))
                    continue;

                if (loadedNodeIds.Contains(nodeId))
                {
                    var model = new CharacterPrimalMatrixNodeModel
                    {
                        Id = player.CharacterId,
                        NodeId = nodeId,
                        Allocations = allocations
                    };

                    EntityEntry<CharacterPrimalMatrixNodeModel> entry = context.Attach(model);
                    entry.Property(p => p.Allocations).IsModified = true;
                }
                else
                {
                    context.Add(new CharacterPrimalMatrixNodeModel
                    {
                        Id = player.CharacterId,
                        NodeId = nodeId,
                        Allocations = allocations
                    });

                    loadedNodeIds.Add(nodeId);
                }
            }

            modifiedNodes.Clear();
        }

        public void SendInitialPackets()
        {
            SendEssenceUpdate(EssenceIdRed, GetEssenceAmount(EssenceIdRed));
            SendEssenceUpdate(EssenceIdBlue, GetEssenceAmount(EssenceIdBlue));
            SendEssenceUpdate(EssenceIdGreen, GetEssenceAmount(EssenceIdGreen));
            SendEssenceUpdate(EssenceIdPurple, GetEssenceAmount(EssenceIdPurple));

            foreach ((uint nodeId, uint alloc) in nodeAllocations)
                SendNodeUpdate(nodeId, alloc);

            // Keep property rewards in sync on login packet send path.
            RebuildPropertyBonusesFromAllocations();
        }
    }
}
