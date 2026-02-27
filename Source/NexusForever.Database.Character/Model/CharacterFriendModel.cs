namespace NexusForever.Database.Character.Model
{
    public class CharacterFriendModel
    {
        public ulong Id { get; set; }
        public ulong CharacterId { get; set; }
        public ulong FriendCharacterId { get; set; }
        public byte Type { get; set; }
        public string Note { get; set; }

        public CharacterModel Character { get; set; }
    }
}
