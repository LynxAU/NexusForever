using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;

namespace NexusForever.Game.Matching.Queue
{
    public class MatchingQueueProposalMember : IMatchingQueueProposalMember
    {
        public IMatchingQueueProposal MatchingQueueProposal { get; private set; }

        public Identity Identity { get; private set; }
        public Role Roles { get; private set; }

        #region Dependency Injection

        private readonly IPlayerManager playerManager;

        public MatchingQueueProposalMember(
            IPlayerManager playerManager)
        {
            this.playerManager = playerManager;
        }

        #endregion

        /// <summary>
        /// Initialise <see cref="IMatchingQueueProposalMember"/>.
        /// </summary>
        public void Initialise(IMatchingQueueProposal matchingQueueProposal, Identity identity, Role roles)
        {
            if (MatchingQueueProposal != null)
                throw new InvalidOperationException();

            MatchingQueueProposal = matchingQueueProposal;
            Identity              = identity;
            Roles                 = roles;
        }

        /// <summary>
        /// Send <see cref="IWritable"/> to member.
        /// </summary>
        public void Send(IWritable message)
        {
            IPlayer player = playerManager.GetPlayer(Identity);
            player?.Session.EnqueueMessageEncrypted(message);
        }

        /// <summary>
        /// Return the player's current PvP rating for the given match context.
        /// Returns 1500 (default ELO baseline) when the player has no relevant team.
        /// </summary>
        public int GetPvpRating(NexusForever.Game.Static.Matching.MatchType matchType, int teamSize)
        {
            IPlayer player = playerManager.GetPlayer(Identity);
            if (player == null)
                return 1500;

            if (matchType == NexusForever.Game.Static.Matching.MatchType.Warplot)
            {
                var warParty = player.GuildManager.GetGuild<IWarParty>(GuildType.WarParty);
                return warParty?.Rating ?? 1500;
            }

            if (matchType == NexusForever.Game.Static.Matching.MatchType.Arena)
            {
                GuildType arenaType = teamSize switch
                {
                    2 => GuildType.ArenaTeam2v2,
                    3 => GuildType.ArenaTeam3v3,
                    _ => GuildType.ArenaTeam5v5
                };
                var arenaTeam = player.GuildManager.GetGuild<IArenaTeam>(arenaType);
                return arenaTeam?.Rating ?? 1500;
            }

            return 1500;
        }

        /// <summary>
        /// Return true if this member has the specified identity on their ignore/block list.
        /// </summary>
        public bool HasIgnored(Identity other)
        {
            IPlayer player = playerManager.GetPlayer(Identity);
            return player?.FriendManager.IsBlocked(other.Id) ?? false;
        }
    }
}
