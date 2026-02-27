using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Abstract.Trade
{
    public interface ITradeManager
    {
        /// <summary>
        /// Initiate a trade request with another player.
        /// </summary>
        void InitiateTrade(IPlayer player, IPlayer target);

        /// <summary>
        /// Accept an incoming trade request.
        /// </summary>
        void AcceptTrade(IPlayer player);

        /// <summary>
        /// Decline an incoming trade request.
        /// </summary>
        void DeclineTrade(IPlayer player);

        /// <summary>
        /// Cancel the current trade.
        /// </summary>
        void CancelTrade(IPlayer player);

        /// <summary>
        /// Add an item to the trade by inventory bag index.
        /// </summary>
        void AddItem(IPlayer player, uint bagIndex, uint quantity);

        /// <summary>
        /// Remove an item from the trade by item guid.
        /// </summary>
        void RemoveItem(IPlayer player, ulong itemGuid);

        /// <summary>
        /// Set the money amount to trade.
        /// </summary>
        void SetMoney(IPlayer player, ulong amount);

        /// <summary>
        /// Commit the trade - both players have accepted.
        /// </summary>
        void CommitTrade(IPlayer player);

        /// <summary>
        /// Get the current trade session for a player, if any.
        /// </summary>
        ITradeSession GetTradeSession(IPlayer player);
    }
}
