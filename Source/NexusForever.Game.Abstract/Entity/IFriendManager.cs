using System.Collections.Generic;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Static.Friend;

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
        bool IsBlocked(ulong characterId);

        /// <summary>Respond to a pending friend invite (Accept/Decline/Ignore).</summary>
        FriendshipResult RespondToInvite(ulong inviteId, FriendshipResponse response);

        /// <summary>Mark a list of pending invites as seen in the UI.</summary>
        void MarkInvitesSeen(IEnumerable<ulong> inviteIds);

        /// <summary>
        /// Called when another online player sends an invite to this player.
        /// Adds the invite to the in-memory list and refreshes the UI packet.
        /// </summary>
        void ReceiveInvite(CharacterFriendInviteModel invite);

        /// <summary>
        /// Called when a receiver accepts our invite (we were the sender).
        /// Adds the friendship to in-memory state and notifies the client.
        /// </summary>
        void FriendAddedExternally(CharacterFriendModel friendModel);

        void SendInitialPackets();
    }
}
