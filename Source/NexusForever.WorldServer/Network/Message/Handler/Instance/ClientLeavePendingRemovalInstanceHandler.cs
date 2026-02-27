using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Instance;

namespace NexusForever.WorldServer.Network.Message.Handler.Instance
{
    /// <summary>
    /// Handles the client choosing to leave while pending removal from an instance
    /// </summary>
    public class ClientLeavePendingRemovalInstanceHandler : IMessageHandler<IWorldSession, ClientLeavePendingRemovalInstance>
    {
        public void HandleMessage(IWorldSession session, ClientLeavePendingRemovalInstance leavePendingRemovalInstance)
        {
            // Client chooses to leave while in pending removal state from an instance
            // This is sent after receiving ServerPendingWorldRemoval message
            // The player should be teleported out of the instance
            
            // TODO: Implement proper instance removal logic
            // This would involve removing the player from the instance and sending them to return location
        }
    }
}
