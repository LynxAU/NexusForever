using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Friend
{
    public class ClientFriendRemoveByNameHandler : IMessageHandler<IWorldSession, ClientFriendRemoveByName>
    {
        /// <summary>
        /// Handles removal of a friend by name
        /// </summary>
        public void HandleMessage(IWorldSession session, ClientFriendRemoveByName message)
        {
            // For now, we'll need to look up the character by name
            // This is a placeholder - in production would need PlayerManager lookup
            session.Player.FriendManager.RemoveFriend(0); // TODO: Implement name lookup
        }
    }
}
