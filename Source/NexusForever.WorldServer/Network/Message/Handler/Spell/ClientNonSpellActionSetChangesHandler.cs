using System;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Abilities;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientNonSpellActionSetChangesHandler : IMessageHandler<IWorldSession, ClientNonSpellActionSetChanges>
    {
        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;

        public ClientNonSpellActionSetChangesHandler(
            IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientNonSpellActionSetChanges requestActionSetChanges)
        {
            if ((int)requestActionSetChanges.ActionBarIndex >= ActionSet.MaxActionCount)
                throw new InvalidPacketValueException();

            switch (requestActionSetChanges.ShortcutType)
            {
                case ShortcutType.BagItem:
                    if (gameTableManager.Item.GetEntry(requestActionSetChanges.ObjectId) == null)
                        throw new InvalidPacketValueException();
                    break;
                case ShortcutType.SpellbookItem:
                    throw new InvalidPacketValueException();
                case ShortcutType.None:
                    break;
                case ShortcutType.Macro:
                    // Allowed. Macros appear to be client-owned.
                    break;
                case ShortcutType.GameCommand:
                    if (requestActionSetChanges.ObjectId == 0u)
                        throw new InvalidPacketValueException();
                    break;
                default:
                    throw new InvalidPacketValueException();
            }

            IActionSet actionSet;
            try
            {
                actionSet = session.Player.SpellManager.GetActionSet(requestActionSetChanges.SpecIndex);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new InvalidPacketValueException();
            }

            if (requestActionSetChanges.ObjectId == 0u || requestActionSetChanges.ShortcutType == ShortcutType.None)
            {
                IActionSetShortcut shortcut = actionSet.GetShortcut(requestActionSetChanges.ActionBarIndex);
                if (shortcut != null)
                    actionSet.RemoveShortcut(requestActionSetChanges.ActionBarIndex);
                return;
            }

            actionSet.AddShortcut(requestActionSetChanges.ActionBarIndex, requestActionSetChanges.ShortcutType, requestActionSetChanges.ObjectId, 0);
        }
    }
}
