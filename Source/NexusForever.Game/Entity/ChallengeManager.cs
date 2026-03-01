using NexusForever.Game.Abstract.Challenge;
using NexusForever.Game.Abstract.Entity;
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

        public ChallengeManager(IPlayer owner)
        {
            player = owner;
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
            foreach (IChallenge challenge in challenges.Values.Where(c => c.IsActivated))
            {
                if (challenge.OnEntityKilled(creatureId))
                    FlushProgressNotifications(challenge);
            }
        }

        public void OnSpellCast(uint spell4Id)
        {
            foreach (IChallenge challenge in challenges.Values.Where(c => c.IsActivated))
            {
                if (challenge.OnSpellCast(spell4Id))
                    FlushProgressNotifications(challenge);
            }
        }

        public void OnItemCollected(uint itemId)
        {
            foreach (IChallenge challenge in challenges.Values.Where(c => c.IsActivated))
            {
                if (challenge.OnItemCollected(itemId))
                    FlushProgressNotifications(challenge);
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
