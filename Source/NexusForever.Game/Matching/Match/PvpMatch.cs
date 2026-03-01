using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Matching;
using NexusForever.GameTable;
using NexusForever.Network.Internal;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Shared;
using NexusForever.Shared.Game;

namespace NexusForever.Game.Matching.Match
{
    public class PvpMatch : Match, IPvpMatch
    {
        private PvpGameState state;
        private UpdateTimer stateTimer;
        private Action stateCallback;

        // only deathpool stats are tracked in the match, other types are tracked in the public events
        // looks like this is legacy and arenas never got migrated to the new public event system
        private readonly Dictionary<Static.Matching.MatchTeam, uint> deathmatchPool = [];

        #region Dependency Injection

        public PvpMatch(
            ILogger<PvpMatch> log,
            IMatchManager matchManager,
            IMatchingDataManager matchingDataManager,
            IFactory<IMatchTeam> matchTeamFactory,
            IGameTableManager gameTableManager,
            IPlayerManager playerManager,
            IInternalMessagePublisher messagePublisher)
            : base(log, matchManager, matchingDataManager, matchTeamFactory, gameTableManager, playerManager, messagePublisher)
        {
        }

        #endregion

        /// <summary>
        /// Initialise the match with the supplied <see cref="IMatchProposal"/>
        /// </summary>
        public override void Initialise(IMatchProposal matchProposal)
        {
            base.Initialise(matchProposal);

            if (MatchingMap.GameTypeEntry.MatchingRulesEnum == MatchRules.DeathmatchPool)
                foreach (IMatchTeam team in GetTeams())
                    deathmatchPool.Add(team.Team, MatchingMap.GameTypeEntry.MatchingRulesData01);

            SetState(PvpGameState.Initialized);
        }

        protected override void InitialiseTeams(IMatchProposal matchProposal)
        {
            // for PvP matches, the team is randomly selected
            Static.Matching.MatchTeam team = (Static.Matching.MatchTeam)Random.Shared.Next(0, 2);
            foreach (IMatchingQueueGroupTeam matchingQueueGroupTeam in matchProposal.MatchingQueueGroup.GetTeams())
            {
                InitialiseTeam(team, matchingQueueGroupTeam);
                team = team == Static.Matching.MatchTeam.Blue ? Static.Matching.MatchTeam.Red : Static.Matching.MatchTeam.Blue;
            }
        }

        /// <summary>
        /// Invoked each world tick with the delta since the previous tick occurred.
        /// </summary>
        public override void Update(double lastTick)
        {
            if (stateTimer != null)
            {
                stateTimer.Update(lastTick);
                if (stateTimer.HasElapsed && stateCallback != null)
                {
                    stateTimer = null;
                    stateCallback();
                }
            }

            base.Update(lastTick);
        }

        /// <summary>
        /// Set the state of the match to the supplied <see cref="PvpGameState"/>.
        /// </summary>
        public void SetState(PvpGameState state)
        {
            this.state = state;

            switch (state)
            {
                // wait 10 seconds or until all players have entered the match to start preparation, whichever happens first
                case PvpGameState.Initialized:
                    SetNextPhase(10000, () => { SetState(PvpGameState.Preparation); });
                    break;
                case PvpGameState.Preparation:
                    SetNextPhase(MatchingMap.GameTypeEntry.PreparationTimeMS, () => { SetState(PvpGameState.InProgress); });
                    break;
                case PvpGameState.InProgress:
                    SetNextPhase(MatchingMap.GameTypeEntry.MatchTimeMS, () => { MatchFinish(MatchWinner.Draw, MatchEndReason.TimeExpired); });
                    break;
                case PvpGameState.Finished:
                {
                    stateTimer = null;
                    stateCallback = null;
                    break;
                }
            }

            Broadcast(new ServerMatchingMatchPvpStateUpdated
            {
                State = state,
            });

            if (map is IContentPvpMapInstance pvpMapInstance)
                pvpMapInstance.OnPvpMatchState(state);
        }

        private void SetNextPhase(uint time, Action callback)
        {
            if (time == 0)
                return;

            stateTimer    = new UpdateTimer(TimeSpan.FromMilliseconds(time));
            stateCallback = callback;
        }

        /// <summary>
        /// Update deathmatch pool for the team the character is on.
        /// </summary>
        public void UpdatePool(Abstract.Identity identity)
        {
            if (MatchingMap.GameTypeEntry.MatchingRulesEnum != MatchRules.DeathmatchPool)
                return;

            IMatchTeam team = GetTeam(identity);
            if (team == null)
                throw new InvalidOperationException();

            deathmatchPool[team.Team]--;
            SendPoolUpdate();

            if (deathmatchPool[team.Team] == 0)
                MatchFinish(team.Team == Static.Matching.MatchTeam.Red ? MatchWinner.Blue : MatchWinner.Red, MatchEndReason.Completed);
        }

        private void SendPoolUpdate()
        {
            Broadcast(new ServerMatchingMatchPvpPoolUpdated
            {
                LivesRemainingTeam1 = deathmatchPool[Static.Matching.MatchTeam.Red],
                LivesRemainingTeam2 = deathmatchPool[Static.Matching.MatchTeam.Blue]
            });
        }

        /// <summary>
        /// Invoked when <see cref="IPlayer"/> enters the match.
        /// </summary>
        public override void MatchEnter(IPlayer player)
        {
            base.MatchEnter(player);

            IMatchTeam team = GetTeam(player.Identity);
            if (team == null)
                throw new InvalidOperationException();

            player.Session.EnqueueMessageEncrypted(new ServerMatchingMatchPvpStateInitial
            {
                Team  = team.Team,
                State = new StateInfo
                {
                    State       = state,
                    TimeElapsed = (uint)TimeSpan.FromSeconds(stateTimer.Duration - stateTimer.Time).TotalMilliseconds
                }
            });

            if (MatchingMap.GameTypeEntry.MatchingRulesEnum == MatchRules.DeathmatchPool)
                SendPoolUpdate();
        }

        /// <summary>
        /// Finish the match with the supplied <see cref="MatchWinner"/> and <see cref="MatchEndReason"/>
        /// </summary>
        public void MatchFinish(MatchWinner matchWinner, MatchEndReason matchEndReason)
        {
            if (Status != MatchStatus.InProgress)
                return;

            MatchFinish();

            if (MatchingMap.GameTypeEntry.MatchTypeEnum == Static.Matching.MatchType.Arena)
                UpdateArenaRatings(matchWinner);

            SetState(PvpGameState.Finished);

            Broadcast(new ServerMatchingMatchPvpFinished
            {
                Winner = matchWinner,
                Reason = matchEndReason
            });

            if (map is IContentPvpMapInstance pvpMapInstance)
                pvpMapInstance.OnPvpMatchFinish(matchWinner, matchEndReason);
        }

        /// <summary>
        /// Collect arena teams from each match team and apply ELO rating changes.
        /// </summary>
        private void UpdateArenaRatings(MatchWinner matchWinner)
        {
            IList<IMatchTeam> teams = GetTeams().ToList();
            if (teams.Count != 2)
                return;

            // Map MatchTeam colour → arena teams (deduplicated by guild ID)
            var teamArenaMap = new Dictionary<Static.Matching.MatchTeam, List<IArenaTeam>>();
            int teamSize = 0;

            foreach (IMatchTeam matchTeam in teams)
            {
                var arenaTeams = new List<IArenaTeam>();
                var seen = new HashSet<ulong>();

                foreach (IMatchTeamMember member in matchTeam.GetMembers())
                {
                    teamSize = Math.Max(teamSize, matchTeam.GetMembers().Count());
                    IPlayer player = playerManager.GetPlayer(member.Identity);
                    if (player == null)
                        continue;

                    IArenaTeam arenaTeam = ResolveArenaTeam(player, teamSize);
                    if (arenaTeam == null || !seen.Add(arenaTeam.Id))
                        continue;

                    arenaTeams.Add(arenaTeam);
                }

                teamArenaMap[matchTeam.Team] = arenaTeams;
            }

            // Use the first found team's rating on each side for ELO calculation
            int ratingRed  = GetAverageRating(teamArenaMap.GetValueOrDefault(Static.Matching.MatchTeam.Red));
            int ratingBlue = GetAverageRating(teamArenaMap.GetValueOrDefault(Static.Matching.MatchTeam.Blue));

            int deltaRed  = CalculateEloDelta(ratingRed,  ratingBlue, GetScore(matchWinner, Static.Matching.MatchTeam.Red));
            int deltaBlue = CalculateEloDelta(ratingBlue, ratingRed,  GetScore(matchWinner, Static.Matching.MatchTeam.Blue));

            ApplyRatingDeltas(teamArenaMap.GetValueOrDefault(Static.Matching.MatchTeam.Red),  deltaRed,  matchWinner == MatchWinner.Red);
            ApplyRatingDeltas(teamArenaMap.GetValueOrDefault(Static.Matching.MatchTeam.Blue), deltaBlue, matchWinner == MatchWinner.Blue);
        }

        private IArenaTeam ResolveArenaTeam(IPlayer player, int teamSize)
        {
            GuildType arenaType = teamSize switch
            {
                2 => GuildType.ArenaTeam2v2,
                3 => GuildType.ArenaTeam3v3,
                _ => GuildType.ArenaTeam5v5
            };
            return player.GuildManager.GetGuild<IArenaTeam>(arenaType);
        }

        private static int GetAverageRating(IList<IArenaTeam> teams)
        {
            if (teams == null || teams.Count == 0)
                return 1500;
            return (int)teams.Average(t => t.Rating);
        }

        private static float GetScore(MatchWinner winner, Static.Matching.MatchTeam team)
        {
            if (winner == MatchWinner.Draw)
                return 0.5f;
            return (winner == MatchWinner.Red && team == Static.Matching.MatchTeam.Red)
                || (winner == MatchWinner.Blue && team == Static.Matching.MatchTeam.Blue)
                ? 1.0f : 0.0f;
        }

        private static int CalculateEloDelta(int myRating, int opponentRating, float actualScore)
        {
            const int kFactor = 32;
            double expected = 1.0 / (1.0 + Math.Pow(10.0, (opponentRating - myRating) / 400.0));
            return (int)Math.Round(kFactor * (actualScore - expected));
        }

        private static void ApplyRatingDeltas(IList<IArenaTeam> teams, int delta, bool won)
        {
            if (teams == null)
                return;
            foreach (IArenaTeam team in teams)
                team.UpdateRating(delta, won);
        }
    }
}
