namespace NexusForever.Database.Character.Model
{
    public class ResidencePlotModel
    {
        public ulong Id { get; set; }
        public byte Index { get; set; }
        public ushort PlotInfoId { get; set; }
        public ushort PlugItemId { get; set; }
        public byte PlugFacing { get; set; }
        public byte BuildState { get; set; }

        // Warplot fields
        public uint UpgradeLevel { get; set; }
        public ushort WarplotPlugItemId { get; set; }

        // Upkeep fields
        public uint UpkeepCharges { get; set; }
        public float UpkeepTime { get; set; }
        public uint ContributionTotal0 { get; set; }
        public uint ContributionTotal1 { get; set; }
        public uint ContributionTotal2 { get; set; }
        public uint ContributionTotal3 { get; set; }
        public uint ContributionTotal4 { get; set; }

        public ResidenceModel Residence { get; set; }
    }
}
