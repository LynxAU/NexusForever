using NexusForever.Game.Static.Housing;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientHousingPlugUpdate)]
    public class ClientHousingPlugUpdate : IReadable
    {
        public Identity Identity { get; } = new();
        public uint PlotPropertyIndex { get; private set; }
        public uint PlugItemId { get; private set; }
        public uint PlotInfoId { get; private set; }
        public uint Unknown { get; private set; }
        public HousingPlugFacing PlugFacing { get; private set; }
        public uint[] ContributionTotals { get; } = new uint[5];

        public void Read(GamePacketReader reader)
        {
            Identity.Read(reader);

            PlotPropertyIndex = reader.ReadUInt();
            PlugItemId        = reader.ReadUInt();
            PlotInfoId        = reader.ReadUInt();
            Unknown           = reader.ReadUInt();
            PlugFacing        = (HousingPlugFacing)reader.ReadByte(3u);

            // HousingContribution related, client function that sends this looks up values from HousingContributionInfo.tbl
            for (int i = 0; i < ContributionTotals.Length; i++)
            {
                ContributionTotals[i] = reader.ReadUInt();
                reader.ReadUInt();
                reader.ReadUInt();
                reader.ReadUInt();
                reader.ReadUInt();
            }
        }
    }
}
