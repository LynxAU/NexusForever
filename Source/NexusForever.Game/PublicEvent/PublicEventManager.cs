using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Template;
using NexusForever.Shared;

namespace NexusForever.Game.PublicEvent
{
    public class PublicEventManager : IPublicEventManager
    {
        private IBaseMap map;
        private readonly Dictionary<uint, IPublicEvent> publicEvents = [];

        private readonly Dictionary<ulong, IPublicEventCharacter> characters = [];

        #region Dependency Injection

        private readonly ILogger<PublicEventManager> log;

        private readonly IPublicEventTemplateManager publicEventTemplateManager;
        private readonly IPublicEventFactory publicEventFactory;
        private readonly IFactory<IPublicEventCharacter> publicEventCharacterFactory;

        public PublicEventManager(
            ILogger<PublicEventManager> log,
            IPublicEventTemplateManager publicEventTemplateManager,
            IPublicEventFactory publicEventFactory,
            IFactory<IPublicEventCharacter> publicEventCharacterFactory)
        {
            this.log                         = log;

            this.publicEventTemplateManager  = publicEventTemplateManager;
            this.publicEventFactory          = publicEventFactory;
            this.publicEventCharacterFactory = publicEventCharacterFactory;
        }

        #endregion

        /// <summary>
        /// Initialise <see cref="PublicEventManager"/> with suppled <see cref="IBaseMap"/> owner.
        /// </summary>
        public void Initialise(IBaseMap map)
        {
            if (this.map != null)
                throw new InvalidOperationException();

            this.map = map;

            log.LogTrace($"Initialised public event manager for map {map.Entry.Id}.");
        }

        /// <summary>
        /// Force cleanup of all <see cref="IPublicEvent"/> and <see cref="IPublicEventCharacter"/>.
        /// </summary>
        /// <remarks>
        /// This should really only be called when the map is being unloaded.
        /// </remarks>
        public void Cleanup()
        {
            foreach (IPublicEvent @event in publicEvents.Values)
            {
                @event.Finish(null);
                @event.Dispose();
            }

            publicEvents.Clear();
            characters.Clear();
        }

        /// <summary>
        /// Invoked each world tick with the delta since the previous tick occurred.
        /// </summary>
        public void Update(double lastTick)
        {
            if (publicEvents.Count == 0)
                return;

            var toRemove = new List<IPublicEvent>();

            foreach (IPublicEvent @event in publicEvents.Values)
            {
                @event.Update(lastTick);
                if (@event.IsFinalised)
                    toRemove.Add(@event);
            }

            foreach (IPublicEvent @event in toRemove)
            {
                publicEvents.Remove(@event.Id);
                @event.Dispose();

                log.LogTrace($"Removed public event {@event.Id} for map {map.Entry.Id} from store.");
            }
        }

        /// <summary>
        /// Create a new <see cref="IPublicEvent"/> with supplied id.
        /// </summary>
        public IPublicEvent CreateEvent(uint id)
        {
            IPublicEventTemplate template = publicEventTemplateManager.GetTemplate(id);
            if (template == null)
                return null;

            if (template.Entry.WorldId != map.Entry.Id)
                throw new InvalidOperationException($"Public event {id} is not available in map {map.Entry.Id}!");

            IPublicEvent @event = publicEventFactory.CreateEvent(id);
            @event.Initialise(this, template, map);

            log.LogTrace($"Created new public event {id} for map {map.Entry.Id}.");

            publicEvents.Add(id, @event);
            return @event;
        }

        /// <summary>
        /// Return the <see cref="IPublicEvent"/> from owner <see cref="IBaseMap"/> with the supplied id.
        /// </summary>
        public IPublicEvent GetEvent(uint id)
        {
            return publicEvents.TryGetValue(id, out IPublicEvent @event) ? @event : null;
        }

        /// <summary>
        /// Return a collection of all <see cref="IPublicEvent"/>'s on the owner <see cref="IBaseMap"/>.
        /// </summary>
        public IEnumerable<IPublicEvent> GetEvents()
        {
            return publicEvents.Values;
        }

        /// <summary>
        /// Add character to <see cref="IPublicEvent"/>.
        /// </summary>
        public void AddEvent(ulong characterId, IPublicEvent publicEvent)
        {
            if (!characters.TryGetValue(characterId, out IPublicEventCharacter character))
                return;

            character.AddEvent(publicEvent);
        }

        /// <summary>
        /// Remove character from <see cref="IPublicEvent"/>.
        /// </summary>
        public void RemoveEvent(ulong characterId, IPublicEvent publicEvent)
        {
            if (!characters.TryGetValue(characterId, out IPublicEventCharacter character))
                return;

            character.RemoveEvent(publicEvent);
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is added to owner <see cref="IBaseMap"/>.
        /// </summary>
        public void OnAddToMap(IGridEntity gridEntity)
        {
            if (gridEntity is IPlayer player)
            {
                IPublicEventCharacter character = publicEventCharacterFactory.Resolve();
                character.Initialise(player);

                characters.TryAdd(player.CharacterId, character);

                log.LogTrace($"Public event information for character {player.CharacterId} added to map {map.Entry.Id} store.");
            }

            InvokeScriptCollection<IPublicEventScript>(s => s.OnAddToMap(gridEntity));
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is removed from owner <see cref="IBaseMap"/>.
        /// </summary>
        public void OnRemoveFromMap(IGridEntity gridEntity)
        {
            InvokeScriptCollection<IPublicEventScript>(s => s.OnRemoveFromMap(gridEntity));

            if (gridEntity is not IPlayer player)
                return;

            if (!characters.TryGetValue(player.CharacterId, out IPublicEventCharacter character))
                return;

            character.OnRemoveFromMap(player);

            characters.Remove(player.CharacterId);

            log.LogTrace($"Public event information for character {player.CharacterId} removed from map {map.Entry.Id} store.");
        }

        /// <summary>
        /// Update any objective for any public event <see cref="IPlayer"/> is part of that meets the supplied <see cref="PublicEventObjectiveType"/>, objectId and count.
        /// </summary>
        public void UpdateObjective(IPlayer player, PublicEventObjectiveType type, uint objectId, int count)
        {
            if (!characters.TryGetValue(player.CharacterId, out IPublicEventCharacter character))
                return;

            character.UpdateObjective(player, type, objectId, count);
        }

        /// <summary>
        /// Update stat for any public event <see cref="IPlayer"/> is part of with the supplied <see cref="PublicEventStat"/> and value.
        /// </summary>
        public void UpdateStat(IPlayer player, PublicEventStat stat, uint value)
        {
            if (!characters.TryGetValue(player.CharacterId, out IPublicEventCharacter character))
                return;

            character.UpdateStat(player, stat, value);
        }

        /// <summary>
        /// Update custom stat for any public event <see cref="IPlayer"/> is part of with the supplied index and value.
        /// </summary>
        public void UpdateCustomStat(IPlayer player, uint index, uint value)
        {
            if (!characters.TryGetValue(player.CharacterId, out IPublicEventCharacter character))
                return;

            character.UpdateCustomStat(player, index, value);
        }

        /// <summary>
        /// Respond to vote in a specific public event for the <see cref="IPlayer"/> with the supplied choice.
        /// </summary>
        public void RespondVote(IPlayer player, uint eventId, uint choice)
        {
            if (!characters.TryGetValue(player.CharacterId, out IPublicEventCharacter character))
                return;

            character.RespondVote(player, eventId, choice);
        }

        public void DistributeCompletionRewards(uint eventId, IEnumerable<ulong> participantCharacterIds)
        {
            IPublicEventTemplate template = publicEventTemplateManager.GetTemplate(eventId);
            if (template == null)
                return;

            uint rewardRotationContentId = template.Entry.RewardRotationContentId;
            if (rewardRotationContentId == 0u)
                return;

            List<RewardRotationItemEntry> rewardItems = GameTableManager.Instance.RewardRotationItem.Entries
                .Where(e => e != null && e.Id == rewardRotationContentId)
                .ToList();

            List<RewardRotationEssenceEntry> rewardEssence = GameTableManager.Instance.RewardRotationEssence.Entries
                .Where(e => e != null && e.Id == rewardRotationContentId)
                .ToList();

            if (rewardItems.Count == 0 && rewardEssence.Count == 0)
                return;

            HashSet<ulong> recipients = participantCharacterIds
                .Where(id => id != 0ul)
                .ToHashSet();

            if (recipients.Count == 0)
                return;

            var offlineRewards = new List<Action<CharacterContext>>();

            foreach (ulong characterId in recipients)
            {
                IPlayer onlinePlayer = PlayerManager.Instance.GetPlayer(characterId);
                if (onlinePlayer != null)
                {
                    ApplyOnlineRewards(onlinePlayer, rewardItems, rewardEssence);
                    continue;
                }

                QueueOfflineRewards(characterId, rewardItems, rewardEssence, offlineRewards);
            }

            if (offlineRewards.Count == 0)
                return;

            DatabaseManager.Instance.GetDatabase<CharacterDatabase>().Save(context =>
            {
                foreach (Action<CharacterContext> rewardAction in offlineRewards)
                    rewardAction(context);
            }).GetAwaiter().GetResult();
        }

        private static void ApplyOnlineRewards(IPlayer player, IEnumerable<RewardRotationItemEntry> rewardItems, IEnumerable<RewardRotationEssenceEntry> rewardEssence)
        {
            foreach (RewardRotationItemEntry reward in rewardItems)
            {
                if (reward.MinPlayerLevel > 0u && player.Level < reward.MinPlayerLevel)
                    continue;

                if (ItemManager.Instance.GetItemInfo(reward.RewardItemObject) != null)
                {
                    uint count = Math.Max(1u, reward.Count);
                    player.Inventory.ItemCreate(InventoryLocation.Inventory, reward.RewardItemObject, count);
                    continue;
                }

                if (Enum.IsDefined(typeof(CurrencyType), (int)reward.RewardItemObject))
                {
                    uint amount = Math.Max(1u, reward.Count);
                    player.CurrencyManager.CurrencyAddAmount((CurrencyType)reward.RewardItemObject, amount);
                }
            }

            foreach (RewardRotationEssenceEntry essenceReward in rewardEssence)
            {
                if (essenceReward.MinPlayerLevel > 0u && player.Level < essenceReward.MinPlayerLevel)
                    continue;

                if (!Enum.IsDefined(typeof(CurrencyType), (int)essenceReward.AccountCurrencyTypeId))
                    continue;

                player.CurrencyManager.CurrencyAddAmount((CurrencyType)essenceReward.AccountCurrencyTypeId, 1u);
            }
        }

        private static void QueueOfflineRewards(
            ulong characterId,
            IEnumerable<RewardRotationItemEntry> rewardItems,
            IEnumerable<RewardRotationEssenceEntry> rewardEssence,
            List<Action<CharacterContext>> queue)
        {
            foreach (RewardRotationItemEntry reward in rewardItems)
            {
                if (ItemManager.Instance.GetItemInfo(reward.RewardItemObject) != null)
                {
                    queue.Add(context => QueueOfflineItemReward(context, characterId, reward.RewardItemObject, Math.Max(1u, reward.Count)));
                    continue;
                }

                if (Enum.IsDefined(typeof(CurrencyType), (int)reward.RewardItemObject))
                {
                    queue.Add(context => QueueOfflineCurrencyReward(context, characterId, (CurrencyType)reward.RewardItemObject, Math.Max(1u, reward.Count)));
                }
            }

            foreach (RewardRotationEssenceEntry essenceReward in rewardEssence)
            {
                if (!Enum.IsDefined(typeof(CurrencyType), (int)essenceReward.AccountCurrencyTypeId))
                    continue;

                queue.Add(context => QueueOfflineCurrencyReward(context, characterId, (CurrencyType)essenceReward.AccountCurrencyTypeId, 1u));
            }
        }

        private static void QueueOfflineCurrencyReward(CharacterContext db, ulong recipientId, CurrencyType currencyType, ulong amount)
        {
            db.CharacterMail.Add(new CharacterMailModel
            {
                Id             = AssetManager.Instance.NextMailId,
                RecipientId    = recipientId,
                SenderType     = (byte)SenderType.GM,
                Subject        = "Public Event Reward",
                Message        = $"You received {amount} {currencyType} from a completed public event.",
                CurrencyType   = (byte)currencyType,
                CurrencyAmount = amount,
                DeliveryTime   = (byte)DeliverySpeed.Instant,
                CreateTime     = DateTime.UtcNow
            });
        }

        private static void QueueOfflineItemReward(CharacterContext db, ulong recipientId, uint itemId, uint count)
        {
            ulong itemGuid = ItemManager.Instance.NextItemId;
            ulong mailId = AssetManager.Instance.NextMailId;

            db.Item.Add(new ItemModel
            {
                Id         = itemGuid,
                OwnerId    = null,
                ItemId     = itemId,
                Location   = 0,
                BagIndex   = 0,
                StackCount = count,
                Charges    = 0,
                Durability = 1f
            });

            var mail = new CharacterMailModel
            {
                Id           = mailId,
                RecipientId  = recipientId,
                SenderType   = (byte)SenderType.GM,
                Subject      = "Public Event Reward",
                Message      = $"You received item reward {itemId} x{count} from a completed public event.",
                DeliveryTime = (byte)DeliverySpeed.Instant,
                CreateTime   = DateTime.UtcNow
            };

            mail.Attachment.Add(new CharacterMailAttachmentModel
            {
                Id       = mailId,
                Index    = 0,
                ItemGuid = itemGuid
            });

            db.CharacterMail.Add(mail);
        }

        private void InvokeScriptCollection<T>(Action<T> action)
        {
            foreach (IPublicEvent @event in publicEvents.Values)
                @event.InvokeScriptCollection(action);
        }
    }
}
