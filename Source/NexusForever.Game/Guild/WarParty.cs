using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.Network.Internal;
using System.Text.Json;

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
            Plugs  = 0x04,
        }

        public override GuildType Type => GuildType.WarParty;
        public override uint MaxMembers => 30u;

        public int Rating => rating;
        private int rating = 1500;

        public int SeasonWins => seasonWins;
        private int seasonWins;

        public int SeasonLosses => seasonLosses;
        private int seasonLosses;

        /// <summary>
        /// Plug slots for warplot defense (slotIndex -> plugItemId).
        /// Used during the build phase before a warplot match starts.
        /// </summary>
        private readonly Dictionary<byte, ushort> plugSlots = new();

        /// <summary>
        /// Boss tokens earned from rated warplot match wins.
        /// Used to unlock boss plugs in the warplot.
        /// </summary>
        public int BossTokens => bossTokens;
        private int bossTokens;

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
                bossTokens   = model.WarPartyData.BossTokens;

                // Deserialize plug slots from JSON
                if (!string.IsNullOrEmpty(model.WarPartyData.PlugSlots))
                {
                    var slots = JsonSerializer.Deserialize<Dictionary<string, ushort>>(model.WarPartyData.PlugSlots);
                    if (slots != null)
                    {
                        foreach (var kvp in slots)
                        {
                            if (byte.TryParse(kvp.Key, out byte slotIndex))
                                plugSlots[slotIndex] = kvp.Value;
                        }
                    }
                }
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

        /// <summary>
        /// Set a plug in a warplot slot.
        /// </summary>
        public void SetPlug(byte slotIndex, ushort plugItemId)
        {
            if (plugItemId == 0)
                plugSlots.Remove(slotIndex);
            else
                plugSlots[slotIndex] = plugItemId;
            warPartySaveMask |= WarPartySaveMask.Plugs;
        }

        /// <summary>
        /// Get the plug item ID for a specific slot.
        /// </summary>
        public ushort GetPlug(byte slotIndex)
        {
            return plugSlots.TryGetValue(slotIndex, out ushort value) ? value : (ushort)0;
        }

        /// <summary>
        /// Get all plug slots for this warparty.
        /// </summary>
        public IReadOnlyDictionary<byte, ushort> GetPlugSlots() => plugSlots;

        /// <summary>
        /// Add boss tokens earned from a rated warplot match win.
        /// </summary>
        public void AddBossToken()
        {
            bossTokens++;
            warPartySaveMask |= WarPartySaveMask.Plugs;
        }

        /// <summary>
        /// Spend boss tokens to unlock a boss plug.
        /// Returns true if tokens were successfully spent.
        /// </summary>
        public bool SpendBossToken()
        {
            if (bossTokens <= 0)
                return false;
            bossTokens--;
            warPartySaveMask |= WarPartySaveMask.Plugs;
            return true;
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
                    SeasonLosses = seasonLosses,
                    BossTokens   = bossTokens,
                    PlugSlots    = SerializePlugSlots()
                });
            }
            else
            {
                var model = new WarPartyModel { Id = Id };
                EntityEntry<WarPartyModel> entity = context.Attach(model);

                if ((warPartySaveMask & WarPartySaveMask.Rating) != 0)
                {
                    model.Rating = rating;
                    entity.Property(p => p.Rating).IsModified = true;

                    model.SeasonWins = seasonWins;
                    entity.Property(p => p.SeasonWins).IsModified = true;

                    model.SeasonLosses = seasonLosses;
                    entity.Property(p => p.SeasonLosses).IsModified = true;
                }

                if ((warPartySaveMask & WarPartySaveMask.Plugs) != 0)
                {
                    model.BossTokens = bossTokens;
                    entity.Property(p => p.BossTokens).IsModified = true;

                    model.PlugSlots = SerializePlugSlots();
                    entity.Property(p => p.PlugSlots).IsModified = true;
                }
            }

            warPartySaveMask = WarPartySaveMask.None;
        }

        /// <summary>
        /// Serialize plug slots to JSON for database storage.
        /// </summary>
        private string SerializePlugSlots()
        {
            var slots = plugSlots.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
            return JsonSerializer.Serialize(slots);
        }
    }
}
