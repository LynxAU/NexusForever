namespace NexusForever.Game.Abstract.Guild
{
    public interface IWarParty : IGuildBase
    {
        int Rating { get; }
        int SeasonWins { get; }
        int SeasonLosses { get; }

        /// <summary>
        /// Apply a rating delta and record a win or loss for the current warplot season.
        /// </summary>
        void UpdateRating(int delta, bool won);
    }
}
