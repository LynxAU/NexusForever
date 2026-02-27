using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Friend
{
    public class ClientFriendSetNoteByNameHandler : IMessageHandler<IWorldSession, ClientFriendSetNoteByName>
    {
        /// <summary>
        /// Handles setting a friend's note by name
        /// </summary>
        public void HandleMessage(IWorldSession session, ClientFriendSetNoteByName message)
        {
            // For now, we'll need to look up the character by name
            // This is a placeholder - in production would need PlayerManager lookup
            // TODO: Implement name lookup
        }
    }
}
