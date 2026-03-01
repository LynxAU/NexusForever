using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Path
{
    public class ClientPathExplorerProgressReportHandler : IMessageHandler<IWorldSession, ClientPathExplorerProgressReport>
    {
        public void HandleMessage(IWorldSession session, ClientPathExplorerProgressReport message)
        {
            session.Player.PathManager.HandleExplorerProgressReport(message.PathMissionId, message.ExplorerNodeIndex);
        }
    }
}
