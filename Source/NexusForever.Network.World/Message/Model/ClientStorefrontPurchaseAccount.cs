using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientStorefrontPurchaseAccount)]
    public class ClientStorefrontPurchaseAccount : IReadable
    {
        public uint OfferId { get; set; }
        public byte Unknown1 { get; set; }
        public ushort CurrencyId { get; set; }
        public uint Unknown3 { get; set; }

        public void Read(GamePacketReader reader)
        {
            OfferId    = reader.ReadUInt(20);
            Unknown1   = reader.ReadByte(5);
            CurrencyId = reader.ReadUShort(14);
            Unknown3   = reader.ReadUInt(20);
        }
    }
}
