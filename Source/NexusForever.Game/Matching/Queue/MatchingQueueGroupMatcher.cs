using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Static.Matching;

namespace NexusForever.Game.Matching.Queue
{
    public class MatchingQueueGroupMatcher : IMatchingQueueGroupMatcher
    {
        // Maximum rating spread (max - min) allowed within a single team for rated PvP.
        // Prevents very high-rated and very low-rated players from being placed together.
        private const int MmrTeamSpreadLimit = 150;

        #region Dependency Injection

        private readonly ILogger<MatchingQueueGroupMatcher> log;

        private readonly IMatchingDataManager matchingDataManager;
        private readonly IMatchingRoleEnforcer matchingRoleEnforcer;

        public MatchingQueueGroupMatcher(
            ILogger<MatchingQueueGroupMatcher> log,
            IMatchingDataManager matchingDataManager,
            IMatchingRoleEnforcer matchingRoleEnforcer)
        {
            this.log                  = log;

            this.matchingDataManager  = matchingDataManager;
            this.matchingRoleEnforcer = matchingRoleEnforcer;
        }

        #endregion

        /// <summary>
        /// Attempt to match <see cref="IMatchingQueueProposal"/> against <see cref="IMatchingQueueGroup"/>.
        /// </summary>
        public IMatchingQueueGroupTeam Match(IMatchingQueueGroup matchingQueueGroup, IMatchingQueueProposal matchingQueueProposal)
        {
            log.LogTrace($"Attempting to match matching queue proposal {matchingQueueProposal.Guid} against matching queue group {matchingQueueGroup.Guid}.");

            IEnumerable<IMatchingMap> commonMatchingMaps = matchingQueueGroup.GetMatchingMaps()
                .Intersect(matchingQueueProposal.GetMatchingMaps());

            if (!commonMatchingMaps.Any())
                return null;

            foreach (IMatchingQueueGroupTeam matchingQueueGroupTeam in matchingQueueGroup.GetTeams())
                if (Match(commonMatchingMaps, matchingQueueGroupTeam, matchingQueueProposal))
                    return matchingQueueGroupTeam;

            return null;
        }

        private bool Match(IEnumerable<IMatchingMap> commonMatchingMaps, IMatchingQueueGroupTeam matchingQueueGroupTeam, IMatchingQueueProposal matchingQueueProposal)
        {
            if (matchingDataManager.IsSingleFactionEnforced(matchingQueueProposal.MatchType))
                if (matchingQueueGroupTeam.Faction != matchingQueueProposal.Faction)
                    return false;

            // Ignore-list check: block the match if any pairing between the existing team
            // and the incoming proposal has a mutual block (either direction).
            var teamMembers     = matchingQueueGroupTeam.GetMembers().ToList();
            var proposalMembers = matchingQueueProposal.GetMembers().ToList();
            if (HasMutualIgnore(teamMembers, proposalMembers))
                return false;

            List<IMatchingQueueProposalMember> allMembers = teamMembers
                .Concat(proposalMembers)
                .ToList();

            foreach (IMatchingMap matchingMap in commonMatchingMaps)
            {
                if (Match(matchingMap, allMembers))
                    return true;
            }

            return false;
        }

        private bool Match(IMatchingMap matchingMap, List<IMatchingQueueProposalMember> matchingQueueProposalMembers)
        {
            if (matchingQueueProposalMembers.Count > matchingMap.GameTypeEntry.TeamSize)
                return false;

            if (matchingDataManager.IsCompositionEnforced(matchingMap.GameTypeEntry.MatchTypeEnum))
            {
                IMatchingRoleEnforcerResult result = matchingRoleEnforcer.Check(matchingQueueProposalMembers);
                if (!result.Success)
                    return false;
            }

            // MMR spread check for rated PvP: prevent combining players with very different ratings.
            var matchType = matchingMap.GameTypeEntry.MatchTypeEnum;
            if (matchType == NexusForever.Game.Static.Matching.MatchType.Arena ||
                matchType == NexusForever.Game.Static.Matching.MatchType.Warplot)
            {
                int teamSize = (int)matchingMap.GameTypeEntry.TeamSize;
                int minRating = matchingQueueProposalMembers.Min(m => m.GetPvpRating(matchType, teamSize));
                int maxRating = matchingQueueProposalMembers.Max(m => m.GetPvpRating(matchType, teamSize));
                if (maxRating - minRating > MmrTeamSpreadLimit)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if any member of <paramref name="teamMembers"/> has ignored any member of
        /// <paramref name="proposalMembers"/>, or vice versa.
        /// </summary>
        private static bool HasMutualIgnore(
            IReadOnlyList<IMatchingQueueProposalMember> teamMembers,
            IReadOnlyList<IMatchingQueueProposalMember> proposalMembers)
        {
            foreach (IMatchingQueueProposalMember teamMember in teamMembers)
                foreach (IMatchingQueueProposalMember proposalMember in proposalMembers)
                    if (teamMember.HasIgnored(proposalMember.Identity) ||
                        proposalMember.HasIgnored(teamMember.Identity))
                        return true;

            return false;
        }
    }
}
