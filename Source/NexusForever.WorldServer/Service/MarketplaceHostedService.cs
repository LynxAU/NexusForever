using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Marketplace;
using NexusForever.Shared.Configuration;

namespace NexusForever.WorldServer.Service
{
    /// <summary>
    /// Hosted service that periodically processes expired auctions and commodity orders.
    /// </summary>
    public class MarketplaceHostedService : BackgroundService
    {
        private sealed class MarketplaceCharacterContextFactory : IDbContextFactory<CharacterContext>
        {
            private readonly IConnectionString connectionString;

            public MarketplaceCharacterContextFactory(IConnectionString connectionString)
            {
                this.connectionString = connectionString;
            }

            public CharacterContext CreateDbContext()
            {
                return new CharacterContext(connectionString);
            }
        }

        private readonly ILogger<MarketplaceHostedService> log;
        private AuctionManager auctionManager;
        private CommodityExchangeManager commodityManager;

        // Run expiration processing every 60 seconds
        private readonly TimeSpan ProcessInterval = TimeSpan.FromSeconds(60);

        public MarketplaceHostedService(ILogger<MarketplaceHostedService> log)
        {
            this.log = log;
        }

        private void EnsureManagers()
        {
            if (auctionManager != null && commodityManager != null)
                return;

            DatabaseConfig databaseConfig = SharedConfiguration.Instance.Get<DatabaseConfig>();
            if (databaseConfig?.Character == null)
                throw new InvalidOperationException("Missing Database:Character configuration for marketplace.");

            IDbContextFactory<CharacterContext> dbContextFactory = new MarketplaceCharacterContextFactory(databaseConfig.Character);
            auctionManager = new AuctionManager(dbContextFactory);
            commodityManager = new CommodityExchangeManager(dbContextFactory);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            log.LogInformation("MarketplaceHostedService starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    EnsureManagers();
                    await auctionManager.ProcessExpiredAuctionsAsync();

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
