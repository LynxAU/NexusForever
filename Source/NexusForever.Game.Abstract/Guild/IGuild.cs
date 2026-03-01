using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Static.Guild;

namespace NexusForever.Game.Abstract.Guild
{
    public interface IGuild : IGuildBase
    {
        IGuildStandard Standard { get; }
        IGuildAchievementManager AchievementManager { get; }
        string MessageOfTheDay { get; set; }
        string AdditionalInfo { get; set; }
        string RecruitmentDescription { get; set; }
        uint RecruitmentDemand { get; set; }
        uint RecruitmentMinLevel { get; set; }
        GuildClassification Classification { get; set; }

        /// <summary>
        /// Create a new <see cref="IGuild"/> using the supplied parameters.
        /// </summary>
        void Initialise(string name, string leaderRankName, string councilRankName, string memberRankName, IGuildStandard standard);

        /// <summary>
        /// Set if taxes are enabled for <see cref="IGuild"/>.
        /// </summary>
        void SetTaxes(bool enabled);
    }
}
