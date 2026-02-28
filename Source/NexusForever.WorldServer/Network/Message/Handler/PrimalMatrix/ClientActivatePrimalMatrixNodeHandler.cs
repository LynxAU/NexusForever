using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.PrimalMatrix
{
    public class ClientActivatePrimalMatrixNodeHandler : IMessageHandler<IWorldSession, ClientActivatePrimalMatrixNode>
    {
        public void HandleMessage(IWorldSession session, ClientActivatePrimalMatrixNode message)
        {
            session.Player.PrimalMatrixManager.ActivateNode(message.NodeId);
        }
    }
}
