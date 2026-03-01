using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.WorldServer.Network.Message.Handler.Guild
{
    public class ClientWarPartyBossTokensRequestHandler : IMessageHandler<IWorldSession, ClientWarPartyBossTokensRequest>
    {
        public void HandleMessage(IWorldSession session, ClientWarPartyBossTokensRequest request)
        {
            // Boss token inventory is not yet implemented.
            // Respond with an empty token list so the client UI initialises cleanly.
            session.EnqueueMessageEncrypted(new ServerWarPartyBossTokens
            {
                GuildIdentity = request.GuildIdentity,
                Tokens        = []
            });
        }
    }
}
