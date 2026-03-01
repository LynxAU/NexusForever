using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Path
{
    public class ClientPathSettlerImprovementBuildTierHandler : IMessageHandler<IWorldSession, ClientPathSettlerImprovementBuildTier>
    {
        public void HandleMessage(IWorldSession session, ClientPathSettlerImprovementBuildTier message)
        {
            session.Player.PathManager.HandleSettlerBuildEvent(message.PathSettlerImprovementGroupId, message.BuildTier);
        }
    }
}
