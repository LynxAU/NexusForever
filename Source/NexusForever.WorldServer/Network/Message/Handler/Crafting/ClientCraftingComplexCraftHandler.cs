using System;
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
    /// Handles the complex crafting minigame completion packet.
    /// The minigame runs client-side; this handler receives the circuit completion result,
    /// selects normal or crit output, and awards XP.
    /// </summary>
    public class ClientCraftingComplexCraftHandler : IMessageHandler<IWorldSession, ClientCraftingComplexCraft>
    {
        // Percentage chance (0–100) of a crit craft when the schematic has a crit output item
        // and at least one circuit was completed by the player.
        private const int CritChancePercent = 15;

        private static readonly Random rng = new();

        #region Dependency Injection

        private readonly ILogger<ClientCraftingComplexCraftHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientCraftingComplexCraftHandler(
            ILogger<ClientCraftingComplexCraftHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientCraftingComplexCraft craftRequest)
        {
            var player = (Player)session.Player;

            var schematic = gameTableManager.TradeskillSchematic2.GetEntry(craftRequest.TradeskillSchematic2Id);
            if (schematic == null)
            {
                log.LogWarning("Player {CharacterId} sent ComplexCraft for unknown schematic {SchematicId}",
                    player.CharacterId, craftRequest.TradeskillSchematic2Id);
                return;
            }

            // Determine crit: requires the schematic to have a crit output, at least one circuit
            // completed by the player, and a random roll within the crit chance window.
            bool hasCritOutput = schematic.Item2IdOutputCrit != 0;
            bool circuitsMet   = craftRequest.CraftStats.CircuitComplete > 0;
            bool isCrit        = hasCritOutput && circuitsMet && rng.Next(100) < CritChancePercent;

            var result = player.TradeskillManager.CraftItemWithResult(
                craftRequest.TradeskillSchematic2Id,
                isCrit,
                out uint craftedItemId,
                out uint earnedXp);

            session.EnqueueMessageEncrypted(new ServerCraftingFinish
            {
                Pass                          = result == CraftingResult.Success,
                TradeskillSchematic2IdCrafted = craftRequest.TradeskillSchematic2Id,
                Item2IdCrafted                = craftedItemId,
                Unused                        = 0,
                HotOrCold                     = isCrit ? CraftingDiscovery.Hot : CraftingDiscovery.Cold,
                Direction                     = CraftingDirection.None,
                EarnedXp                      = result == CraftingResult.Success ? earnedXp : 0u
            });

            if (result != CraftingResult.Success)
            {
                log.LogWarning("Player {CharacterId} failed complex craft of schematic {SchematicId}: {Result}",
                    player.CharacterId, craftRequest.TradeskillSchematic2Id, result);
            }
            else
            {
                log.LogDebug("Player {CharacterId} complex crafted schematic {SchematicId} (crit: {IsCrit})",
                    player.CharacterId, craftRequest.TradeskillSchematic2Id, isCrit);

                session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.CraftSchematic, craftRequest.TradeskillSchematic2Id, 1u);
                session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.Unknown20,      craftRequest.TradeskillSchematic2Id, 1u);
            }
        }
    }
}
