using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.Network.Internal;

namespace NexusForever.Game.Guild
{
    public class WarParty : GuildBase, IWarParty
    {
        [Flags]
        private enum WarPartySaveMask
        {
            None   = 0x00,
            Create = 0x01,
            Rating = 0x02,
        }

        public override GuildType Type => GuildType.WarParty;
        public override uint MaxMembers => 30u;

        public int Rating => rating;
        private int rating = 1500;

        public int SeasonWins => seasonWins;
        private int seasonWins;

        public int SeasonLosses => seasonLosses;
        private int seasonLosses;

        private WarPartySaveMask warPartySaveMask;

        #region Dependency Injection

        public WarParty(
            IRealmContext realmContext,
            IInternalMessagePublisher messagePublisher)
            : base(realmContext, messagePublisher)
        {
        }

        #endregion

        /// <summary>
        /// Load an existing <see cref="IWarParty"/> from a database model.
        /// </summary>
        public override void Initialise(GuildModel model)
        {
            if (model.WarPartyData != null)
            {
                rating       = model.WarPartyData.Rating;
                seasonWins   = model.WarPartyData.SeasonWins;
                seasonLosses = model.WarPartyData.SeasonLosses;
            }

            base.Initialise(model);

            warPartySaveMask = WarPartySaveMask.None;
        }

        /// <summary>
        /// Create a new <see cref="IWarParty"/> using supplied parameters.
        /// </summary>
        public override void Initialise(string guildName, string leaderRankName, string councilRankName, string memberRankName)
        {
            base.Initialise(guildName, leaderRankName, councilRankName, memberRankName);
            warPartySaveMask = WarPartySaveMask.Create;
        }

        /// <summary>
        /// Apply a rating delta and record a win or loss for the current warplot season.
        /// </summary>
        public void UpdateRating(int delta, bool won)
        {
            rating       = Math.Max(0, rating + delta);
            seasonWins   += won ? 1 : 0;
            seasonLosses += won ? 0 : 1;
            warPartySaveMask |= WarPartySaveMask.Rating;
        }

        /// <summary>
        /// Reset season wins and losses at the end of a warplot season.
        /// Optionally resets the rating back to the default starting value.
        /// </summary>
        public void ResetSeason(bool resetRating = false)
        {
            seasonWins   = 0;
            seasonLosses = 0;
            if (resetRating)
                rating = 1500;
            warPartySaveMask |= WarPartySaveMask.Rating;
        }

        protected override void Save(CharacterContext context, GuildBaseSaveMask guildSaveMask)
        {
            if (warPartySaveMask == WarPartySaveMask.None)
                return;

            if ((warPartySaveMask & WarPartySaveMask.Create) != 0)
            {
                context.Add(new WarPartyModel
                {
                    Id           = Id,
                    Rating       = rating,
                    SeasonWins   = seasonWins,
                    SeasonLosses = seasonLosses
                });
            }
            else if ((warPartySaveMask & WarPartySaveMask.Rating) != 0)
            {
                var model = new WarPartyModel { Id = Id };
                EntityEntry<WarPartyModel> entity = context.Attach(model);

                model.Rating = rating;
                entity.Property(p => p.Rating).IsModified = true;

                model.SeasonWins = seasonWins;
                entity.Property(p => p.SeasonWins).IsModified = true;

                model.SeasonLosses = seasonLosses;
                entity.Property(p => p.SeasonLosses).IsModified = true;
            }

            warPartySaveMask = WarPartySaveMask.None;
        }
    }
}
