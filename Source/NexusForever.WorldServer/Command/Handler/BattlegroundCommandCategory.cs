using System.Linq;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Matching;
using NexusForever.Game.Static.Matching;
using NexusForever.Game.Static.RBAC;
using NexusForever.WorldServer.Command.Context;
using NexusForever.WorldServer.Command.Static;

namespace NexusForever.WorldServer.Command.Handler
{
    [Command(Permission.Battleground, "A collection of commands to manage battlegrounds.", "bg")]
    public class BattlegroundCommandCategory : CommandCategory
    {
        [Command(Permission.Battleground, "Print Battleground MatchingGameMap entries and their WorldIds.", "mapinfo")]
        public void HandleBattlegroundMapInfo(ICommandContext context)
        {
            var bgMaps = MatchingDataManager.Instance.GetMatchingMaps(MatchType.BattleGround).ToList();
            if (bgMaps.Count == 0)
            {
                context.SendMessage("No Battleground MatchingGameMap entries found in the game table.");
                return;
            }

            foreach (IMatchingMap map in bgMaps)
            {
                bool hasEntrance0 = MatchingDataManager.Instance.GetMapEntrance(map.GameMapEntry.WorldId, 0) != null;
                bool hasEntrance1 = MatchingDataManager.Instance.GetMapEntrance(map.GameMapEntry.WorldId, 1) != null;
                context.SendMessage(
                    $"MapId={map.GameMapEntry.Id} WorldId={map.GameMapEntry.WorldId} " +
                    $"TypeId={map.GameMapEntry.MatchingGameTypeId} " +
                    $"T0Entrance={(hasEntrance0 ? "OK" : "MISSING")} T1Entrance={(hasEntrance1 ? "OK" : "MISSING")}");
            }
        }
    }
}
