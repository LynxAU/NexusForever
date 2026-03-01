using System.Collections.Generic;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Guild;
using NLog;

namespace NexusForever.WorldServer.Network.Message.Handler.Guild
{
    public class ClientWarPartyBossTokensRequestHandler : IMessageHandler<IWorldSession, ClientWarPartyBossTokensRequest>
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        public void HandleMessage(IWorldSession session, ClientWarPartyBossTokensRequest request)
        {
            IWarParty warParty = session.Player.GuildManager.GetGuild<IWarParty>(GuildType.WarParty);
            if (warParty == null)
            {
                log.Warn($"Player {session.Player.CharacterId} requested boss tokens with no war party.");
                return;
            }

            // If client supplied a specific guild id, enforce it matches the player's current war party.
            if (request.GuildIdentity.Id != 0ul && request.GuildIdentity.Id != warParty.Id)
            {
                log.Warn($"Player {session.Player.CharacterId} requested boss tokens for warparty {request.GuildIdentity.Id} but belongs to {warParty.Id}.");
                return;
            }

            // Build response with available boss tokens
            var response = new ServerWarPartyBossTokens
            {
                GuildIdentity = request.GuildIdentity,
                Tokens = BuildBossTokenList(warParty.BossTokens)
            };

            session.EnqueueMessageEncrypted(response);
        }

        /// <summary>
        /// Build list of boss tokens based on available token count.
        /// </summary>
        private static List<WarPartyBossToken> BuildBossTokenList(int availableTokens)
        {
            var tokens = new List<WarPartyBossToken>();

            foreach (HousingWarplotBossTokenEntry entry in GameTableManager.Instance.HousingWarplotBossToken.Entries)
            {
                if (entry == null)
                    continue;

                // Token is available if we've earned enough tokens to afford this one
                if (availableTokens > tokens.Count)
                {
                    tokens.Add(new WarPartyBossToken
                    {
                        TokenItem2Id = entry.Id,
                        Count = 1
                    });
                }
            }

            return tokens;
        }
    }
}
