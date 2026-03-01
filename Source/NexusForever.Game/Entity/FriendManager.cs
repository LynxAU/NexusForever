using System;
using System.Collections.Generic;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Character;
using NexusForever.Game;
using NexusForever.Game.Static.Friend;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Shared.Game;
using NLog;

namespace NexusForever.Game.Entity
{
    public class FriendManager : IFriendManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private readonly IPlayer player;

        // Accepted friendships (keyed by FriendCharacterId)
        private readonly Dictionary<ulong, CharacterFriendModel> friends = new();
        private readonly HashSet<ulong> friendsPendingCreate = new();
        private readonly List<CharacterFriendModel> friendsPendingDelete = new();

        // Pending invites received by this player (keyed by InviteId)
        private readonly Dictionary<ulong, CharacterFriendInviteModel> incomingInvites = new();
        private readonly HashSet<ulong> incomingInvitesSeen = new();

        // Invites this player sent this session — INSERT at next Save
        private readonly List<CharacterFriendInviteModel> outgoingInvitesPendingCreate = new();

        // Invites to DELETE (accepted or declined)
        private readonly List<CharacterFriendInviteModel> invitesPendingDelete = new();

        // Friend records for offline senders inserted in receiver's Save when they accept
        private readonly List<CharacterFriendModel> crossFriendsPendingCreate = new();

        public FriendManager(IPlayer owner, CharacterModel model)
        {
            player = owner;

            foreach (CharacterFriendModel friendModel in model.Friends)
                friends[friendModel.FriendCharacterId] = friendModel;

            foreach (CharacterFriendInviteModel invite in model.FriendInvitesReceived)
                incomingInvites[invite.Id] = invite;
        }

        public void Update(double lastTick)
        {
            // Future: periodic friend status / location updates
        }

        public void Save(CharacterContext context)
        {
            // ---- accepted friends ----
            foreach (CharacterFriendModel friend in friendsPendingDelete)
                context.CharacterFriend.Remove(friend);
            friendsPendingDelete.Clear();

            foreach (CharacterFriendModel friend in friends.Values)
            {
                if (friendsPendingCreate.Contains(friend.FriendCharacterId))
                    context.CharacterFriend.Add(friend);
                else
                    context.CharacterFriend.Update(friend);
            }
            friendsPendingCreate.Clear();

            // Cross-friend records written on behalf of offline senders when receiver accepts
            foreach (CharacterFriendModel cross in crossFriendsPendingCreate)
                context.CharacterFriend.Add(cross);
            crossFriendsPendingCreate.Clear();

            // ---- invites ----
            foreach (CharacterFriendInviteModel invite in outgoingInvitesPendingCreate)
                context.CharacterFriendInvite.Add(invite);
            outgoingInvitesPendingCreate.Clear();

            foreach (CharacterFriendInviteModel invite in invitesPendingDelete)
                context.CharacterFriendInvite.Remove(invite);
            invitesPendingDelete.Clear();

            foreach (ulong id in incomingInvitesSeen)
            {
                if (incomingInvites.TryGetValue(id, out CharacterFriendInviteModel invite))
                    context.CharacterFriendInvite.Update(invite);
            }
            incomingInvitesSeen.Clear();
        }

        // ---- Friend list ----

        public void SendFriendList()
        {
            var list = new List<FriendData>();
            foreach (CharacterFriendModel friend in friends.Values)
            {
                list.Add(new FriendData
                {
                    FriendshipId   = friend.Id,
                    Type           = (FriendshipType)friend.Type,
                    Note           = friend.Note,
                    PlayerIdentity = new Identity { Id = friend.FriendCharacterId, RealmId = RealmContext.Instance.RealmId }
                });
            }

            player.Session.EnqueueMessageEncrypted(new ServerFriendList { Friends = list });
        }

        public void SendFriendInviteList()
        {
            var list = new List<ServerFriendInviteList.InviteData>();
            foreach (CharacterFriendInviteModel invite in incomingInvites.Values)
            {
                ICharacter sender = CharacterManager.Instance.GetCharacter(invite.SenderId);
                if (sender == null)
                    continue;

                list.Add(new ServerFriendInviteList.InviteData
                {
                    InviteId       = invite.Id,
                    PlayerIdentity = new Identity { Id = invite.SenderId, RealmId = RealmContext.Instance.RealmId },
                    Seen           = invite.Seen,
                    ExpiryInDays   = 30f,
                    Note           = invite.Note,
                    Name           = sender.Name,
                    Class          = sender.Class,
                    Path           = sender.Path,
                    Level          = (byte)Math.Min(sender.Level, 255u)
                });
            }

            player.Session.EnqueueMessageEncrypted(new ServerFriendInviteList { Invites = list });
        }

        // ---- Adding / removing friends ----

        public FriendshipResult AddFriend(string name, string note)
        {
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

            var invite = new CharacterFriendInviteModel
            {
                Id         = (ulong)DateTime.UtcNow.Ticks,
                SenderId   = player.CharacterId,
                ReceiverId = targetId,
                Note       = note ?? "",
                CreatedAt  = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Seen       = 0
            };

            outgoingInvitesPendingCreate.Add(invite);

            IPlayer receiver = PlayerManager.Instance.GetPlayer(targetId);
            receiver?.FriendManager.ReceiveInvite(invite);

            log.Debug($"Player {player.Name} sent friend invite to {targetName}.");
            return FriendshipResult.RequestSent;
        }

        public FriendshipResult RemoveFriend(ulong friendCharacterId)
        {
            if (!friends.TryGetValue(friendCharacterId, out CharacterFriendModel friendModel))
                return FriendshipResult.FriendshipNotFound;

            friends.Remove(friendCharacterId);

            if (!friendsPendingCreate.Remove(friendCharacterId))
                friendsPendingDelete.Add(friendModel);

            player.Session.EnqueueMessageEncrypted(new ServerFriendRemove { FriendshipId = friendModel.Id });

            log.Debug($"Player {player.Name} removed friend {friendCharacterId}.");
            return FriendshipResult.Ok;
        }

        public FriendshipResult SetFriendNote(ulong friendCharacterId, string note)
        {
            if (!friends.TryGetValue(friendCharacterId, out CharacterFriendModel friendModel))
                return FriendshipResult.FriendshipNotFound;

            friendModel.Note = note ?? "";

            player.Session.EnqueueMessageEncrypted(new ServerFriendSetNote
            {
                FriendshipId = friendModel.Id,
                Note         = friendModel.Note
            });

            log.Debug($"Player {player.Name} updated note for friend {friendCharacterId}.");
            return FriendshipResult.Ok;
        }

        public bool IsBlocked(ulong characterId)
        {
            if (!friends.TryGetValue(characterId, out CharacterFriendModel friendModel))
                return false;

            FriendshipType type = (FriendshipType)friendModel.Type;
            return type is FriendshipType.Ignore or FriendshipType.FriendAndRival;
        }

        // ---- Invite responses ----

        public FriendshipResult RespondToInvite(ulong inviteId, FriendshipResponse response)
        {
            if (!incomingInvites.TryGetValue(inviteId, out CharacterFriendInviteModel invite))
            {
                log.Warn($"Player {player.Name} responded to unknown invite {inviteId}.");
                return FriendshipResult.RequestNotFound;
            }

            incomingInvites.Remove(inviteId);
            invitesPendingDelete.Add(invite);
            player.Session.EnqueueMessageEncrypted(new ServerFriendInviteRemove { InviteId = inviteId });

            if (response is FriendshipResponse.Accept or FriendshipResponse.Mutual)
            {
                var myFriendModel = new CharacterFriendModel
                {
                    Id                = (ulong)DateTime.UtcNow.Ticks,
                    CharacterId       = player.CharacterId,
                    FriendCharacterId = invite.SenderId,
                    Type              = (byte)FriendshipType.Friend,
                    Note              = invite.Note
                };
                friends[invite.SenderId] = myFriendModel;
                friendsPendingCreate.Add(invite.SenderId);

                player.Session.EnqueueMessageEncrypted(new ServerFriendAdd
                {
                    Friend = new FriendData
                    {
                        FriendshipId   = myFriendModel.Id,
                        Type           = FriendshipType.Friend,
                        Note           = myFriendModel.Note,
                        PlayerIdentity = new Identity { Id = invite.SenderId, RealmId = RealmContext.Instance.RealmId }
                    }
                });

                var senderFriendModel = new CharacterFriendModel
                {
                    Id                = (ulong)(DateTime.UtcNow.Ticks + 1L),
                    CharacterId       = invite.SenderId,
                    FriendCharacterId = player.CharacterId,
                    Type              = (byte)FriendshipType.Friend,
                    Note              = ""
                };

                IPlayer sender = PlayerManager.Instance.GetPlayer(invite.SenderId);
                if (sender != null)
                    sender.FriendManager.FriendAddedExternally(senderFriendModel);
                else
                    crossFriendsPendingCreate.Add(senderFriendModel);

                log.Debug($"Player {player.Name} accepted friend invite from {invite.SenderId}.");
            }
            else
            {
                log.Debug($"Player {player.Name} declined/ignored invite {inviteId} from {invite.SenderId}.");
            }

            return FriendshipResult.Ok;
        }

        public void MarkInvitesSeen(IEnumerable<ulong> inviteIds)
        {
            foreach (ulong id in inviteIds)
            {
                if (incomingInvites.TryGetValue(id, out CharacterFriendInviteModel invite) && invite.Seen == 0)
                {
                    invite.Seen = 1;
                    incomingInvitesSeen.Add(id);
                }
            }
        }

        // ---- Cross-player notifications ----

        public void ReceiveInvite(CharacterFriendInviteModel invite)
        {
            incomingInvites[invite.Id] = invite;
            SendFriendInviteList();
        }

        public void FriendAddedExternally(CharacterFriendModel friendModel)
        {
            if (friends.ContainsKey(friendModel.FriendCharacterId))
                return;

            friends[friendModel.FriendCharacterId] = friendModel;
            friendsPendingCreate.Add(friendModel.FriendCharacterId);

            player.Session.EnqueueMessageEncrypted(new ServerFriendAdd
            {
                Friend = new FriendData
                {
                    FriendshipId   = friendModel.Id,
                    Type           = FriendshipType.Friend,
                    Note           = friendModel.Note,
                    PlayerIdentity = new Identity { Id = friendModel.FriendCharacterId, RealmId = RealmContext.Instance.RealmId }
                }
            });
        }

        // ---- Initial packets ----

        public void SendInitialPackets()
        {
            SendFriendList();
            SendFriendInviteList();
        }
    }
}
