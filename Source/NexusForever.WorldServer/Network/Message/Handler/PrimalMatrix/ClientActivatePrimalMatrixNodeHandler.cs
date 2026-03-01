using NexusForever.Game.Static.Quest;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.PrimalMatrix
{
    public class ClientActivatePrimalMatrixNodeHandler : IMessageHandler<IWorldSession, ClientActivatePrimalMatrixNode>
    {
        public void HandleMessage(IWorldSession session, ClientActivatePrimalMatrixNode message)
        {
            // BeginMatrix quest objective fires the first time a player activates a node.
            session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.BeginMatrix, 0u, 1u);
            session.Player.PrimalMatrixManager.ActivateNode(message.NodeId);
        }
    }
}
