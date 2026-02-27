using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Instance;

namespace NexusForever.WorldServer.Network.Message.Handler.Instance
{
    /// <summary>
    /// Handles resetting all instance lockouts for the player
    /// </summary>
    public class ClientResetInstancesHandler : IMessageHandler<IWorldSession, ClientResetInstances>
    {
        public void HandleMessage(IWorldSession session, ClientResetInstances resetInstances)
        {
            // TODO: Reset all instance lockouts for the player
            // This would involve clearing any saved instance data from the database
            // and sending the appropriate response to the client
            
            session.EnqueueMessageEncrypted(new ServerInstanceResetResult());
        }
    }
}
