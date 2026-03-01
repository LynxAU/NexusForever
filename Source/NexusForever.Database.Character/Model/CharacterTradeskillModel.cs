namespace NexusForever.Database.Character.Model
{
    public class CharacterTradeskillModel
    {
        public ulong Id { get; set; }
        public uint TradeskillId { get; set; }
        public uint CurrentXp { get; set; }
        public uint CurrentTier { get; set; }

        public virtual CharacterModel Character { get; set; }
    }
}
