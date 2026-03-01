using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Path
{
    public class ClientPathMissionAttemptScientistExperimentationHandler : IMessageHandler<IWorldSession, ClientPathMissionAttemptScientistExperimentation>
    {
        public void HandleMessage(IWorldSession session, ClientPathMissionAttemptScientistExperimentation message)
        {
            session.Player.PathManager.HandleScientistExperimentation(message.Choices);
        }
    }
}
