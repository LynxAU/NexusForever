using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Character;
using NexusForever.Game.Housing;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Housing
{
    public class ClientHousingNeighborListRequestHandler : IMessageHandler<IWorldSession, ClientHousingNeighborListRequest>
    {
        public void HandleMessage(IWorldSession session, ClientHousingNeighborListRequest request)
        {
            IResidence residence = session.Player.ResidenceManager.Residence;
            if (residence == null)
                return;

            var neighborList = new ServerHousingNeighbors();
            foreach (INeighbor neighbor in residence.GetNeighbors())
            {
                // Get the neighbor residence to get their name and privacy level
                IResidence neighborResidence = GlobalResidenceManager.Instance.GetResidence(neighbor.ResidenceId);
                if (neighborResidence == null)
                    continue;

                neighborList.Neighbors.Add(new ServerHousingNeighbors.Neighbor
                {
                    ResidenceId = neighbor.ResidenceId,
                    Name = neighborResidence.Name,
                    PrivacyLevel = (byte)neighborResidence.PrivacyLevel,
                    IsPending = neighbor.IsPending
                });
            }

            session.EnqueueMessageEncrypted(neighborList);
        }
    }
}
