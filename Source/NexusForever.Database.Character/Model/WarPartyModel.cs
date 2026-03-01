namespace NexusForever.Database.Character.Model
{
    public class WarPartyModel
    {
        public ulong Id { get; set; }
        public int Rating { get; set; }
        public int SeasonWins { get; set; }
        public int SeasonLosses { get; set; }
        public int BossTokens { get; set; }
        public string PlugSlots { get; set; } // JSON serialized Dictionary<byte, ushort>

        public GuildModel Guild { get; set; }
    }
}
