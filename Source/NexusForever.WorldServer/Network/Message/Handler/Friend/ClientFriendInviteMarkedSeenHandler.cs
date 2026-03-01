using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Friend
{
    public class ClientFriendInviteMarkedSeenHandler : IMessageHandler<IWorldSession, ClientFriendInviteMarkedSeen>
    {
        public void HandleMessage(IWorldSession session, ClientFriendInviteMarkedSeen message)
        {
            session.Player.FriendManager.MarkInvitesSeen(message.InvitesSeen);
        }
    }
}
