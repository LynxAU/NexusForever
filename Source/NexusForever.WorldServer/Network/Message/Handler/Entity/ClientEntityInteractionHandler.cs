using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    public class ClientEntityInteractionHandler : IMessageHandler<IWorldSession, ClientEntityInteract>
    {
        #region Dependency Injection

        private readonly ILogger<ClientEntityInteractionHandler> log;

        private readonly IAssetManager assetManager;

        public ClientEntityInteractionHandler(
            ILogger<ClientEntityInteractionHandler> log,
            IAssetManager assetManager)
        {
            this.log          = log;
            this.assetManager = assetManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientEntityInteract entityInteraction)
        {
            IWorldEntity entity = session.Player.GetVisible<IWorldEntity>(entityInteraction.Guid);
            
            // Handle quest objectives based on interaction type
            bool isQuestNpc = entityInteraction.Event == 37;
            bool isTradeskillTrainer = entityInteraction.Event == 43;
            bool isTradeskillEngraving = entityInteraction.Event == 79;
            bool isMasterCraftsman = entityInteraction.Event == 87;
            bool isActivation = false;
            
            // Determine if this is an activation interaction (not a dialogue)
            // Events 8, 40-43, 45-48, 65-67, 69-76, 79-87, etc. are UI/windows - no quest objectives
            // Only certain events should trigger quest objectives
            if (entity != null)
            {
                // Quest NPC dialogue (event 37) triggers TalkTo objectives
                if (isQuestNpc)
                {
                    session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.TalkTo, entity.CreatureId, 1u);
                    foreach (uint targetGroupId in assetManager.GetTargetGroupsForCreatureId(entity.CreatureId) ?? Enumerable.Empty<uint>())
                        session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.TalkToTargetGroup, targetGroupId, 1u);
                }
                // Tradeskill trainer interaction (event 43) triggers LearnTradeskill objectives
                else if (isTradeskillTrainer)
                {
                    log.LogDebug("Triggering LearnTradeskill quest objective for player {PlayerId}, entity {EntityGuid}, event {Event}",
                        session.Player.CharacterId, entityInteraction.Guid, entityInteraction.Event);
                    session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.LearnTradeskill, 0, 1u);
                }
                // Tradeskill engraving station (event 79) triggers ObtainSchematic objectives
                else if (isTradeskillEngraving)
                {
                    log.LogDebug("Triggering ObtainSchematic quest objective for player {PlayerId}, entity {EntityGuid}, event {Event}",
                        session.Player.CharacterId, entityInteraction.Guid, entityInteraction.Event);
                    session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ObtainSchematic, 0, 1u);
                }
                // Master craftsman (event 87) triggers CraftSchematic objectives
                else if (isMasterCraftsman)
                {
                    log.LogDebug("Triggering CraftSchematic quest objective for player {PlayerId}, entity {EntityGuid}, event {Event}",
                        session.Player.CharacterId, entityInteraction.Guid, entityInteraction.Event);
                    session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.CraftSchematic, 0, 1u);
                }
                // Other interactions (not dialogue) trigger ActivateEntity
                else if (entityInteraction.Event != 49) // Skip vendors - they have their own handling
                {
                    isActivation = true;
                }
                
                // Trigger ActivateEntity for non-dialogue interactions
                if (isActivation)
                {
                    session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ActivateEntity, entity.CreatureId, 1u);
                    // Also check for ActivateTargetGroup if this entity is part of a target group
                    foreach (uint targetGroupId in assetManager.GetTargetGroupsForCreatureId(entity.CreatureId) ?? Enumerable.Empty<uint>())
                    {
                        session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ActivateTargetGroup, targetGroupId, 1u);
                        // ActivateTargetGroupChecklist is similar but for creatures with Activate spell effect
                        log.LogDebug("Triggering ActivateTargetGroupChecklist quest objective for player {PlayerId}, targetGroup {TargetGroupId}, creature {CreatureId}",
                            session.Player.CharacterId, targetGroupId, entity.CreatureId);
                        session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ActivateTargetGroupChecklist, targetGroupId, 1u);
                    }
                }
            }

            switch (entityInteraction.Event)
            {
                case 37: // Quest NPC
                {
                    session.EnqueueMessageEncrypted(new ServerDialogStart
                    {
                        DialogUnitId = entityInteraction.Guid
                    });
                    break;
                }
                case 49: // Handle Vendor
                    HandleVendor(session, entity);
                    break;
                case 68: // "MailboxActivate"
                    var mailboxEntity = session.Player.Map.GetEntity<IMailboxEntity>(entityInteraction.Guid);
                    break;
                case 8: // "HousingGuildNeighborhoodBrokerOpen"
                case 40:
                case 41: // "ResourceConversionOpen"
                case 42: // "ToggleAbilitiesWindow"
                case 43: // "InvokeTradeskillTrainerWindow"
                case 45: // "InvokeShuttlePrompt"
                case 46:
                case 47:
                case 48: // "InvokeTaxiWindow"
                case 65: // "MannequinWindowOpen"
                case 66: // "ShowBank"
                case 67: // "ShowRealmBank"
                case 69: // "ShowDye"
                case 70: // "GuildRegistrarOpen"
                case 71: // "WarPartyRegistrarOpen"
                case 72: // "GuildBankerOpen"
                case 73: // "WarPartyBankerOpen"
                case 75: // "ToggleMarketplaceWindow"
                case 76: // "ToggleAuctionWindow"
                case 79: // "TradeskillEngravingStationOpen"
                case 80: // "HousingMannequinOpen"
                case 81: // "CityDirectionsList"
                case 82: // "ToggleCREDDExchangeWindow"
                case 84: // "CommunityRegistrarOpen"
                case 85: // "ContractBoardOpen"
                case 86: // "BarberOpen"
                case 87: // "MasterCraftsmanOpen"
                default:
                    log.LogWarning($"Received unhandled interaction event {entityInteraction.Event} from Entity {entityInteraction.Guid}");
                    break;
            }
        }

        private void HandleVendor(IWorldSession session, IWorldEntity worldEntity)
        {
            if (worldEntity is not INonPlayerEntity vendorEntity)
                throw new InvalidOperationException();

            if (vendorEntity.VendorInfo == null)
                throw new InvalidOperationException();

            session.Player.SelectedVendorInfo = vendorEntity.VendorInfo;

            ServerVendorItemsUpdated vendorItemsUpdated = vendorEntity.VendorInfo.Build();
            vendorItemsUpdated.Guid = vendorEntity.Guid;
            session.EnqueueMessageEncrypted(vendorItemsUpdated);
        }
    }
}
