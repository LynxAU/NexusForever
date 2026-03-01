using Microsoft.Extensions.Logging;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Crafting;

namespace NexusForever.WorldServer.Network.Message.Handler.Crafting
{
    /// <summary>
    /// Handles the auto-craft request (batch crafting via the auto-craft UI button).
    /// Functionally identical to <see cref="ClientCraftingCraftItemHandler"/> but uses
    /// the <see cref="ClientCraftingCraftItemAutoCraft"/> message type.
    /// </summary>
    public class ClientCraftingCraftItemAutoCraftHandler : IMessageHandler<IWorldSession, ClientCraftingCraftItemAutoCraft>
    {
        #region Dependency Injection

        private readonly ILogger<ClientCraftingCraftItemAutoCraftHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientCraftingCraftItemAutoCraftHandler(
            ILogger<ClientCraftingCraftItemAutoCraftHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientCraftingCraftItemAutoCraft craftRequest)
        {
            var  player   = (Player)session.Player;
            uint count    = craftRequest.SchematicCount > 0 ? craftRequest.SchematicCount : 1u;
            uint earnedXp = player.TradeskillManager.GetCraftRewardXp(craftRequest.TradeskillSchematic2Id) * count;

            var result = player.TradeskillManager.CraftItem(craftRequest.TradeskillSchematic2Id, count);

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
                log.LogWarning("Player {CharacterId} failed auto-craft of schematic {SchematicId} x{Count}: {Result}",
                    player.CharacterId, craftRequest.TradeskillSchematic2Id, count, result);
            }
            else
            {
                log.LogDebug("Player {CharacterId} auto-crafted schematic {SchematicId} x{Count}",
                    player.CharacterId, craftRequest.TradeskillSchematic2Id, count);

                session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.CraftSchematic, craftRequest.TradeskillSchematic2Id, count);
                session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.Unknown20,      craftRequest.TradeskillSchematic2Id, count);
            }
        }
    }
}
