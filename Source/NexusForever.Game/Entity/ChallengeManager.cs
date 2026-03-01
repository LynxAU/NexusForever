using System;
using System.Linq;
using NexusForever.Game.Abstract.Challenge;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Challenges;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Challenges;
using NLog;

namespace NexusForever.Game.Entity
{
    public class ChallengeManager : IChallengeManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private readonly IPlayer player;

        // Active (or recently completed) challenges keyed by challenge id
        private readonly Dictionary<uint, IChallenge> challenges = new();

        public ChallengeManager(IPlayer owner, CharacterModel model)
        {
            player = owner;

            foreach (CharacterChallengeModel challengeModel in model.Challenge)
            {
                IChallengeInfo info = Challenge.GlobalChallengeManager.Instance.GetChallengeInfo(challengeModel.ChallengeId);
                if (info == null)
                    continue;

                var challenge = new Challenge.Challenge(info);
                challenge.RestoreState(
                    challengeModel.IsUnlocked,
                    challengeModel.IsActivated,
                    challengeModel.IsCompleted,
                    challengeModel.IsOnCooldown,
                    challengeModel.CurrentCount,
                    challengeModel.CurrentTier,
                    challengeModel.CompletionCount,
                    challengeModel.TimeRemaining,
                    challengeModel.CooldownRemaining,
                    challengeModel.ActivatedDt);
                challenges[challenge.Id] = challenge;
            }
        }

        public void Save(CharacterContext context)
        {
            Dictionary<uint, CharacterChallengeModel> stored = context.CharacterChallenge
                .Where(c => c.Id == player.CharacterId)
                .ToDictionary(c => c.ChallengeId);

            foreach (IChallenge challenge in challenges.Values)
            {
                if (stored.TryGetValue(challenge.Id, out CharacterChallengeModel row))
                {
                    row.CurrentCount = challenge.CurrentCount;
                    row.CurrentTier = challenge.CurrentTier;
                    row.CompletionCount = challenge.CompletionCount;
                    row.IsUnlocked = challenge.IsUnlocked;
                    row.IsActivated = challenge.IsActivated;
                    row.IsCompleted = challenge.IsCompleted;
                    row.IsOnCooldown = challenge.IsOnCooldown;
                    row.TimeRemaining = challenge.TimeRemainingSeconds;
                    row.CooldownRemaining = challenge.CooldownRemainingSeconds;
                    row.ActivatedDt = challenge.ActivatedDt;
                    continue;
                }

                context.CharacterChallenge.Add(new CharacterChallengeModel
                {
                    Id = player.CharacterId,
                    ChallengeId = challenge.Id,
                    CurrentCount = challenge.CurrentCount,
                    CurrentTier = challenge.CurrentTier,
                    CompletionCount = challenge.CompletionCount,
                    IsUnlocked = challenge.IsUnlocked,
                    IsActivated = challenge.IsActivated,
                    IsCompleted = challenge.IsCompleted,
                    IsOnCooldown = challenge.IsOnCooldown,
                    TimeRemaining = challenge.TimeRemainingSeconds,
                    CooldownRemaining = challenge.CooldownRemainingSeconds,
                    ActivatedDt = challenge.ActivatedDt
                });
            }

            foreach (CharacterChallengeModel row in stored.Values.Where(r => !challenges.ContainsKey(r.ChallengeId)))
                context.CharacterChallenge.Remove(row);
        }

        public void Dispose()
        {
            challenges.Clear();
        }

        public void Update(double lastTick)
        {
            foreach (IChallenge challenge in challenges.Values)
            {
                bool expired = challenge.Update(lastTick);

                if (expired)
                {
                    SendResult(challenge.Id, ChallengeResult.TimerExpired);
                    SendUpdate(challenge);
                    continue;
                }

                // Flush tier notifications queued since last tick
                uint? tier = challenge.ConsumePendingTierNotify();
                if (tier.HasValue)
                    SendTierAchieved(challenge.Id, tier.Value);
            }
        }

        public void SendInitialPackets()
        {
            var packet = new ServerChallengeUpdate
            {
                ActiveChallenges = challenges.Values
                    .Select(c => c.Build())
                    .ToList()
            };

            player.Session.EnqueueMessageEncrypted(packet);
        }

        public void ChallengeActivate(uint challengeId)
        {
            // Lazily create the challenge on first activation
            if (!challenges.TryGetValue(challengeId, out IChallenge challenge))
            {
                IChallengeInfo info = Challenge.GlobalChallengeManager.Instance.GetChallengeInfo(challengeId);
                if (info == null)
                {
                    log.Warn($"Player {player.CharacterId} tried to activate unknown challenge {challengeId}.");
                    SendResult(challengeId, ChallengeResult.GenericFail);
                    return;
                }

                challenge = new Challenge.Challenge(info);
                challenges[challengeId] = challenge;
            }

            if (challenge.IsOnCooldown)
            {
                SendResult(challengeId, ChallengeResult.CooldownActive);
                return;
            }

            challenge.Activate();
            SendResult(challengeId, ChallengeResult.Activate);
            SendUpdate(challenge);
        }

        public void ChallengeAbandon(uint challengeId)
        {
            if (!challenges.TryGetValue(challengeId, out IChallenge challenge))
                return;

            challenge.Abandon();
            SendResult(challengeId, ChallengeResult.AbandonRemove);
            SendUpdate(challenge);
        }

        public void OnEntityKilled(uint creatureId)
        {
            AutoActivateChallenges(
                ChallengeType.Combat,
                info =>
                    (info.Target == 0u || info.Target == creatureId)
                    && MatchesCreatureCategory(info, creatureId));

            foreach (IChallenge challenge in challenges.Values.Where(c => c.IsActivated))
            {
                IChallengeInfo info = Challenge.GlobalChallengeManager.Instance.GetChallengeInfo(challenge.Id);
                if (info == null || !CanCountChallenge(info))
                    continue;

                if (!MatchesCreatureCategory(info, creatureId))
                    continue;

                if (challenge.OnEntityKilled(creatureId))
                    FlushProgressNotifications(challenge);
            }
        }

        public void OnSpellCast(uint spell4Id)
        {
            AutoActivateChallenges(
                ChallengeType.Ability,
                info => info.Target == 0u || info.Target == spell4Id);

            foreach (IChallenge challenge in challenges.Values.Where(c => c.IsActivated))
            {
                IChallengeInfo info = Challenge.GlobalChallengeManager.Instance.GetChallengeInfo(challenge.Id);
                if (info == null || !CanCountChallenge(info))
                    continue;

                if (challenge.OnSpellCast(spell4Id))
                    FlushProgressNotifications(challenge);
            }
        }

        public void OnItemCollected(uint itemId)
        {
            AutoActivateChallenges(
                ChallengeType.Item,
                info => info.Target == 0u || info.Target == itemId);
            AutoActivateChallenges(
                ChallengeType.Collect,
                info => info.Target == 0u || info.Target == itemId);

            foreach (IChallenge challenge in challenges.Values.Where(c => c.IsActivated))
            {
                IChallengeInfo info = Challenge.GlobalChallengeManager.Instance.GetChallengeInfo(challenge.Id);
                if (info == null || !CanCountChallenge(info))
                    continue;

                if (challenge.OnItemCollected(itemId))
                    FlushProgressNotifications(challenge);
            }
        }

        private bool CanCountChallenge(IChallengeInfo info)
        {
            if (info.RequiredWorldZoneId == 0u)
                return true;

            uint playerZone = player.Zone?.Id ?? 0u;
            return playerZone == info.RequiredWorldZoneId;
        }

        private static bool MatchesCreatureCategory(IChallengeInfo info, uint creatureId)
        {
            if (info.CreatureCategoryFilterId == 0u)
                return true;

            Creature2Entry creatureEntry = GameTableManager.Instance.Creature2.GetEntry(creatureId);
            if (creatureEntry == null)
                return false;

            // Retail data may encode this filter as either family or faction depending on challenge row.
            return creatureEntry.Creature2FamilyId == info.CreatureCategoryFilterId
                || creatureEntry.FactionId == info.CreatureCategoryFilterId;
        }

        private void AutoActivateChallenges(ChallengeType type, Func<IChallengeInfo, bool> predicate)
        {
            foreach (IChallengeInfo info in Challenge.GlobalChallengeManager.Instance.GetChallengesByType(type))
            {
                if (!CanCountChallenge(info) || !predicate(info))
                    continue;

                if (!challenges.TryGetValue(info.Id, out IChallenge challenge))
                {
                    challenge = new Challenge.Challenge(info);
                    challenges[info.Id] = challenge;
                }

                if (challenge.IsActivated || challenge.IsOnCooldown)
                    continue;

                challenge.Activate();
                SendResult(challenge.Id, ChallengeResult.Activate);
                SendUpdate(challenge);
            }
        }

        private void FlushProgressNotifications(IChallenge challenge)
        {
            uint? tier = challenge.ConsumePendingTierNotify();
            if (tier.HasValue)
                SendTierAchieved(challenge.Id, tier.Value);

            if (challenge.IsCompleted)
            {
                DeliverChallengeRewards(challenge.RewardTrackId);
                player.QuestManager.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, challenge.Id, 1u);
                SendResult(challenge.Id, ChallengeResult.Completed);
            }
            else
                SendUpdate(challenge);
        }

        private void DeliverChallengeRewards(uint rewardTrackId)
        {
            if (rewardTrackId == 0)
                return;

            foreach (RewardTrackRewardsEntry entry in GameTableManager.Instance.RewardTrackRewards.Entries
                .Where(e => e != null && e.RewardTrackId == rewardTrackId))
            {
                if (entry.CurrencyTypeId != 0 && entry.CurrencyAmount != 0)
                    player.CurrencyManager.CurrencyAddAmount((CurrencyType)entry.CurrencyTypeId, entry.CurrencyAmount);

                DeliverRewardSlot(entry.RewardTrackRewardTypeEnum00, entry.RewardChoiceId00, entry.RewardChoiceCount00);
                DeliverRewardSlot(entry.RewardTrackRewardTypeEnum01, entry.RewardChoiceId01, entry.RewardChoiceCount01);
                DeliverRewardSlot(entry.RewardTrackRewardTypeEnum02, entry.RewardChoiceId02, entry.RewardChoiceCount02);
            }
        }

        private void DeliverRewardSlot(uint typeEnum, uint id, uint count)
        {
            if (typeEnum == 0 || id == 0)
                return;

            switch ((QuestRewardType)typeEnum)
            {
                case QuestRewardType.Item:
                case QuestRewardType.AccountItem:
                    player.Inventory.ItemCreate(InventoryLocation.Inventory, id, count);
                    break;
                case QuestRewardType.Money:
                case QuestRewardType.AccountCurrency:
                    player.CurrencyManager.CurrencyAddAmount((CurrencyType)id, count);
                    break;
                case QuestRewardType.RotationEssence:
                    foreach (RewardRotationEssenceEntry essence in GameTableManager.Instance.RewardRotationEssence.Entries
                        .Where(e => e != null && e.Id == id))
                    {
                        if (!Enum.IsDefined(typeof(AccountCurrencyType), (int)essence.AccountCurrencyTypeId))
                            continue;

                        ulong amount = Math.Max(1u, count);
                        player.Account.CurrencyManager.CurrencyAddAmount((AccountCurrencyType)essence.AccountCurrencyTypeId, amount);
                    }
                    break;
                case QuestRewardType.GenericUnlock:
                case QuestRewardType.AccountGenericUnlock:
                    player.Account.GenericUnlockManager.Unlock((ushort)id);
                    break;
                default:
                    log.Warn($"Unhandled challenge reward slot type {typeEnum}!");
                    break;
            }
        }

        private void SendResult(uint challengeId, ChallengeResult result)
        {
            player.Session.EnqueueMessageEncrypted(new ServerChallengeResult
            {
                ChallengeId = (ushort)challengeId,
                Result      = result
            });
        }

        private void SendTierAchieved(uint challengeId, uint tier)
        {
            player.Session.EnqueueMessageEncrypted(new ServerChallengeResult
            {
                ChallengeId = (ushort)challengeId,
                Result      = ChallengeResult.TierAchieved,
                Data        = (int)tier
            });
        }

        private void SendUpdate(IChallenge challenge)
        {
            player.Session.EnqueueMessageEncrypted(new ServerChallengeUpdate
            {
                ActiveChallenges = new List<ServerChallengeUpdate.Challenge> { challenge.Build() }
            });
        }
    }
}
