using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Instance;

namespace NexusForever.WorldServer.Network.Message.Handler.Instance
{
    /// <summary>
    /// Handles resetting a single instance lockout for the player
    /// </summary>
    public class ClientResetSingleInstanceHandler : IMessageHandler<IWorldSession, ClientResetSingleInstance>
    {
        public void HandleMessage(IWorldSession session, ClientResetSingleInstance resetSingleInstance)
        {
            // TODO: Reset instance lockout for the specific instance portal
            // InstancePortalUnitId identifies which instance to reset
            // This would involve clearing the saved instance data for that specific instance
            
            session.EnqueueMessageEncrypted(new ServerInstanceResetResult());
        }
    }
}
