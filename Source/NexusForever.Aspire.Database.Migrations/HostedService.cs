using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Group;
using NexusForever.Database.World;

namespace NexusForever.Aspire.Database.Migrations
{
    public class HostedService : BackgroundService
    {
        #region Dependency Injection

        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly AuthContext _authContext;
        private readonly CharacterContext _characterContext;
        private readonly WorldContext _worldContext;
        private readonly GroupContext _groupContext;

        public HostedService(
            IHostApplicationLifetime hostApplicationLifetime,
            AuthContext authContext,
            CharacterContext characterContext,
            WorldContext worldContext,
            GroupContext groupContext)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _authContext             = authContext;
            _characterContext        = characterContext;
            _worldContext            = worldContext;
            _groupContext            = groupContext;
        }

        #endregion

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _authContext.Database.MigrateAsync();
            await _characterContext.Database.MigrateAsync();
            await _worldContext.Database.MigrateAsync();
            await _groupContext.Database.MigrateAsync();

            _hostApplicationLifetime.StopApplication();
        }
    }
}
