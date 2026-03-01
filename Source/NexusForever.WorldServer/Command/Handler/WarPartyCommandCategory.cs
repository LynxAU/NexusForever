using System;
using System.Collections.Generic;
using System.Linq;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Entity;
using NexusForever.Game.Guild;
using NexusForever.Game.Matching;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Static.Matching;
using NexusForever.Game.Static.RBAC;
using NexusForever.GameTable;
using NexusForever.WorldServer.Command.Context;
using NexusForever.WorldServer.Command.Static;

namespace NexusForever.WorldServer.Command.Handler
{
    [Command(Permission.WarParty, "A collection of commands to manage war parties.", "warparty")]
    public class WarPartyCommandCategory : CommandCategory
    {
        /// <summary>
        /// Minimum combined games (wins + losses) a war party must have played to receive season rewards.
        /// </summary>
        private const int MinGamesPlayed = 10;

        /// <summary>
        /// Reward amounts (WarCoins) by rank, paired to rating floors from PvPRatingFloor.tbl (Warplot).
        /// Highest floor -> highest reward. Hardcoded amounts; floors are data-driven.
        /// </summary>
        private static readonly ulong[] RewardsByRank = { 2000uL, 1500uL, 1000uL, 500uL };

        private static (int MinRating, ulong WarCoinReward)[] GetWarplotRewardBrackets()
        {
            return GameTableManager.Instance.PvPRatingFloor.Entries
                .Where(e => e != null && e.PvpRatingTypeEnum == (uint)MatchingGameRatingType.Warplot && e.FloorValue > 0)
                .OrderByDescending(e => e.FloorValue)
                .Take(RewardsByRank.Length)
                .Select((e, i) => ((int)e.FloorValue, RewardsByRank[i]))
                .ToArray();
        }

        [Command(Permission.WarPartySeasonEnd, "End the current warplot season: deliver WarCoin rewards to qualifying war parties and reset season stats.", "seasonend")]
        public void HandleWarPartySeasonEnd(ICommandContext context,
            [Parameter("If true, also reset ratings to 1500.", ParameterFlags.Optional)]
            bool resetRating = false)
        {
            int partiesProcessed = 0;
            int playersRewarded  = 0;
            int offlineRewarded  = 0;

            var offlineMails = new List<CharacterMailModel>();
            var brackets     = GetWarplotRewardBrackets();

            foreach (IWarParty party in GlobalGuildManager.Instance.GetWarParties())
            {
                int gamesPlayed   = party.SeasonWins + party.SeasonLosses;
                ulong warCoinReward = 0;

                if (gamesPlayed >= MinGamesPlayed)
                {
                    foreach ((int minRating, ulong reward) in brackets)
                    {
                        if (party.Rating >= minRating)
                        {
                            warCoinReward = reward;
                            break;
                        }
                    }
                }

                if (warCoinReward > 0)
                {
                    foreach (IGuildMember member in party)
                    {
                        IPlayer onlinePlayer = PlayerManager.Instance.GetPlayer(member.CharacterId);
                        if (onlinePlayer != null)
                        {
                            onlinePlayer.CurrencyManager.CurrencyAddAmount(CurrencyType.WarCoin, warCoinReward);
                            playersRewarded++;
                        }
                        else
                        {
                            ulong mailId = AssetManager.Instance.NextMailId;
                            offlineMails.Add(new CharacterMailModel
                            {
                                Id             = mailId,
                                RecipientId    = member.CharacterId,
                                SenderType     = (byte)SenderType.GM,
                                Subject        = "Warplot Season Reward",
                                Message        = $"Your war party earned {warCoinReward} WarCoins this season (Rating: {party.Rating}, Wins: {party.SeasonWins}, Losses: {party.SeasonLosses}). Well fought!",
                                CurrencyType   = (byte)CurrencyType.WarCoin,
                                CurrencyAmount = warCoinReward,
                                DeliveryTime   = (byte)DeliverySpeed.Instant,
                                CreateTime     = DateTime.UtcNow
                            });
                            offlineRewarded++;
                        }
                    }
                }

                party.ResetSeason(resetRating);
                partiesProcessed++;
            }

            if (offlineMails.Count > 0)
            {
                DatabaseManager.Instance.GetDatabase<CharacterDatabase>().Save(ctx =>
                {
                    foreach (CharacterMailModel mail in offlineMails)
                        ctx.CharacterMail.Add(mail);
                }).GetAwaiter().GetResult();
            }

            context.SendMessage($"Warplot season ended. War parties processed: {partiesProcessed}, online players rewarded: {playersRewarded}, offline mails sent: {offlineRewarded}. Reset rating: {resetRating}.");
        }

        [Command(Permission.WarParty, "Print Warplot MatchingGameMap entries and their WorldIds (for WarplotScript setup).", "mapinfo")]
        public void HandleWarPartyMapInfo(ICommandContext context)
        {
            List<IMatchingMap> warplotMaps = MatchingDataManager.Instance.GetMatchingMaps(MatchType.Warplot).ToList();
            if (warplotMaps.Count == 0)
            {
                context.SendMessage("No Warplot MatchingGameMap entries found in the game table.");
                return;
            }

            foreach (IMatchingMap map in warplotMaps)
                context.SendMessage($"MatchingGameMap Id={map.GameMapEntry.Id} WorldId={map.GameMapEntry.WorldId} RecommendedItemLevel={map.GameMapEntry.RecommendedItemLevel}");
        }
    }
}
