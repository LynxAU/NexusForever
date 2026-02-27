using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientSpellInteractionResultHandler : IMessageHandler<IWorldSession, ClientSpellInteractionResult>
    {
        public void HandleMessage(IWorldSession session, ClientSpellInteractionResult message)
        {
            // Result: 0 = fail, 1 = success, 2 = cancel
            if (message.Result != 1)
                return;

            // CastingId refers to an active cast, not a visible world entity.
            var spell = session.Player.GetActiveSpell(s => s.CastingId == message.CastingId);
            if (spell == null)
                return;

            // The CSI object guid is tracked as the spell's primary target.
            uint targetGuid = spell.Parameters.PrimaryTargetId;
            if (targetGuid == 0)
                return;

            var gridEntity = session.Player.GetVisible<IGridEntity>(targetGuid);
            if (gridEntity is not IWorldEntity targetEntity)
                return;

            session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.SucceedCSI, targetEntity.CreatureId, 1u);
        }
    }
}
