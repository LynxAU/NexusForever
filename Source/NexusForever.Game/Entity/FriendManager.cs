using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
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
            foreach (CharacterFriendModel friend in friends.Values)
            {
                context.CharacterFriend.Update(friend);
            }
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
            // Look up character by name
            var characterManager = PlayerManager.Instance;
            IPlayer targetPlayer = characterManager.GetPlayer(name);

            if (targetPlayer == null)
            {
                // TODO: Look up character from database if offline
                log.Warn($"Cannot add friend: {name} not found online or in database.");
                return FriendshipResult.PlayerOffline;
            }

            if (targetPlayer.CharacterId == player.CharacterId)
            {
                return FriendshipResult.CannotInviteSelf;
            }

            if (friends.ContainsKey(targetPlayer.CharacterId))
            {
                return FriendshipResult.PlayerAlreadyFriend;
            }

            // Check max friends limit
            if (friends.Count >= 100) // Typical max friends
            {
                return FriendshipResult.MaxFriends;
            }

            // Create new friend entry
            var friendModel = new CharacterFriendModel
            {
                Id = (ulong)DateTime.UtcNow.Ticks,
                CharacterId = player.CharacterId,
                FriendCharacterId = targetPlayer.CharacterId,
                Type = (byte)FriendshipType.Friend,
                Note = note ?? ""
            };

            friends.Add(targetPlayer.CharacterId, friendModel);

            // Send notification to the player who added the friend
            var friendData = new FriendData
            {
                FriendshipId = friendModel.Id,
                Type = FriendshipType.Friend,
                Note = friendModel.Note,
                PlayerIdentity = new Identity
                {
                    Id      = targetPlayer.CharacterId,
                    RealmId = RealmContext.Instance.RealmId
                }
            };

            player.Session.EnqueueMessageEncrypted(new ServerFriendAdd
            {
                Friend = friendData
            });

            log.Debug($"Player {player.Name} added {targetPlayer.Name} as friend.");

            return FriendshipResult.RequestSent;
        }

        public FriendshipResult RemoveFriend(ulong friendCharacterId)
        {
            if (!friends.TryGetValue(friendCharacterId, out var friendModel))
            {
                return FriendshipResult.FriendshipNotFound;
            }

            friends.Remove(friendCharacterId);

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
