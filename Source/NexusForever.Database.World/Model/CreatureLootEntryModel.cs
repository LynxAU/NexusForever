namespace NexusForever.Database.World.Model
{
    public class CreatureLootEntryModel
    {
        public uint CreatureId { get; set; }
        public uint LootGroupId { get; set; }
        public uint ItemId { get; set; }
        public byte Context { get; set; }
        public byte SourceConfidence { get; set; }
        public uint MinCount { get; set; }
        public uint MaxCount { get; set; }
        public float DropRate { get; set; }
        public string EvidenceRef { get; set; }
        public bool Enabled { get; set; }
    }
}
