using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Instance;

namespace NexusForever.WorldServer.Network.Message.Handler.Instance
{
    /// <summary>
    /// Handles the client closing the instance settings dialog
    /// </summary>
    public class ClientClosedInstanceSettingsHandler : IMessageHandler<IWorldSession, ClientClosedInstanceSettings>
    {
        public void HandleMessage(IWorldSession session, ClientClosedInstanceSettings closedInstanceSettings)
        {
            // Client has closed the instance settings dialog
            // This is typically sent after the client has viewed/changed instance settings
            // No action needed in most cases, but could be used for analytics or state tracking
        }
    }
}
