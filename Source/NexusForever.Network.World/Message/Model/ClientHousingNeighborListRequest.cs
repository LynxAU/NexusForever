using NexusForever.Network.Message;
using NexusForever.Network;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientHousingNeighborListRequest)]
    public class ClientHousingNeighborListRequest : IReadable
    {
        public void Read(GamePacketReader reader)
        {
            // Empty message - client sends nothing
        }
    }
}
