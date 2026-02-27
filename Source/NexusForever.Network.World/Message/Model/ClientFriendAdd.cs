using NexusForever.Game.Static.Friend;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientFriendAdd)]
    public class ClientFriendAdd : IReadable
    {
        public string Name { get; private set; }
        public string RealmName { get; private set; }
        public string Note { get; private set; }
        public FriendshipType Type { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Name = reader.ReadWideString();
            RealmName = reader.ReadWideString();
            Note = reader.ReadWideString();
            Type = reader.ReadEnum<FriendshipType>(4u);
        }
    }
}
