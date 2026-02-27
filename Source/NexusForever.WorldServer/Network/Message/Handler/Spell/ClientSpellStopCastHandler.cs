using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientSpellStopCastHandler : IMessageHandler<IWorldSession, ClientSpellStopCast>
    {
        public void HandleMessage(IWorldSession session, ClientSpellStopCast spellStopCast)
        {
            CastResult result = spellStopCast.CastResult == CastResult.SpellInterrupted
                ? CastResult.SpellInterrupted
                : CastResult.SpellCancelled;

            session.Player.CancelSpellCast(spellStopCast.CastingId, result);
        }
    }
}
