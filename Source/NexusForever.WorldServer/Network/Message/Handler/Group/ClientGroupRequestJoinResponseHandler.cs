using NexusForever.Game;
using NexusForever.Game.Static.Quest;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;

namespace NexusForever.WorldServer.Network.Message.Handler.Group
{
    public class ClientGroupRequestJoinResponseHandler : IMessageHandler<IWorldSession, ClientGroupRequestJoinResponse>
    {
        #region Dependency Injection

        private readonly IInternalMessagePublisher messagePublisher;

        public ClientGroupRequestJoinResponseHandler(
            IInternalMessagePublisher messagePublisher)
        {
            this.messagePublisher = messagePublisher;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientGroupRequestJoinResponse groupRequestJoinResponse)
        {
            messagePublisher.PublishAsync(new GroupMemberRequestReponseMessage
            {
                GroupId     = groupRequestJoinResponse.GroupId,
                Identity    = session.Player.Identity.ToInternalIdentity(),
                InviteeName = groupRequestJoinResponse.InviteeName,
                Response    = groupRequestJoinResponse.AcceptedRequest,
            }).FireAndForgetAsync();

            // Trigger Unknown40 objectives - participating in group content
            // Data is unknown but triggers when player joins/participates in group content
            if (groupRequestJoinResponse.AcceptedRequest)
                session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.Unknown40, 0, 1);
        }
    }
}
