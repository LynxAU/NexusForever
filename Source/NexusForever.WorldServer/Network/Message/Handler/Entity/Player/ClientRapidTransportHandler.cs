using NexusForever.Game.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity.Player
{
    public class ClientRapidTransportHandler : IMessageHandler<IWorldSession, ClientRapidTransport>
    {
        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;

        public ClientRapidTransportHandler(
            IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientRapidTransport rapidTransport)
        {
            // Validate taxi node exists
            TaxiNodeEntry taxiNode = gameTableManager.TaxiNode.GetEntry(rapidTransport.TaxiNode);
            if (taxiNode == null)
            {
                session.Player.SendGenericError(GenericError.UnknownTargetUnit);
                return;
            }

            // Check player level requirement
            if (session.Player.Level < taxiNode.AutoUnlockLevel)
            {
                session.Player.SendGenericError(GenericError.ItemLevelToLow);
                return;
            }

            // Validate world location exists
            WorldLocation2Entry worldLocation = gameTableManager.WorldLocation2.GetEntry(taxiNode.WorldLocation2Id);
            if (worldLocation == null)
            {
                session.Player.SendGenericError(GenericError.UnknownTargetUnit);
                return;
            }

            // Get the spell entry for rapid transport
            GameFormulaEntry entry = gameTableManager.GameFormula.GetEntry(1307);
            if (entry == null)
            {
                session.Player.SendGenericError(GenericError.UnknownTargetUnit);
                return;
            }

            // Cast the rapid transport spell
            // Spell system handles cooldown and cost validation internally
            session.Player.CastSpell(entry.Dataint0, new SpellParameters
            {
                TaxiNode = rapidTransport.TaxiNode
            });
        }
    }
}
