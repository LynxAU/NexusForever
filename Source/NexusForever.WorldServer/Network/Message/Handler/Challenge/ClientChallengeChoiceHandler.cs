using NexusForever.Game.Static.Challenges;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Challenges;

namespace NexusForever.WorldServer.Network.Message.Handler.Challenge
{
    public class ClientChallengeChoiceHandler : IMessageHandler<IWorldSession, ClientChallengeChoice>
    {
        public void HandleMessage(IWorldSession session, ClientChallengeChoice choice)
        {
            if (session.Player == null)
                return;

            switch (choice.Choice)
            {
                case ChallengeChoice.Activate:
                    session.Player.ChallengeManager.ChallengeActivate(choice.ChallengeId);
                    break;

                case ChallengeChoice.Abandon:
                    session.Player.ChallengeManager.ChallengeAbandon(choice.ChallengeId);
                    break;

                // Shared challenge acceptance/decline — not yet implemented
                case ChallengeChoice.AcceptShared:
                case ChallengeChoice.DeclineShared:
                    break;
            }
        }
    }
}
