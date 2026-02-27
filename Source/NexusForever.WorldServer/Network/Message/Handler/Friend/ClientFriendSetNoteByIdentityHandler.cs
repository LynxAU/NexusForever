using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Friend
{
    public class ClientFriendSetNoteByIdentityHandler : IMessageHandler<IWorldSession, ClientFriendSetNoteByIdentity>
    {
        /// <summary>
        /// Handles setting a friend's note by identity
        /// </summary>
        public void HandleMessage(IWorldSession session, ClientFriendSetNoteByIdentity message)
        {
            session.Player.FriendManager.SetFriendNote(message.PlayerIdentity.Id, message.Note);
        }
    }
}
