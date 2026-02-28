using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientActivatePrimalMatrixNode)]
    public class ClientActivatePrimalMatrixNode : IReadable
    {
        public uint NodeId { get; private set; }

        public void Read(GamePacketReader reader)
        {
            NodeId = reader.ReadUInt();
        }
    }
}
