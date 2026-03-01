using NexusForever.Game.Static.Guild;

namespace NexusForever.Game.Abstract.Guild
{
    public interface IArenaTeam : IGuildBase
    {
        int Rating { get; }
        int SeasonWins { get; }
        int SeasonLosses { get; }

        /// <summary>
        /// Create a new <see cref="IArenaTeam"/> using supplied parameters.
        /// </summary>
        void Initialise(GuildType type, string guildName, string leaderRankName, string councilRankName, string memberRankName);

        /// <summary>
        /// Apply a rating delta and record a win or loss for the current season.
        /// </summary>
        void UpdateRating(int delta, bool won);
    }
}
