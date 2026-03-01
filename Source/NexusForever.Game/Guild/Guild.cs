using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Achievement;
using NexusForever.Game.Static.Guild;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NexusForever.Network.Internal;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.Game.Guild
{
    public partial class Guild : GuildBase, IGuild
    {
        /// <summary>
        /// Determines which fields need saving for <see cref="IGuild"/> when being saved to the database.
        /// </summary>
        [Flags]
        public enum GuildSaveMask
        {
            None            = 0x0000,
            MessageOfTheDay = 0x0001,
            AdditionalInfo  = 0x0002,
            RecruitmentDescription = 0x0004,
            RecruitmentDemand      = 0x0008,
            RecruitmentMinLevel    = 0x0010,
            Classification         = 0x0020,
            BankTabNames           = 0x0040,
            UnlockedPerks          = 0x0080,
            ActivePerks            = 0x0100
        }

        public override GuildType Type => GuildType.Guild;
        public override uint MaxMembers => 40u;

        public IGuildStandard Standard { get; set; }
        public IGuildAchievementManager AchievementManager { get; private set; }

        public string MessageOfTheDay
        {
            get => messageOfTheDay;
            set
            {
                messageOfTheDay = value;
                saveMask |= GuildSaveMask.MessageOfTheDay;
            }
        }
        private string messageOfTheDay;

        public string AdditionalInfo
        {
            get => additionalInfo;
            set
            {
                additionalInfo = value;
                saveMask |= GuildSaveMask.AdditionalInfo;
            }
        }
        private string additionalInfo;

        public string RecruitmentDescription
        {
            get => recruitmentDescription;
            set
            {
                recruitmentDescription = value;
                saveMask |= GuildSaveMask.RecruitmentDescription;
            }
        }
        private string recruitmentDescription;

        public uint RecruitmentDemand
        {
            get => recruitmentDemand;
            set
            {
                recruitmentDemand = value;
                saveMask |= GuildSaveMask.RecruitmentDemand;
            }
        }
        private uint recruitmentDemand;

        public uint RecruitmentMinLevel
        {
            get => recruitmentMinLevel;
            set
            {
                recruitmentMinLevel = value;
                saveMask |= GuildSaveMask.RecruitmentMinLevel;
            }
        }
        private uint recruitmentMinLevel;

        public GuildClassification Classification
        {
            get => classification;
            set
            {
                classification = value;
                saveMask |= GuildSaveMask.Classification;
            }
        }
        private GuildClassification classification;
        private string[] bankTabNames = Enumerable.Repeat(string.Empty, 10).ToArray();
        private HashSet<GuildPerk> unlockedPerks = new();
        private Dictionary<GuildPerk, DateTime> activePerks = new();

        private GuildSaveMask saveMask;

        #region Dependency Injection

        public Guild(
            IRealmContext realmContext,
            IInternalMessagePublisher messagePublisher)
            : base(realmContext, messagePublisher)
        {
        }

        #endregion

        /// <summary>
        /// Create a new <see cref="IGuild"/> from an existing database model.
        /// </summary>
        public override void Initialise(GuildModel model)
        {
            Standard           = new GuildStandard(model.GuildData);
            AchievementManager = new GuildAchievementManager(this, model);
            messageOfTheDay    = model.GuildData.MessageOfTheDay;
            additionalInfo     = model.GuildData.AdditionalInfo;
            recruitmentDescription = model.GuildData.RecruitmentDescription ?? string.Empty;
            recruitmentDemand      = model.GuildData.RecruitmentDemand;
            recruitmentMinLevel    = model.GuildData.RecruitmentMinLevel == 0u ? 1u : model.GuildData.RecruitmentMinLevel;
            classification         = (GuildClassification)model.GuildData.Classification;
            bankTabNames           = ParseBankTabNames(model.GuildData.BankTabNamesJson);
            unlockedPerks          = ParseUnlockedPerks(model.GuildData.UnlockedPerksJson);
            activePerks            = ParseActivePerks(model.GuildData.ActivePerksJson);

            base.Initialise(model);
        }

        /// <summary>
        /// Create a new <see cref="IGuild"/> using the supplied parameters.
        /// </summary>
        public void Initialise(string name, string leaderRankName, string councilRankName, string memberRankName, IGuildStandard standard)
        {
            Standard           = standard;
            AchievementManager = new GuildAchievementManager(this);
            messageOfTheDay    = "";
            additionalInfo     = "";
            recruitmentDescription = "";
            recruitmentDemand      = 0u;
            recruitmentMinLevel    = 1u;
            classification         = GuildClassification.Leveling;
            bankTabNames           = Enumerable.Repeat(string.Empty, 10).ToArray();
            unlockedPerks          = new HashSet<GuildPerk>();
            activePerks            = new Dictionary<GuildPerk, DateTime>();

            Initialise(name, leaderRankName, councilRankName, memberRankName);
        }

        protected override void Save(CharacterContext context, GuildBaseSaveMask baseSaveMask)
        {
            if ((baseSaveMask & GuildBaseSaveMask.Create) != 0)
            {
                context.Add(new GuildDataModel
                {
                    Id                   = Id,
                    AdditionalInfo       = AdditionalInfo,
                    MessageOfTheDay      = MessageOfTheDay,
                    RecruitmentDescription = RecruitmentDescription,
                    RecruitmentDemand      = RecruitmentDemand,
                    RecruitmentMinLevel    = RecruitmentMinLevel,
                    Classification         = (uint)Classification,
                    BankTabNamesJson       = SerialiseBankTabNames(),
                    UnlockedPerksJson      = SerialiseUnlockedPerks(),
                    ActivePerksJson        = SerialiseActivePerks(),
                    BackgroundIconPartId = (ushort)Standard.BackgroundIcon.GuildStandardPartEntry.Id,
                    ForegroundIconPartId = (ushort)Standard.ForegroundIcon.GuildStandardPartEntry.Id,
                    ScanLinesPartId      = (ushort)Standard.ScanLines.GuildStandardPartEntry.Id
                });
            }

            if (saveMask != GuildSaveMask.None)
            {
                var model = new GuildDataModel
                {
                    Id = Id
                };

                EntityEntry<GuildDataModel> entity = context.Attach(model);
                if ((saveMask & GuildSaveMask.MessageOfTheDay) != 0)
                {
                    model.MessageOfTheDay = MessageOfTheDay;
                    entity.Property(p => p.MessageOfTheDay).IsModified = true;
                }

                if ((saveMask & GuildSaveMask.AdditionalInfo) != 0)
                {
                    model.AdditionalInfo = AdditionalInfo;
                    entity.Property(p => p.AdditionalInfo).IsModified = true;
                }

                if ((saveMask & GuildSaveMask.RecruitmentDescription) != 0)
                {
                    model.RecruitmentDescription = RecruitmentDescription;
                    entity.Property(p => p.RecruitmentDescription).IsModified = true;
                }

                if ((saveMask & GuildSaveMask.RecruitmentDemand) != 0)
                {
                    model.RecruitmentDemand = RecruitmentDemand;
                    entity.Property(p => p.RecruitmentDemand).IsModified = true;
                }

                if ((saveMask & GuildSaveMask.RecruitmentMinLevel) != 0)
                {
                    model.RecruitmentMinLevel = RecruitmentMinLevel;
                    entity.Property(p => p.RecruitmentMinLevel).IsModified = true;
                }

                if ((saveMask & GuildSaveMask.Classification) != 0)
                {
                    model.Classification = (uint)Classification;
                    entity.Property(p => p.Classification).IsModified = true;
                }

                if ((saveMask & GuildSaveMask.BankTabNames) != 0)
                {
                    model.BankTabNamesJson = SerialiseBankTabNames();
                    entity.Property(p => p.BankTabNamesJson).IsModified = true;
                }

                if ((saveMask & GuildSaveMask.UnlockedPerks) != 0)
                {
                    model.UnlockedPerksJson = SerialiseUnlockedPerks();
                    entity.Property(p => p.UnlockedPerksJson).IsModified = true;
                }

                if ((saveMask & GuildSaveMask.ActivePerks) != 0)
                {
                    model.ActivePerksJson = SerialiseActivePerks();
                    entity.Property(p => p.ActivePerksJson).IsModified = true;
                }

                saveMask = GuildSaveMask.None;
            }

            AchievementManager.Save(context);
        }

        public override GuildData Build()
        {
            var guildData = new GuildData
            {
                GuildId           = Id,
                GuildName         = Name,
                Flags             = Flags,
                Type              = Type,
                Ranks             = GetGuildRanksPackets().ToList(),
                GuildStandard     = Standard.Build(),
                MemberCount       = (uint)members.Count,
                OnlineMemberCount = (uint)onlineMembers.Count,
                BankTabCount      = (uint)bankTabNames.Count(n => !string.IsNullOrWhiteSpace(n)),
                BankTabNames      = bankTabNames.ToList(),
                GuildInfo =
                {
                    MessageOfTheDay         = MessageOfTheDay,
                    GuildInfo               = AdditionalInfo,
                    GuildCreationDateInDays = (float)DateTime.Now.Subtract(CreateTime).TotalDays * -1f
                }
            };

            foreach (GuildPerk perk in unlockedPerks)
            {
                int bit = (int)perk - 1;
                if (bit >= 0)
                    guildData.UnlockedPerks.SetBit((uint)bit, true);
            }

            foreach ((GuildPerk perk, DateTime activatedAt) in activePerks)
            {
                guildData.ActivePerks.Add(new GuildData.ActivePerk
                {
                    Perk    = perk,
                    EndTime = (float)DateTime.UtcNow.Subtract(activatedAt).TotalDays * -1f
                });
            }

            return guildData;
        }

        public bool RenameBankTab(byte index, string name)
        {
            if (index >= bankTabNames.Length)
                return false;

            bankTabNames[index] = name ?? string.Empty;
            saveMask |= GuildSaveMask.BankTabNames;
            return true;
        }

        public bool IsPerkUnlocked(GuildPerk perk)
        {
            return unlockedPerks.Contains(perk);
        }

        public bool IsPerkActive(GuildPerk perk)
        {
            return activePerks.ContainsKey(perk);
        }

        public string[] GetBankTabNames()
        {
            return bankTabNames.ToArray();
        }

        public bool UnlockPerk(GuildPerk perk)
        {
            if (!unlockedPerks.Add(perk))
                return false;

            saveMask |= GuildSaveMask.UnlockedPerks;
            return true;
        }

        public bool ActivatePerk(GuildPerk perk)
        {
            if (!unlockedPerks.Contains(perk))
                return false;

            activePerks[perk] = DateTime.UtcNow;
            saveMask |= GuildSaveMask.ActivePerks;
            return true;
        }

        private string[] ParseBankTabNames(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return Enumerable.Repeat(string.Empty, 10).ToArray();

            try
            {
                string[] parsed = JsonSerializer.Deserialize<string[]>(raw);
                if (parsed == null || parsed.Length == 0)
                    return Enumerable.Repeat(string.Empty, 10).ToArray();

                if (parsed.Length < 10)
                    return parsed.Concat(Enumerable.Repeat(string.Empty, 10 - parsed.Length)).Take(10).ToArray();

                return parsed.Take(10).ToArray();
            }
            catch
            {
                return Enumerable.Repeat(string.Empty, 10).ToArray();
            }
        }

        private HashSet<GuildPerk> ParseUnlockedPerks(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new HashSet<GuildPerk>();

            try
            {
                uint[] parsed = JsonSerializer.Deserialize<uint[]>(raw);
                if (parsed == null)
                    return new HashSet<GuildPerk>();

                return parsed
                    .Where(v => Enum.IsDefined(typeof(GuildPerk), (int)v))
                    .Select(v => (GuildPerk)v)
                    .ToHashSet();
            }
            catch
            {
                return new HashSet<GuildPerk>();
            }
        }

        private Dictionary<GuildPerk, DateTime> ParseActivePerks(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new Dictionary<GuildPerk, DateTime>();

            try
            {
                Dictionary<uint, DateTime> parsed = JsonSerializer.Deserialize<Dictionary<uint, DateTime>>(raw);
                if (parsed == null)
                    return new Dictionary<GuildPerk, DateTime>();

                return parsed
                    .Where(kvp => Enum.IsDefined(typeof(GuildPerk), (int)kvp.Key))
                    .ToDictionary(kvp => (GuildPerk)kvp.Key, kvp => kvp.Value);
            }
            catch
            {
                return new Dictionary<GuildPerk, DateTime>();
            }
        }

        private string SerialiseBankTabNames()
        {
            return JsonSerializer.Serialize(bankTabNames);
        }

        private string SerialiseUnlockedPerks()
        {
            uint[] perkIds = unlockedPerks.Select(p => (uint)p).OrderBy(v => v).ToArray();
            return JsonSerializer.Serialize(perkIds);
        }

        private string SerialiseActivePerks()
        {
            var payload = activePerks.ToDictionary(kvp => (uint)kvp.Key, kvp => kvp.Value);
            return JsonSerializer.Serialize(payload);
        }

        /// <summary>
        /// Set if taxes are enabled for <see cref="IGuild"/>.
        /// </summary>
        public void SetTaxes(bool enabled)
        {
            if (enabled)
                SetFlag(GuildFlag.Taxes);
            else
                RemoveFlag(GuildFlag.Taxes);

            SendGuildFlagUpdate();
        }
    }
}
