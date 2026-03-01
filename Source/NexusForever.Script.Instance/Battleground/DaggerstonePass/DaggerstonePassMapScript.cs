using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Static.Matching;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Battleground.DaggerstonePass
{
    [ScriptFilterOwnerId(2166)]
    public class DaggerstonePassMapScript : EventBasePvpContentMapScript
    {
        public override uint PublicEventId    => 438u;
        public override uint PublicSubEventId => 466u;

        #region Dependency Injection

        private readonly IMatchingDataManager matchingDataManager;
        private readonly IPlayerManager playerManager;

        public DaggerstonePassMapScript(
            IMatchingDataManager matchingDataManager,
            IPlayerManager playerManager)
        {
            this.matchingDataManager = matchingDataManager;
            this.playerManager       = playerManager;
        }

        #endregion

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
