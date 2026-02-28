using System;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Entity
{
    [Message(GameMessageOpcode.ServerPrimalMatrixEssence)]
    public class ServerPrimalMatrixEssence : IWritable
    {
        public uint EssenceId { get; set; }
        public uint Amount { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(EssenceId, 32);
            writer.Write(Amount, 32);
        }
    }
}
