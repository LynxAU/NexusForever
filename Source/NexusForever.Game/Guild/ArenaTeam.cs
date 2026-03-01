using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.Network.Internal;

namespace NexusForever.Game.Guild
{
    public class ArenaTeam : GuildBase, IArenaTeam
    {
        [Flags]
        private enum ArenaTeamSaveMask
        {
            None   = 0x00,
            Create = 0x01,
            Rating = 0x02,
        }

        public override GuildType Type => type;
        private GuildType type;

        public int Rating => rating;
        private int rating = 1500;

        public int SeasonWins => seasonWins;
        private int seasonWins;

        public int SeasonLosses => seasonLosses;
        private int seasonLosses;

        private ArenaTeamSaveMask arenaTeamSaveMask;

        public override uint MaxMembers
        {
            get => Type switch
            {
                GuildType.ArenaTeam2v2 => 2u,
                GuildType.ArenaTeam3v3 => 3u,
                GuildType.ArenaTeam5v5 => 5u,
                _ => throw new InvalidOperationException()
            };
        }

        #region Dependency Injection

        public ArenaTeam(
            IRealmContext realmContext,
            IInternalMessagePublisher messagePublisher)
            : base(realmContext, messagePublisher)
        {
        }

        #endregion

        /// <summary>
        /// Load an existing <see cref="IArenaTeam"/> from a database model.
        /// </summary>
        public override void Initialise(GuildModel model)
        {
            type = (GuildType)model.Type;

            if (model.ArenaTeamData != null)
            {
                rating       = model.ArenaTeamData.Rating;
                seasonWins   = model.ArenaTeamData.SeasonWins;
                seasonLosses = model.ArenaTeamData.SeasonLosses;
            }

            base.Initialise(model);

            arenaTeamSaveMask = ArenaTeamSaveMask.None;
        }

        /// <summary>
        /// Create a new <see cref="IArenaTeam"/> using supplied parameters.
        /// </summary>
        public void Initialise(GuildType type, string guildName, string leaderRankName, string councilRankName, string memberRankName)
        {
            this.type = type;

            Initialise(guildName, leaderRankName, councilRankName, memberRankName);

            arenaTeamSaveMask = ArenaTeamSaveMask.Create;
        }

        /// <summary>
        /// Apply a rating delta and record a win or loss for the current season.
        /// </summary>
        public void UpdateRating(int delta, bool won)
        {
            rating       = Math.Max(0, rating + delta);
            seasonWins   += won ? 1 : 0;
            seasonLosses += won ? 0 : 1;
            arenaTeamSaveMask |= ArenaTeamSaveMask.Rating;
        }

        protected override void Save(CharacterContext context, GuildBaseSaveMask guildSaveMask)
        {
            if (arenaTeamSaveMask == ArenaTeamSaveMask.None)
                return;

            if ((arenaTeamSaveMask & ArenaTeamSaveMask.Create) != 0)
            {
                context.Add(new ArenaTeamModel
                {
                    Id           = Id,
                    Rating       = rating,
                    SeasonWins   = seasonWins,
                    SeasonLosses = seasonLosses
                });
            }
            else if ((arenaTeamSaveMask & ArenaTeamSaveMask.Rating) != 0)
            {
                var model = new ArenaTeamModel { Id = Id };
                EntityEntry<ArenaTeamModel> entity = context.Attach(model);

                model.Rating = rating;
                entity.Property(p => p.Rating).IsModified = true;

                model.SeasonWins = seasonWins;
                entity.Property(p => p.SeasonWins).IsModified = true;

                model.SeasonLosses = seasonLosses;
                entity.Property(p => p.SeasonLosses).IsModified = true;
            }

            arenaTeamSaveMask = ArenaTeamSaveMask.None;
        }
    }
}
