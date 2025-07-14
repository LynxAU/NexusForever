using Microsoft.Extensions.DependencyInjection;
using NexusForever.Shared;
using NexusForever.WorldServer.Command;
using NexusForever.WorldServer.Network.Internal.Handler.Group;
using NexusForever.WorldServer.Network.Internal.Handler.Player;
using Rebus.Config;

namespace NexusForever.WorldServer
{
    public static class ServiceCollectionExtensions
    {
        public static void AddWorld(this IServiceCollection sc)
        {
            sc.AddSingletonLegacy<ICommandManager, CommandManager>();
            sc.AddSingletonLegacy<ILoginQueueManager, LoginQueueManager>();
        }

        public static IServiceCollection AddNetworkInternalHandlers(this IServiceCollection sc)
        {
            sc.AddRebusHandler<GroupActionResultHandler>();
            sc.AddRebusHandler<GroupFlagsUpdatedHandler>();
            sc.AddRebusHandler<GroupLootRulesUpdatedHandler>();
            sc.AddRebusHandler<GroupMarkerUpdatedHandler>();
            sc.AddRebusHandler<GroupMaxSizeUpdatedHandler>();
            sc.AddRebusHandler<GroupMemberAddedHandler>();
            sc.AddRebusHandler<GroupMemberFlagsUpdatedHandler>();
            sc.AddRebusHandler<GroupMemberJoinedHandler>();
            sc.AddRebusHandler<GroupMemberLeftHandler>();
            sc.AddRebusHandler<GroupMemberPositionUpdatedHandler>();
            sc.AddRebusHandler<GroupMemberPromotedHandler>();
            sc.AddRebusHandler<GroupMemberRealmUpdatedHandler>();
            sc.AddRebusHandler<GroupMemberRemovedHandler>();
            sc.AddRebusHandler<GroupMemberRequestedHandler>();
            sc.AddRebusHandler<GroupMemberRequestResultHandler>();
            sc.AddRebusHandler<GroupMemberStatsUpdatedHandler>();
            sc.AddRebusHandler<GroupPlayerInvitedHandler>();
            sc.AddRebusHandler<GroupPlayerInviteResultHandler>();
            sc.AddRebusHandler<GroupReadyCheckStartedHandler>();

            sc.AddRebusHandler<PlayerGroupAssociationUpdatedHandler>();

            return sc;
        }
    }
}
