using Microsoft.Extensions.Logging;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Quest;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Crafting;

namespace NexusForever.WorldServer.Network.Message.Handler.Crafting
{
    public class ClientCraftingSimpleCraftHandler : IMessageHandler<IWorldSession, ClientCraftingSimpleCraft>
    {
        private readonly ILogger<ClientCraftingSimpleCraftHandler> log;

        public ClientCraftingSimpleCraftHandler(ILogger<ClientCraftingSimpleCraftHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingSimpleCraft craftRequest)
        {
            // Simple craft is essentially the same as regular craft but without complex stats
            var player = (Player)session.Player;
            var result = player.TradeskillManager.CraftItem(
                craftRequest.TradeskillSchematic2Id,
                1);

            // Send crafting result
            session.EnqueueMessageEncrypted(new ServerCraftingFinish
            {
                Pass = result == CraftingResult.Success,
                TradeskillSchematic2IdCrafted = craftRequest.TradeskillSchematic2Id,
                Item2IdCrafted = 0,
                Unused = 0,
                HotOrCold = CraftingDiscovery.Cold,
                Direction = CraftingDirection.None,
                EarnedXp = 0
            });

            if (result != CraftingResult.Success)
            {
                log.LogWarning("Player {CharacterId} failed to simple craft schematic {SchematicId}: {Result}",
                    session.Player.CharacterId, craftRequest.TradeskillSchematic2Id, result);
            }
            else
            {
                log.LogDebug("Player {CharacterId} simple crafted schematic {SchematicId}",
                    session.Player.CharacterId, craftRequest.TradeskillSchematic2Id);
                
                // Trigger Unknown20 (Cooking) quest objectives - for cooking recipe quests
                session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.Unknown20, craftRequest.TradeskillSchematic2Id, 1);
            }
        }
    }
}
