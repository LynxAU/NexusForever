using NexusForever.Game.Static.Account;
using NexusForever.GameTable;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Path
{
    public class ClientPathUnlockHandler : IMessageHandler<IWorldSession, ClientPathUnlockRequest>
    {
        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;

        public ClientPathUnlockHandler(
            IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientPathUnlockRequest clientPathUnlockRequest)
        {
            var formulaEntry = gameTableManager.GameFormula.GetEntry(2365);
            if (formulaEntry == null)
            {
                session.Player.PathManager.SendServerPathUnlockResult(GenericError.Params);
                return;
            }

            uint unlockCost = formulaEntry.Dataint0;

            GenericError CanUnlockPath()
            {
                bool hasEnoughTokens = session.Account.CurrencyManager.CanAfford(AccountCurrencyType.ServiceToken, unlockCost);
                if (!hasEnoughTokens)
                    return GenericError.PathInsufficientFunds;

                if (session.Player.PathManager.IsPathUnlocked(clientPathUnlockRequest.Path))
                    return GenericError.PathAlreadyUnlocked;

                return GenericError.Ok;
            }

            GenericError result = CanUnlockPath();
            if (result != GenericError.Ok)
            {
                session.Player.PathManager.SendServerPathUnlockResult(result);
                return;
            }

            session.Account.CurrencyManager.CurrencySubtractAmount(AccountCurrencyType.ServiceToken, unlockCost);
            session.Player.PathManager.UnlockPath(clientPathUnlockRequest.Path);
        }
    }
}
