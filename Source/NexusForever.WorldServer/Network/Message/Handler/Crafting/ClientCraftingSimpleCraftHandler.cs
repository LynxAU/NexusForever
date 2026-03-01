using Microsoft.Extensions.Logging;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Crafting;

namespace NexusForever.WorldServer.Network.Message.Handler.Crafting
{
    public class ClientCraftingSimpleCraftHandler : IMessageHandler<IWorldSession, ClientCraftingSimpleCraft>
    {
        #region Dependency Injection

        private readonly ILogger<ClientCraftingSimpleCraftHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientCraftingSimpleCraftHandler(
            ILogger<ClientCraftingSimpleCraftHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientCraftingSimpleCraft craftRequest)
        {
            var  player   = (Player)session.Player;
            uint earnedXp = player.TradeskillManager.GetCraftRewardXp(craftRequest.TradeskillSchematic2Id);

            var result = player.TradeskillManager.CraftItem(craftRequest.TradeskillSchematic2Id, 1);

            var  schematic     = gameTableManager.TradeskillSchematic2.GetEntry(craftRequest.TradeskillSchematic2Id);
            uint craftedItemId = result == CraftingResult.Success ? (schematic?.Item2IdOutput ?? 0u) : 0u;

            session.EnqueueMessageEncrypted(new ServerCraftingFinish
            {
                Pass                          = result == CraftingResult.Success,
                TradeskillSchematic2IdCrafted = craftRequest.TradeskillSchematic2Id,
                Item2IdCrafted                = craftedItemId,
                Unused                        = 0,
                HotOrCold                     = CraftingDiscovery.Cold,
                Direction                     = CraftingDirection.None,
                EarnedXp                      = result == CraftingResult.Success ? earnedXp : 0u
            });

            if (result != CraftingResult.Success)
            {
                log.LogWarning("Player {CharacterId} failed to simple craft schematic {SchematicId}: {Result}",
                    player.CharacterId, craftRequest.TradeskillSchematic2Id, result);
            }
            else
            {
                log.LogDebug("Player {CharacterId} simple crafted schematic {SchematicId}",
                    player.CharacterId, craftRequest.TradeskillSchematic2Id);

                session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.CraftSchematic, craftRequest.TradeskillSchematic2Id, 1u);
                session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.Unknown20,      craftRequest.TradeskillSchematic2Id, 1u);
            }
        }
    }
}
