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
            // TODO: resolve portal entity -> worldId mapping when portal type is known
            session.EnqueueMessageEncrypted(new ServerInstanceResetResult());
        }
    }
}
