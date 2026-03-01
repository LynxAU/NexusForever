namespace NexusForever.Database.Character.Model
{
    public class GuildDataModel
    {
        public ulong Id { get; set; }
        public string MessageOfTheDay { get; set; }
        public string AdditionalInfo { get; set; }
        public string RecruitmentDescription { get; set; }
        public uint RecruitmentDemand { get; set; }
        public uint RecruitmentMinLevel { get; set; }
        public uint Classification { get; set; }
        public ushort BackgroundIconPartId { get; set; }
        public ushort ForegroundIconPartId { get; set; }
        public ushort ScanLinesPartId { get; set; }

        public GuildModel Guild { get; set; }
    }
}
