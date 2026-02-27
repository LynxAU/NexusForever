namespace NexusForever.Database.Character.Model
{
    public class CharacterVirtualItemModel
    {
        public ulong Id { get; set; }
        public ushort VirtualItemId { get; set; }
        public uint Count { get; set; }

        public CharacterModel Character { get; set; }
    }
}
