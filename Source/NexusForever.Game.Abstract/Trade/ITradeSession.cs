using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static;

namespace NexusForever.Game.Abstract.Trade
{
    public interface ITradeSession
    {
        IPlayer Player { get; }
        IPlayer Target { get; }
        TradeState State { get; }
        bool IsPlayerReady { get; }
        bool IsTargetReady { get; }
        ulong PlayerMoney { get; }
        ulong TargetMoney { get; }
        
        /// <summary>
        /// Items being offered by the player in this trade.
        /// </summary>
        IReadOnlyList<TradeItem> PlayerItems { get; }

        /// <summary>
        /// Items being offered by the target in this trade.
        /// </summary>
        IReadOnlyList<TradeItem> TargetItems { get; }
    }

    public class TradeItem
    {
        public uint ItemId { get; set; }
        public ulong ItemGuid { get; set; }
        public uint Quantity { get; set; }
        public uint BagIndex { get; set; }
    }

    public enum TradeState
    {
        None        = 0,
        Pending    = 1,  // Invite sent, waiting for accept
        Active     = 2,   // Trade accepted, items being exchanged
        Committed  = 3,   // Both players committed, awaiting finalization
        Complete   = 4,   // Trade finished successfully
        Cancelled  = 5,   // Trade was cancelled
    }
}
