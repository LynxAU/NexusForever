using System;
using System.Collections.Generic;
using System.Linq;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Abilities;
using NexusForever.GameTable;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Configuration;

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
                    if (requestAmpReset.Value > ushort.MaxValue)
                        throw new InvalidPacketValueException();
                    if (gameTableManager.EldanAugmentation.GetEntry(requestAmpReset.Value) == null)
                        throw new InvalidPacketValueException();
                    break;
                default:
                    throw new InvalidPacketValueException();
            }

            IActionSetAmp[] respecTargets = GetRespecTargets(actionSet, requestAmpReset.RespecType, requestAmpReset.Value);
            ulong respecCost = CalculateRespecCost(respecTargets);
            if (respecCost > 0 && !session.Player.CurrencyManager.CanAfford(CurrencyType.Credits, respecCost))
            {
                SendRespecResult(session, requestAmpReset.SpecIndex, LimitedActionSetResult.InsufficientAbilityPoints);
                return;
            }

            if (respecCost > 0)
                session.Player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, respecCost);

            actionSet.RemoveAmp(requestAmpReset.RespecType, requestAmpReset.Value);
            session.EnqueueMessageEncrypted(actionSet.BuildServerAmpList());
            SendRespecResult(session, requestAmpReset.SpecIndex, LimitedActionSetResult.Ok);
        }

        private static IActionSetAmp[] GetRespecTargets(IActionSet actionSet, AmpRespecType type, uint value)
        {
            return type switch
            {
                AmpRespecType.Full => actionSet.Amps.ToArray(),
                AmpRespecType.Section => actionSet.Amps
                    .Where(a => a.Entry.EldanAugmentationCategoryId == value)
                    .ToArray(),
                AmpRespecType.Single => actionSet.GetAmp((ushort)value) is IActionSetAmp amp ? [amp] : [],
                _ => []
            };
        }

        private static ulong CalculateRespecCost(IReadOnlyCollection<IActionSetAmp> respecTargets)
        {
            if (respecTargets.Count == 0)
                return 0ul;

            ulong costPerAmp = SharedConfiguration.Instance.Get<RealmConfig>().AmpRespecCostPerAmpCredits;
            if (costPerAmp == 0ul)
                return 0ul;

            return (ulong)respecTargets.Count * costPerAmp;
        }

        private static void SendRespecResult(IWorldSession session, byte specIndex, LimitedActionSetResult result)
        {
            session.EnqueueMessageEncrypted(new ServerAmpRespecResult
            {
                Results =
                [
                    new ServerAmpRespecResult.AmpResult
                    {
                        SpecIndex = specIndex,
                        Result = result
                    }
                ]
            });
        }
    }
}
