using Microsoft.Extensions.Logging;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Quest;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Crafting;

namespace NexusForever.WorldServer.Network.Message.Handler.Crafting
{
    public class ClientCraftingCraftItemHandler : IMessageHandler<IWorldSession, ClientCraftingCraftItem>
    {
        private readonly ILogger<ClientCraftingCraftItemHandler> log;

        public ClientCraftingCraftItemHandler(ILogger<ClientCraftingCraftItemHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingCraftItem craftRequest)
        {
            var player = (Player)session.Player;
            var result = player.TradeskillManager.CraftItem(
                craftRequest.TradeskillSchematic2Id,
                craftRequest.SchematicCount);

            // Send crafting result
            session.EnqueueMessageEncrypted(new ServerCraftingFinish
            {
                Pass = result == CraftingResult.Success,
                TradeskillSchematic2IdCrafted = craftRequest.TradeskillSchematic2Id,
                Item2IdCrafted = 0, // Would need to get from schematic
                Unused = 0,
                HotOrCold = CraftingDiscovery.Cold,
                Direction = CraftingDirection.None,
                EarnedXp = 0 // Would need to calculate from game tables
            });

            if (result != CraftingResult.Success)
            {
                log.LogWarning("Player {CharacterId} failed to craft schematic {SchematicId}: {Result}",
                    session.Player.CharacterId, craftRequest.TradeskillSchematic2Id, result);
            }
            else
            {
                log.LogDebug("Player {CharacterId} crafted schematic {SchematicId} x{Count}",
                    session.Player.CharacterId, craftRequest.TradeskillSchematic2Id, craftRequest.SchematicCount);
                
                // Trigger Unknown20 (Cooking) quest objectives - for cooking recipe quests
                // This is in addition to CraftSchematic (33) which handles general crafting
                session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.Unknown20, craftRequest.TradeskillSchematic2Id, craftRequest.SchematicCount);
            }
        }
    }
}
