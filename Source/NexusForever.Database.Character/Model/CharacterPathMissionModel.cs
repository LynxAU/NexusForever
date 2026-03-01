namespace NexusForever.Database.Character.Model
{
    public class CharacterPathMissionModel
    {
        public ulong Id { get; set; }
        public uint MissionId { get; set; }
        public bool IsCompleted { get; set; }
        public uint Progress { get; set; }

        public CharacterModel Character { get; set; }
    }
}
