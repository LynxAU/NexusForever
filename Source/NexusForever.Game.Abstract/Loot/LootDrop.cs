using NexusForever.Game.Static.Loot;

namespace NexusForever.Game.Abstract.Loot
{
    public class LootDrop
    {
        public uint ItemId { get; init; }
        public uint Count { get; init; }
        public LootSourceConfidence SourceConfidence { get; init; }
        public string EvidenceReference { get; init; }
    }
}
