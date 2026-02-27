using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Trade;

namespace NexusForever.Game.Trade
{
    public class TradeSession : ITradeSession
    {
        public IPlayer Player { get; }
        public IPlayer Target { get; }
        public TradeState State { get; private set; } = TradeState.Pending;

        public bool IsPlayerReady { get; private set; }
        public bool IsTargetReady { get; private set; }

        public ulong PlayerMoney { get; private set; }
        public ulong TargetMoney { get; private set; }

        private readonly List<TradeItem> playerItems = new();
        private readonly List<TradeItem> targetItems = new();

        public IReadOnlyList<TradeItem> PlayerItems => playerItems.AsReadOnly();
        public IReadOnlyList<TradeItem> TargetItems => targetItems.AsReadOnly();

        public TradeSession(IPlayer player, IPlayer target)
        {
            Player = player;
            Target = target;
        }

        public void StartTrade()
        {
            State = TradeState.Active;
            IsPlayerReady = false;
            IsTargetReady = false;
        }

        public IPlayer GetOtherPlayer(IPlayer player)
        {
            return player.CharacterId == Player.CharacterId ? Target : Player;
        }

        public void AddItem(IPlayer player, TradeItem item)
        {
            if (player.CharacterId == Player.CharacterId)
                playerItems.Add(item);
            else
                targetItems.Add(item);

            // Reset ready state when items change
            IsPlayerReady = false;
            IsTargetReady = false;
        }

        public void RemoveItem(IPlayer player, ulong itemGuid)
        {
            if (player.CharacterId == Player.CharacterId)
                playerItems.RemoveAll(i => i.ItemGuid == itemGuid);
            else
                targetItems.RemoveAll(i => i.ItemGuid == itemGuid);

            IsPlayerReady = false;
            IsTargetReady = false;
        }

        public void SetMoney(IPlayer player, ulong amount)
        {
            if (player.CharacterId == Player.CharacterId)
                PlayerMoney = amount;
            else
                TargetMoney = amount;

            // Reset ready state when money changes
            IsPlayerReady = false;
            IsTargetReady = false;
        }

        public void SetReady(IPlayer player)
        {
            if (player.CharacterId == Player.CharacterId)
                IsPlayerReady = true;
            else
                IsTargetReady = true;
        }

        public void ReturnItemsAndMoney()
        {
            // Items are automatically returned when removed from the trade session
            // Money is tracked but not held in escrow - it's verified at commit time
            State = TradeState.Cancelled;
        }

        public void Complete()
        {
            State = TradeState.Complete;
        }
    }
}
