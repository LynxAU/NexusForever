using System;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Abilities;
using NexusForever.GameTable;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Abilities;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientRespecAmpsHandler : IMessageHandler<IWorldSession, ClientRespecAmps>
    {
        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;

        public ClientRespecAmpsHandler(
            IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientRespecAmps requestAmpReset)
        {
            IActionSet actionSet;
            try
            {
                actionSet = session.Player.SpellManager.GetActionSet(requestAmpReset.SpecIndex);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new InvalidPacketValueException();
            }

            switch (requestAmpReset.RespecType)
            {
                case AmpRespecType.Full:
                    if (requestAmpReset.Value != 0u)
                        throw new InvalidPacketValueException();
                    break;
                case AmpRespecType.Section:
                    if (gameTableManager.EldanAugmentationCategory.GetEntry(requestAmpReset.Value) == null)
                        throw new InvalidPacketValueException();
                    break;
                case AmpRespecType.Single:
                    if (gameTableManager.EldanAugmentation.GetEntry(requestAmpReset.Value) == null)
                        throw new InvalidPacketValueException();
                    break;
                default:
                    throw new InvalidPacketValueException();
            }

            // Respec cost handling is deferred; no cost currently charged.
            actionSet.RemoveAmp(requestAmpReset.RespecType, requestAmpReset.Value);
            session.EnqueueMessageEncrypted(actionSet.BuildServerAmpList());
        }
    }
}
