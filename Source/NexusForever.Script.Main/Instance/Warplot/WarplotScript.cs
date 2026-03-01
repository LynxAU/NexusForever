using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.Matching;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Warplot
{
    /// <summary>
    /// Warplot PvP map script (WorldId 1610 / Map\WarplotsMap).
    /// </summary>
    [ScriptFilterOwnerId(1610)]
    public class WarplotScript : IContentPvpMapScript, IOwnedScript<IContentPvpMapInstance>
    {
        private const uint WarplotPublicEventId = 253u;

        #region Dependency Injection

        private readonly IMatchingDataManager matchingDataManager;
        private readonly IPlayerManager playerManager;

        public WarplotScript(
            IMatchingDataManager matchingDataManager,
            IPlayerManager playerManager)
        {
            this.matchingDataManager = matchingDataManager;
            this.playerManager       = playerManager;
        }

        #endregion

        private IContentPvpMapInstance map;
        private IPublicEvent warplotEvent;

        public void OnLoad(IContentPvpMapInstance owner)
        {
            map          = owner;
            warplotEvent = map.PublicEventManager.CreateEvent(WarplotPublicEventId);
        }

        public void OnAddToMap(IGridEntity entity)
        {
            if (entity is not IPlayer player || map?.Match == null || warplotEvent == null)
                return;

            IMatchTeam team = map.Match.GetTeam(player.Identity);
            if (team == null)
                return;

            warplotEvent.JoinEvent(player, team.Team == MatchTeam.Red ? PublicEventTeam.RedTeam_2 : PublicEventTeam.BlueTeam_2);
        }

        public void OnPublicEventFinish(IPublicEvent publicEvent, IPublicEventTeam publicEventTeam)
        {
            if (warplotEvent == null || publicEvent != warplotEvent || map?.Match is not IPvpMatch pvpMatch)
                return;

            MatchWinner winner;
            if (publicEventTeam == null)
                winner = MatchWinner.Draw;
            else
                winner = publicEventTeam.Team == PublicEventTeam.RedTeam_2 ? MatchWinner.Red : MatchWinner.Blue;

            pvpMatch.MatchFinish(winner, MatchEndReason.Completed);
        }

        public void OnPvpMatchState(PvpGameState state)
        {
            warplotEvent?.OnPvpMatchState(state);
        }

        public void OnPvpMatchFinish(MatchWinner matchWinner, MatchEndReason matchEndReason)
        {
            if (warplotEvent != null)
            {
                if (matchWinner == MatchWinner.Red)
                    warplotEvent.Finish(PublicEventTeam.RedTeam_2);
                else if (matchWinner == MatchWinner.Blue)
                    warplotEvent.Finish(PublicEventTeam.BlueTeam_2);
                else
                    warplotEvent.Finish(PublicEventTeam.PublicTeam);
            }

            if (map?.Match == null)
                return;

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

        public void OnRemoveFromMap(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            if (player.ControlGuid != player.Guid)
                player.SetControl(player);
        }
    }
}
