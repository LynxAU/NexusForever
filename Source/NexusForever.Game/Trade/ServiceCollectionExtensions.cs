using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Trade;
using NexusForever.Game.Trade;

namespace NexusForever.Game.Trade
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameTrade(this IServiceCollection sc)
        {
            sc.AddSingleton<ITradeManager, TradeManager>();
        }
    }
}
