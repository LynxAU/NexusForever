using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Friend
{
    public class ClientFriendAddHandler : IMessageHandler<IWorldSession, ClientFriendAdd>
    {
        /// <summary>
        /// Handles adding a new friend by name
        /// </summary>
        public void HandleMessage(IWorldSession session, ClientFriendAdd message)
        {
            session.Player.FriendManager.AddFriend(message.Name, message.Note);
        }
    }
}
