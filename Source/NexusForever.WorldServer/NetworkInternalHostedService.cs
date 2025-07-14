using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network.Internal.Message.Player;
using Rebus.Bus;

namespace NexusForever.WorldServer
{
    public class NetworkInternalHostedService : IHostedService
    {
        #region Dependency Injection

        private readonly IBus bus;

        public NetworkInternalHostedService(
            IBus bus)
        {
            this.bus = bus;
        }

        #endregion

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await bus.Subscribe<GroupActionResultMessage>();
            await bus.Subscribe<GroupFlagsUpdatedMessage>();
            await bus.Subscribe<GroupLootRulesUpdatedMessage>();
            await bus.Subscribe<GroupMarkerUpdatedMessage>();
            await bus.Subscribe<GroupMaxSizeUpdatedMessage>();
            await bus.Subscribe<GroupMemberAddedMessage>();
            await bus.Subscribe<GroupMemberFlagsUpdatedMessage>();
            await bus.Subscribe<GroupMemberJoinedMessage>();
            await bus.Subscribe<GroupMemberLeftMessage>();
            await bus.Subscribe<GroupMemberPositionUpdatedMessage>();
            await bus.Subscribe<GroupMemberPromotedMessage>();
            await bus.Subscribe<GroupMemberRealmUpdatedMessage>();
            await bus.Subscribe<GroupMemberRemovedMessage>();
            await bus.Subscribe<GroupMemberRequestedMessage>();
            await bus.Subscribe<GroupMemberRequestResultMessage>();
            await bus.Subscribe<GroupMemberStatsUpdatedMessage>();
            await bus.Subscribe<GroupPlayerInvitedMessage>();
            await bus.Subscribe<GroupPlayerInviteResultMessage>();
            await bus.Subscribe<GroupReadyCheckStartedMessage>();

            await bus.Subscribe<PlayerGroupAssociationUpdatedMessage>();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
