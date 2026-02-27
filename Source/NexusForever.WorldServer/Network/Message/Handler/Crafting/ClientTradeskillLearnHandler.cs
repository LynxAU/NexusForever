using System;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.WorldServer.Network.Message.Handler.Crafting
{
    public class ClientTradeskillLearnHandler : IMessageHandler<IWorldSession, ClientTradeskillLearn>
    {
        #region Dependency Injection

        private readonly ILogger<ClientTradeskillLearnHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientTradeskillLearnHandler(
            ILogger<ClientTradeskillLearnHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log = log;
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientTradeskillLearn learnRequest)
        {
            if (!Enum.IsDefined(typeof(TradeskillType), learnRequest.ToLearnTradeskillId))
            {
                session.EnqueueMessageEncrypted(new ServerTradeskillSigilResult
                {
                    TradeskillSigilResult = TradeskillResult.InvalidItem
                });
                return;
            }

            if (gameTableManager.Tradeskill.GetEntry((uint)learnRequest.ToLearnTradeskillId) == null)
            {
                session.EnqueueMessageEncrypted(new ServerTradeskillSigilResult
                {
                    TradeskillSigilResult = TradeskillResult.InvalidItem
                });
                return;
            }

            if (learnRequest.ToDropTradeskillId != 0)
                session.Player.TryDropTradeskill(learnRequest.ToDropTradeskillId);

            if (!session.Player.TryLearnTradeskill(learnRequest.ToLearnTradeskillId))
            {
                session.EnqueueMessageEncrypted(new ServerTradeskillSigilResult
                {
                    TradeskillSigilResult = TradeskillResult.Unlocked
                });
                return;
            }

            session.EnqueueMessageEncrypted(new ServerTradeskillSigilResult
            {
                TradeskillSigilResult = TradeskillResult.Success
            });

            session.EnqueueMessageEncrypted(new ServerProfessionUpdate
            {
                Tradeskill = new TradeskillInfo
                {
                    TradeskillId = learnRequest.ToLearnTradeskillId,
                    IsActive     = 1u
                }
            });

            session.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.LearnTradeskill, (uint)learnRequest.ToLearnTradeskillId, 1u);
            log.LogDebug("Player {CharacterId} learned tradeskill {TradeskillId}", session.Player.CharacterId, learnRequest.ToLearnTradeskillId);
        }
    }
}
