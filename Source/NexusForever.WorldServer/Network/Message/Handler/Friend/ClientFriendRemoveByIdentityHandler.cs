using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Friend
{
    public class ClientFriendRemoveByIdentityHandler : IMessageHandler<IWorldSession, ClientFriendRemoveByIdentity>
    {
        /// <summary>
        /// Handles removal of a friend by identity
        /// </summary>
        public void HandleMessage(IWorldSession session, ClientFriendRemoveByIdentity message)
        {
            session.Player.FriendManager.RemoveFriend(message.PlayerIdentity.Id);
        }
    }
}
