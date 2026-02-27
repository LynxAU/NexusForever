using NexusForever.Database.Character;
using NexusForever.Game.Static.Friend;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Game.Abstract.Entity
{
    public interface IFriendManager
    {
        void Update(double lastTick);
        void Save(CharacterContext context);

        void SendFriendList();
        void SendFriendInviteList();

        FriendshipResult AddFriend(string name, string note);
        FriendshipResult RemoveFriend(ulong friendCharacterId);
        FriendshipResult SetFriendNote(ulong friendCharacterId, string note);

        void SendInitialPackets();
    }
}
