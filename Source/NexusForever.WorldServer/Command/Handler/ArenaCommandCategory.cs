using System;
using System.Collections.Generic;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Entity;
using NexusForever.Game.Guild;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Static.RBAC;
using NexusForever.WorldServer.Command.Context;
using NexusForever.WorldServer.Command.Static;

namespace NexusForever.WorldServer.Command.Handler
{
    [Command(Permission.Arena, "A collection of commands to manage arenas.", "arena")]
    public class ArenaCommandCategory : CommandCategory
    {
        /// <summary>
        /// Minimum combined games (wins + losses) a team must have played to receive season rewards.
        /// </summary>
        private const int MinGamesPlayed = 10;

        /// <summary>
        /// Glory currency rewards per rating bracket.
        /// </summary>
        private static readonly (int MinRating, ulong GloryReward)[] Brackets =
        {
            (1800, 2000uL),
            (1700, 1500uL),
            (1600, 1000uL),
            (1500,  500uL),
        };

        [Command(Permission.ArenaSeasonEnd, "End the current arena season: deliver Glory rewards to qualifying teams and reset season stats.", "seasonend")]
        public void HandleArenaSeasonEnd(ICommandContext context,
            [Parameter("If true, also reset ratings to 1500.", ParameterFlags.Optional)]
            bool resetRating = false)
        {
            int teamsProcessed  = 0;
            int playersRewarded = 0;
            int offlineRewarded = 0;

            var offlineMails = new List<CharacterMailModel>();

            foreach (IArenaTeam team in GlobalGuildManager.Instance.GetArenaTeams())
            {
                int gamesPlayed = team.SeasonWins + team.SeasonLosses;
                ulong gloryReward = 0;

                if (gamesPlayed >= MinGamesPlayed)
                {
                    foreach ((int minRating, ulong reward) in Brackets)
                    {
                        if (team.Rating >= minRating)
                        {
                            gloryReward = reward;
                            break;
                        }
                    }
                }

                if (gloryReward > 0)
                {
                    foreach (IGuildMember member in team)
                    {
                        IPlayer onlinePlayer = PlayerManager.Instance.GetPlayer(member.CharacterId);
                        if (onlinePlayer != null)
                        {
                            onlinePlayer.CurrencyManager.CurrencyAddAmount(CurrencyType.Glory, gloryReward);
                            playersRewarded++;
                        }
                        else
                        {
                            // Mail Glory to offline players
                            ulong mailId = AssetManager.Instance.NextMailId;
                            offlineMails.Add(new CharacterMailModel
                            {
                                Id             = mailId,
                                RecipientId    = member.CharacterId,
                                SenderType     = (byte)SenderType.GM,
                                Subject        = "Arena Season Reward",
                                Message        = $"Your arena team earned {gloryReward} Glory this season (Rating: {team.Rating}, Wins: {team.SeasonWins}, Losses: {team.SeasonLosses}). Well fought!",
                                CurrencyType   = (byte)CurrencyType.Glory,
                                CurrencyAmount = gloryReward,
                                DeliveryTime   = (byte)DeliverySpeed.Instant,
                                CreateTime     = DateTime.UtcNow
                            });
                            offlineRewarded++;
                        }
                    }
                }

                team.ResetSeason(resetRating);
                teamsProcessed++;
            }

            // Persist offline mails to the database.
            if (offlineMails.Count > 0)
            {
                DatabaseManager.Instance.GetDatabase<CharacterDatabase>().Save(ctx =>
                {
                    foreach (CharacterMailModel mail in offlineMails)
                        ctx.CharacterMail.Add(mail);
                }).GetAwaiter().GetResult();
            }

            context.SendMessage($"Arena season ended. Teams processed: {teamsProcessed}, online players rewarded: {playersRewarded}, offline mails sent: {offlineRewarded}. Reset rating: {resetRating}.");
        }
    }
}
