namespace NexusForever.Database.Character.Model
{
    public class CharacterPathExplorerNodeModel
    {
        public ulong Id { get; set; }
        public uint MissionId { get; set; }
        public uint NodeIndex { get; set; }

        public CharacterModel Character { get; set; }
    }
}
