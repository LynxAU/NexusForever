using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Marketplace;

namespace NexusForever.WorldServer.Service
{
    /// <summary>
    /// Hosted service that periodically processes expired auctions and commodity orders.
    /// </summary>
    public class MarketplaceHostedService : BackgroundService
    {
        private readonly ILogger<MarketplaceHostedService> log;
        private readonly IServiceProvider serviceProvider;

        // Run expiration processing every 60 seconds
        private readonly TimeSpan ProcessInterval = TimeSpan.FromSeconds(60);

        public MarketplaceHostedService(
            ILogger<MarketplaceHostedService> log,
            IServiceProvider serviceProvider)
        {
            this.log = log;
            this.serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            log.LogInformation("MarketplaceHostedService starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = serviceProvider.CreateScope();
                    
                    // Process expired auctions
                    var auctionManager = scope.ServiceProvider.GetRequiredService<AuctionManager>();
                    await auctionManager.ProcessExpiredAuctionsAsync();

                    // Process expired commodity orders
                    var commodityManager = scope.ServiceProvider.GetRequiredService<CommodityExchangeManager>();
                    await commodityManager.ProcessExpiredOrdersAsync();
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error processing marketplace expirations");
                }

                await Task.Delay(ProcessInterval, stoppingToken);
            }

            log.LogInformation("MarketplaceHostedService stopping...");
        }
    }
}
