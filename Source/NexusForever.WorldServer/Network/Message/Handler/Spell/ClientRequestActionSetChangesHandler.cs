using System;
using System.Collections.Generic;
using System.Linq;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Abilities;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientRequestActionSetChangesHandler : IMessageHandler<IWorldSession, ClientRequestActionSetChanges>
    {
        public void HandleMessage(IWorldSession session, ClientRequestActionSetChanges requestActionSetChanges)
        {
            IActionSet actionSet;
            try
            {
                actionSet = session.Player.SpellManager.GetActionSet(requestActionSetChanges.ActionSetIndex);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new InvalidPacketValueException();
            }

            List<IActionSetShortcut> shortcuts = actionSet.Actions.ToList();
            for (UILocation i = 0; i < (UILocation)requestActionSetChanges.Actions.Count; i++)
            {
                IActionSetShortcut shortcut = actionSet.GetShortcut(i);
                if (shortcut != null)
                    actionSet.RemoveShortcut(i);

                uint spell4BaseId = requestActionSetChanges.Actions[(int)i];
                if (spell4BaseId == 0u)
                    continue;

                if (session.Player.SpellManager.GetSpell(spell4BaseId) == null)
                    throw new InvalidPacketValueException();

                IActionSetShortcut existingShortcut = shortcuts.SingleOrDefault(s => s.ObjectId == spell4BaseId);
                byte tier = existingShortcut?.Tier ?? 1;
                actionSet.AddShortcut(i, ShortcutType.SpellbookItem, spell4BaseId, tier);
            }

            foreach (ClientRequestActionSetChanges.ActionTier actionTier in requestActionSetChanges.ActionTiers)
            {
                if (actionTier.Tier > ActionSet.MaxTier)
                    throw new InvalidPacketValueException();

                if (session.Player.SpellManager.GetSpell(actionTier.Action) == null)
                    throw new InvalidPacketValueException();

                session.Player.SpellManager.UpdateSpell(actionTier.Action, actionTier.Tier, requestActionSetChanges.ActionSetIndex);
            }

            session.EnqueueMessageEncrypted(actionSet.BuildServerActionSet());
            if (requestActionSetChanges.ActionTiers.Count > 0)
                session.Player.SpellManager.SendServerAbilityPoints();

            // only new AMP can be added with this packet, filter out existing ones
            List<ushort> newAmps = requestActionSetChanges.Amps
                .Except(actionSet.Amps
                    .Select(a => (ushort)a.Entry.Id))
                .ToList();

            if (newAmps.Count > 0)
            {
                foreach (ushort id in newAmps)
                    actionSet.AddAmp(id);

                session.EnqueueMessageEncrypted(actionSet.BuildServerAmpList());
            }
        }
    }
}
