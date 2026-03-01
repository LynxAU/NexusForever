using System.Collections.Generic;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Guild;
using NexusForever.Network.World.Message.Model.Housing;
using NLog;

namespace NexusForever.WorldServer.Network.Message.Handler.Guild
{
    /// <summary>
    /// Handle client request to set a warplot plug in a specific slot.
    /// </summary>
    public class ClientWarPlotPlugSetHandler : IMessageHandler<IWorldSession, ClientWarPlotPlugSet>
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        public void HandleMessage(IWorldSession session, ClientWarPlotPlugSet message)
        {
            IWarParty warParty = session.Player.GuildManager.GetGuild<IWarParty>(GuildType.WarParty);
            if (warParty == null)
            {
                log.Warn($"Player {session.Player.CharacterId} attempted to set warplot plug with no war party.");
                return;
            }

            // If client supplied a specific guild id, enforce it matches the player's current war party.
            if (message.GuildIdentity.Id != 0ul && message.GuildIdentity.Id != warParty.Id)
            {
                log.Warn($"Player {session.Player.CharacterId} attempted to set warplot plug for warparty {message.GuildIdentity.Id} but belongs to {warParty.Id}.");
                return;
            }

            IGuildMember member = warParty.GetMember(session.Player.CharacterId);
            if (member == null)
            {
                log.Warn($"Player {session.Player.CharacterId} attempted to set warplot plug but is not in warparty {warParty.Id}.");
                return;
            }

            if (message.PlugItemId == 0)
            {
                if (!member.Rank.HasPermission(GuildRankPermission.RemoveWarPlotPlugs))
                {
                    log.Warn($"Player {session.Player.CharacterId} attempted to remove warplot plug without permission.");
                    return;
                }
            }
            else if (!member.Rank.HasPermission(GuildRankPermission.AddWarPlotPlugs))
            {
                log.Warn($"Player {session.Player.CharacterId} attempted to add warplot plug without permission.");
                return;
            }

            // Validate plug index range.
            if (message.PlugIndex > 15)
            {
                log.Warn($"Player {session.Player.CharacterId} attempted to set warplot plug at invalid index {message.PlugIndex}.");
                return;
            }

            if (message.PlugItemId != 0 && !IsValidPlugItem(message.PlugItemId))
            {
                log.Warn($"Player {session.Player.CharacterId} attempted to set invalid warplot plug item {message.PlugItemId}.");
                return;
            }

            ushort existingPlug = warParty.GetPlug(message.PlugIndex);
            if (existingPlug == message.PlugItemId)
            {
                SendPlugState(session, warParty);
                return;
            }

            if (!TrySpendBossTokenIfRequired(warParty, message.PlugItemId))
            {
                log.Warn($"Player {session.Player.CharacterId} attempted to set warplot plug item {message.PlugItemId} without enough boss tokens.");
                return;
            }

            warParty.SetPlug(message.PlugIndex, message.PlugItemId);

            log.Debug($"Player {session.Player.CharacterId} set warplot plug index {message.PlugIndex} to item {message.PlugItemId} for warparty {warParty.Id}.");

            SendPlugState(session, warParty);
            SendBossTokenState(session, warParty);
        }

        private void SendPlugState(IWorldSession session, IWarParty warParty)
        {
            var plugState = new ServerWarPlotPlugState
            {
                CratedDecorCount = 0,
                Unknown1 = 0,
                PlacedDecorCount = 0,
                Plugs = new List<WarPlotPlug>()
            };

            foreach (KeyValuePair<byte, ushort> kvp in warParty.GetPlugSlots())
            {
                if (!IsValidPlugItem(kvp.Value))
                    continue;

                plugState.Plugs.Add(new WarPlotPlug
                {
                    Index = kvp.Key,
                    Health = 1000,
                    HealthMax = 1000,
                    Tier = 1
                });
            }

            plugState.PlacedDecorCount = (ushort)plugState.Plugs.Count;

            session.EnqueueMessageEncrypted(plugState);
        }

        private static bool IsValidPlugItem(ushort plugItemId)
        {
            foreach (HousingWarplotPlugInfoEntry entry in GameTableManager.Instance.HousingWarplotPlugInfo.Entries)
            {
                if (entry != null && entry.HousingPlugItemId == plugItemId)
                    return true;
            }

            return false;
        }

        private static bool TrySpendBossTokenIfRequired(IWarParty warParty, ushort plugItemId)
        {
            if (plugItemId == 0)
                return true;

            Item2Entry itemEntry = GameTableManager.Instance.Item.GetEntry(plugItemId);
            if (itemEntry == null || itemEntry.HousingWarplotBossTokenId == 0)
                return true;

            return warParty.SpendBossToken();
        }

        private static void SendBossTokenState(IWorldSession session, IWarParty warParty)
        {
            var response = new ServerWarPartyBossTokens
            {
                GuildIdentity = warParty.Identity,
                Tokens = BuildBossTokenList(warParty.BossTokens)
            };

            session.EnqueueMessageEncrypted(response);
        }

        private static List<WarPartyBossToken> BuildBossTokenList(int availableTokens)
        {
            var tokens = new List<WarPartyBossToken>();

            foreach (HousingWarplotBossTokenEntry entry in GameTableManager.Instance.HousingWarplotBossToken.Entries)
            {
                if (entry == null)
                    continue;

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
