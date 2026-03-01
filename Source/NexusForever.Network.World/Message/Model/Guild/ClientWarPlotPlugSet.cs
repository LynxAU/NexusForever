using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model.Guild
{
    /// <summary>
    /// Client requests to set a warplot plug in a specific slot.
    /// </summary>
    [Message(GameMessageOpcode.ClientWarPlotPlugSet)]
    public class ClientWarPlotPlugSet : IReadable
    {
        public Identity GuildIdentity { get; private set; } = new();
        public byte PlugIndex { get; private set; }
        public ushort PlugItemId { get; private set; }

        public void Read(GamePacketReader reader)
        {
            GuildIdentity.Read(reader);
            PlugIndex = reader.ReadByte();
            PlugItemId = reader.ReadUShort();
        }
    }
}
