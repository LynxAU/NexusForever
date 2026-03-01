using Microsoft.Extensions.Logging;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Crafting;

namespace NexusForever.WorldServer.Network.Message.Handler.Crafting
{
    public class ClientCraftingCraftItemHandler : IMessageHandler<IWorldSession, ClientCraftingCraftItem>
    {
        #region Dependency Injection

        private readonly ILogger<ClientCraftingCraftItemHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientCraftingCraftItemHandler(
            ILogger<ClientCraftingCraftItemHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientCraftingCraftItem craftRequest)
        {
            var  player   = (Player)session.Player;
            uint earnedXp = player.TradeskillManager.GetCraftRewardXp(craftRequest.TradeskillSchematic2Id) * craftRequest.SchematicCount;

            var result = player.TradeskillManager.CraftItem(
                craftRequest.TradeskillSchematic2Id,
                craftRequest.SchematicCount);

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
                log.LogWarning("Player {CharacterId} failed to craft schematic {SchematicId}: {Result}",
                    player.CharacterId, craftRequest.TradeskillSchematic2Id, result);
            }
            else
            {
                log.LogDebug("Player {CharacterId} crafted schematic {SchematicId} x{Count}",
                    player.CharacterId, craftRequest.TradeskillSchematic2Id, craftRequest.SchematicCount);

                session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.CraftSchematic, craftRequest.TradeskillSchematic2Id, craftRequest.SchematicCount);
                session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.Unknown20,      craftRequest.TradeskillSchematic2Id, craftRequest.SchematicCount);
            }
        }
    }
}
