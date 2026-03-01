using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Friend
{
    public class ClientFriendInviteResponseHandler : IMessageHandler<IWorldSession, ClientFriendInviteResponse>
    {
        public void HandleMessage(IWorldSession session, ClientFriendInviteResponse message)
        {
            session.Player.FriendManager.RespondToInvite(message.InviteId, message.Response);
        }
    }
}
