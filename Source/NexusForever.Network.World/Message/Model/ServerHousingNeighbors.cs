using System.Collections.Generic;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerHousingNeighbors)]
    public class ServerHousingNeighbors : IWritable
    {
        public List<Neighbor> Neighbors { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Neighbors.Count);
            foreach (var neighbor in Neighbors)
            {
                neighbor.Write(writer);
            }
        }

        public class Neighbor : IWritable
        {
            public ulong ResidenceId { get; set; }
            public string Name { get; set; }
            public byte PrivacyLevel { get; set; }
            public bool IsPending { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(ResidenceId);
                writer.WriteStringWide(Name);
                writer.Write(PrivacyLevel);
                writer.Write(IsPending);
            }
        }
    }
}
