namespace NexusForever.Database.Character.Model
{
    public class ArenaTeamModel
    {
        public ulong Id { get; set; }
        public int Rating { get; set; }
        public int SeasonWins { get; set; }
        public int SeasonLosses { get; set; }

        public GuildModel Guild { get; set; }
    }
}
