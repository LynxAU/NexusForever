using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Instance;
using NexusForever.Game.Abstract.Map.Instance;

namespace NexusForever.WorldServer.Network.Message.Handler.Instance
{
    /// <summary>
    /// Handles the client choosing to leave while pending removal from an instance
    /// </summary>
    public class ClientLeavePendingRemovalInstanceHandler : IMessageHandler<IWorldSession, ClientLeavePendingRemovalInstance>
    {
        public void HandleMessage(IWorldSession session, ClientLeavePendingRemovalInstance leavePendingRemovalInstance)
        {
            if (session.Player.Map is not IMapInstance instance)
                return;

            instance.TryLeavePendingRemoval(session.Player);
        }
    }
}
