 namespace NexusForever.Game.Abstract.Guild
{
    public interface IWarParty : IGuildBase
    {
        int Rating { get; }
        int SeasonWins { get; }
        int SeasonLosses { get; }
        int BossTokens { get; }

        /// <summary>
        /// Apply a rating delta and record a win or loss for the current warplot season.
        /// </summary>
        void UpdateRating(int delta, bool won);

        /// <summary>
        /// Reset season wins and losses at the end of a warplot season.
        /// Optionally resets the rating back to the starting value.
        /// </summary>
        void ResetSeason(bool resetRating = false);

        /// <summary>
        /// Set a plug item in a warplot slot.
        /// </summary>
        void SetPlug(byte slotIndex, ushort plugItemId);

        /// <summary>
        /// Return the plug item id currently assigned to the supplied slot.
        /// </summary>
        ushort GetPlug(byte slotIndex);

        /// <summary>
        /// Return all configured warplot plug slots.
        /// </summary>
        IReadOnlyDictionary<byte, ushort> GetPlugSlots();

        /// <summary>
        /// Add one boss token.
        /// </summary>
        void AddBossToken();

        /// <summary>
        /// Spend one boss token.
        /// </summary>
        bool SpendBossToken();
    }
}
