using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Character;
using NexusForever.Game.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Friend
{
    public class ClientFriendSetNoteByNameHandler : IMessageHandler<IWorldSession, ClientFriendSetNoteByName>
    {
        public void HandleMessage(IWorldSession session, ClientFriendSetNoteByName message)
        {
            ulong? targetId = null;

            IPlayer online = PlayerManager.Instance.GetPlayer(message.Name);
            if (online != null)
                targetId = online.CharacterId;
            else
            {
                var offline = CharacterManager.Instance.GetCharacter(message.Name);
                if (offline != null)
                    targetId = offline.CharacterId;
            }

            if (targetId == null)
                return;

            session.Player.FriendManager.SetFriendNote(targetId.Value, message.Note);
        }
    }
}
