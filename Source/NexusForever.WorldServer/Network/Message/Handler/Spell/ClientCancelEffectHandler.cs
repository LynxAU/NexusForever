using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientCancelEffectHandler : IMessageHandler<IWorldSession, ClientCancelEffect>
    {
        public void HandleMessage(IWorldSession session, ClientCancelEffect cancelSpell)
        {
            var activeSpell = session.Player.GetActiveSpell(s => s.CastingId == cancelSpell.ServerUniqueId);
            if (activeSpell == null)
                return;

            if (activeSpell.IsCasting)
            {
                session.Player.CancelSpellCast(cancelSpell.ServerUniqueId);
                return;
            }

            if (activeSpell.IsFinished)
                throw new InvalidPacketValueException();

            session.Player.EnqueueToVisible(new ServerSpellFinish
            {
                ServerUniqueId = cancelSpell.ServerUniqueId
            }, true);
        }
    }
}
