using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Character;
using NexusForever.Game.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Friend
{
    public class ClientFriendRemoveByNameHandler : IMessageHandler<IWorldSession, ClientFriendRemoveByName>
    {
        public void HandleMessage(IWorldSession session, ClientFriendRemoveByName message)
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
                return; // character not found; client will time out gracefully

            session.Player.FriendManager.RemoveFriend(targetId.Value);
        }
    }
}
