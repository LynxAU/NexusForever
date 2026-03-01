using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;

namespace NexusForever.Game.Abstract.Matching.Queue
{
    public interface IMatchingQueueProposalMember
    {
        IMatchingQueueProposal MatchingQueueProposal { get; }

        Identity Identity { get; }
        Role Roles { get; }

        /// <summary>
        /// Initialise <see cref="IMatchingQueueProposalMember"/>.
        /// </summary>
        void Initialise(IMatchingQueueProposal matchingQueueProposal, Identity identity, Role roles);

        /// <summary>
        /// Send <see cref="IWritable"/> to member.
        /// </summary>
        void Send(IWritable message);

        /// <summary>
        /// Return the player's current PvP rating for the given match context.
        /// Returns 1500 (default ELO baseline) when the player has no relevant team.
        /// </summary>
        int GetPvpRating(NexusForever.Game.Static.Matching.MatchType matchType, int teamSize);

        /// <summary>
        /// Return true if this member has the specified identity on their ignore/block list.
        /// </summary>
        bool HasIgnored(Identity other);
    }
}
