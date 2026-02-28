using System;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Entity
{
    [Message(GameMessageOpcode.ServerPrimalMatrixNode)]
    public class ServerPrimalMatrixNode : IWritable
    {
        public ulong EntityId { get; set; }
        public uint NodeId { get; set; }
        public uint EssenceId { get; set; }
        public uint Amount { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(EntityId, 64);
            writer.Write(NodeId, 32);
            writer.Write(EssenceId, 32);
            writer.Write(Amount, 32);
        }
    }
}
