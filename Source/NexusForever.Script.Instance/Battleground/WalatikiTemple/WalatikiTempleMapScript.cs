using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Static.Matching;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Battleground.WalatikiTemple
{
    [ScriptFilterOwnerId(797)]
    public class WalatikiTempleMapScript : EventBasePvpContentMapScript
    {
        public override uint PublicEventId    => 217u;
        public override uint PublicSubEventId => 366u;

        #region Dependency Injection

        private readonly IMatchingDataManager matchingDataManager;
        private readonly IPlayerManager playerManager;

        public WalatikiTempleMapScript(
            IMatchingDataManager matchingDataManager,
            IPlayerManager playerManager)
        {
            this.matchingDataManager = matchingDataManager;
            this.playerManager       = playerManager;
        }

        #endregion

        /// <summary>
        /// Invoked when the <see cref="IPvpMatch"/> for the map finishes.
        /// Calls the base class to handle public event cleanup, then teleports all players
        /// back to their team's entrance position.
        /// </summary>
        public override void OnPvpMatchFinish(MatchWinner matchWinner, MatchEndReason matchEndReason)
        {
            base.OnPvpMatchFinish(matchWinner, matchEndReason);

            foreach (IMatchTeam matchTeam in map.Match.GetTeams())
            {
                var entrance = matchingDataManager.GetMapEntrance(map.Entry.Id, (byte)matchTeam.Team);
                if (entrance == null)
                    continue;

                foreach (IMatchTeamMember matchTeamMember in matchTeam.GetMembers())
                {
                    var player = playerManager.GetPlayer(matchTeamMember.Identity.Id);
                    if (player == null)
                        continue;

                    player.SetControl(null);
                    player.MovementManager.SetPosition(entrance.Position, false);
                    player.MovementManager.SetRotation(entrance.Rotation, false);
                }
            }
        }
    }
}
