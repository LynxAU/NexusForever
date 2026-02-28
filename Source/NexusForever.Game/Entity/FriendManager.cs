using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Character;
using NexusForever.Game;
using NexusForever.Game.Static.Friend;
using NexusForever.GameTable;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game;
using NLog;

namespace NexusForever.Game.Entity
{
    public class FriendManager : IFriendManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private readonly IPlayer player;
        private readonly Dictionary<ulong, CharacterFriendModel> friends = new();

        // Tracks friends added this session that must be INSERTed rather than UPDATEd.
        private readonly HashSet<ulong> pendingCreate = new();
        // Tracks friend models removed this session that must be DELETEd from the DB.
        private readonly List<CharacterFriendModel> pendingDelete = new();

        public FriendManager(IPlayer owner, CharacterModel model)
        {
            player = owner;

            foreach (CharacterFriendModel friendModel in model.Friends)
            {
                friends.Add(friendModel.FriendCharacterId, friendModel);
            }
        }

        public void Update(double lastTick)
        {
            // Future: send friend location updates periodically
        }

        public void Save(CharacterContext context)
        {
            foreach (CharacterFriendModel friend in pendingDelete)
                context.CharacterFriend.Remove(friend);
            pendingDelete.Clear();

            foreach (CharacterFriendModel friend in friends.Values)
            {
                if (pendingCreate.Contains(friend.FriendCharacterId))
                    context.CharacterFriend.Add(friend);
                else
                    context.CharacterFriend.Update(friend);
            }
            pendingCreate.Clear();
        }

        public void SendFriendList()
        {
            var friendDataList = new List<FriendData>();

            foreach (CharacterFriendModel friend in friends.Values)
            {
                var friendData = new FriendData
                {
                    FriendshipId = friend.Id,
                    Type = (FriendshipType)friend.Type,
                    Note = friend.Note,
                    PlayerIdentity = new Identity
                    {
                        Id      = friend.FriendCharacterId,
                        RealmId = RealmContext.Instance.RealmId
                    }
                };

                friendDataList.Add(friendData);
            }

            player.Session.EnqueueMessageEncrypted(new ServerFriendList
            {
                Friends = friendDataList
            });
        }

        public void SendFriendInviteList()
        {
            // TODO: Implement invite friend list if there's pending invites storage
            player.Session.EnqueueMessageEncrypted(new ServerFriendInviteList
            {
                Invites = new List<ServerFriendInviteList.InviteData>()
            });
        }

        public FriendshipResult AddFriend(string name, string note)
        {
            // Resolve target: prefer online player, fall back to CharacterManager cache (offline characters).
            ulong targetId;
            string targetName;

            IPlayer onlinePlayer = PlayerManager.Instance.GetPlayer(name);
            if (onlinePlayer != null)
            {
                targetId   = onlinePlayer.CharacterId;
                targetName = onlinePlayer.Name;
            }
            else
            {
                ICharacter offlineCharacter = CharacterManager.Instance.GetCharacter(name);
                if (offlineCharacter == null)
                {
                    log.Warn($"Cannot add friend: {name} not found.");
                    return FriendshipResult.PlayerNotFound;
                }

                targetId   = offlineCharacter.CharacterId;
                targetName = offlineCharacter.Name;
            }

            if (targetId == player.CharacterId)
                return FriendshipResult.CannotInviteSelf;

            if (friends.ContainsKey(targetId))
                return FriendshipResult.PlayerAlreadyFriend;

            if (friends.Count >= 100)
                return FriendshipResult.MaxFriends;

            var friendModel = new CharacterFriendModel
            {
                Id                = (ulong)DateTime.UtcNow.Ticks,
                CharacterId       = player.CharacterId,
                FriendCharacterId = targetId,
                Type              = (byte)FriendshipType.Friend,
                Note              = note ?? ""
            };

            friends.Add(targetId, friendModel);
            pendingCreate.Add(targetId);

            player.Session.EnqueueMessageEncrypted(new ServerFriendAdd
            {
                Friend = new FriendData
                {
                    FriendshipId = friendModel.Id,
                    Type         = FriendshipType.Friend,
                    Note         = friendModel.Note,
                    PlayerIdentity = new Identity
                    {
                        Id      = targetId,
                        RealmId = RealmContext.Instance.RealmId
                    }
                }
            });

            log.Debug($"Player {player.Name} added {targetName} as friend.");
            return FriendshipResult.RequestSent;
        }

        public FriendshipResult RemoveFriend(ulong friendCharacterId)
        {
            if (!friends.TryGetValue(friendCharacterId, out var friendModel))
            {
                return FriendshipResult.FriendshipNotFound;
            }

            friends.Remove(friendCharacterId);

            // If the friend was added this session and never persisted, just discard it.
            // Otherwise queue a DELETE for the next Save().
            if (!pendingCreate.Remove(friendCharacterId))
                pendingDelete.Add(friendModel);

            player.Session.EnqueueMessageEncrypted(new ServerFriendRemove
            {
                FriendshipId = friendModel.Id
            });

            log.Debug($"Player {player.Name} removed friend {friendCharacterId}.");

            return FriendshipResult.Ok;
        }

        public FriendshipResult SetFriendNote(ulong friendCharacterId, string note)
        {
            if (!friends.TryGetValue(friendCharacterId, out var friendModel))
            {
                return FriendshipResult.FriendshipNotFound;
            }

            friendModel.Note = note ?? "";

            player.Session.EnqueueMessageEncrypted(new ServerFriendSetNote
            {
                FriendshipId = friendModel.Id,
                Note = friendModel.Note
            });

            log.Debug($"Player {player.Name} updated note for friend {friendCharacterId}.");

            return FriendshipResult.Ok;
        }

        public void SendInitialPackets()
        {
            SendFriendList();
            SendFriendInviteList();
        }
    }
}
