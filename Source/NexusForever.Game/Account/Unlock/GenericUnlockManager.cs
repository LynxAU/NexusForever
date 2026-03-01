using System.Collections;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Unlock;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.GenericUnlock;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.GenericUnlock;

namespace NexusForever.Game.Account.Unlock
{
    public class GenericUnlockManager : IGenericUnlockManager
    {
        private readonly IAccount account;
        private readonly Dictionary<uint, IGenericUnlock> unlocks = new();

        /// <summary>
        /// Create a new <see cref="IGenericUnlockManager"/> from <see cref="AccountModel"/> database model.
        /// </summary>
        public GenericUnlockManager(IAccount account, AccountModel model)
        {
            this.account = account;

            foreach (AccountGenericUnlockModel unlockModel in model.AccountGenericUnlock)
                unlocks.Add(unlockModel.Entry, new GenericUnlock(account, unlockModel));
        }

        public void Save(AuthContext context)
        {
            foreach (IGenericUnlock genericUnlock in unlocks.Values)
                genericUnlock.Save(context);
        }

        /// <summary>
        /// Create new <see cref="IGenericUnlock"/> entry from supplied id.
        /// </summary>
        public void Unlock(ushort genericUnlockEntryId)
        {
            GenericUnlockEntryEntry entry = GameTableManager.Instance.GenericUnlockEntry.GetEntry(genericUnlockEntryId);
            if (entry == null)
            {
                SendUnlockResult(GenericUnlockResult.Invalid);
                return;
            }

            if (unlocks.ContainsKey(genericUnlockEntryId))
            {
                SendUnlockResult(GenericUnlockResult.AlreadyUnlocked);
                return;
            }

            unlocks.Add(genericUnlockEntryId, new GenericUnlock(account, entry));

            SendUnlock(genericUnlockEntryId);
            SendUnlockResult(GenericUnlockResult.Granted);
        }

        /// <summary>
        /// Unlock all generic unlocks of supplied <see cref="GenericUnlockType"/>.
        /// </summary>
        public void UnlockAll(GenericUnlockType type)
        {
            foreach (GenericUnlockEntryEntry entry in GameTableManager.Instance.GenericUnlockEntry.Entries
                .Where(e => e.GenericUnlockTypeEnum == type))
            {
                if (unlocks.ContainsKey(entry.Id))
                    continue;

                unlocks.Add(entry.Id, new GenericUnlock(account, entry));
                SendUnlock((ushort)entry.Id);
            }
        }

        public bool IsUnlocked(GenericUnlockType type, uint objectId)
        {
            return unlocks.Values.Any(e => e.Type == type && e.Entry.UnlockObject == objectId);
        }

        public bool IsDyeUnlocked(uint dyeColourRampId)
        {
            return unlocks.Values
                .Where(u => u.Type == GenericUnlockType.Dye)
                .Any(e => e.Entry.UnlockObject == dyeColourRampId);
        }

        /// <summary>
        /// Unlock all <see cref="IGenericUnlock"/> entries contained in the supplied set id.
        /// Sends a single combined result: Granted, PartialUnlock, or AlreadyUnlocked.
        /// </summary>
        public void UnlockSet(uint genericUnlockSetId)
        {
            GenericUnlockSetEntry setEntry = GameTableManager.Instance.GenericUnlockSet.GetEntry(genericUnlockSetId);
            if (setEntry == null)
            {
                SendUnlockResult(GenericUnlockResult.Invalid);
                return;
            }

            uint[] entryIds =
            {
                setEntry.GenericUnlockEntryId00,
                setEntry.GenericUnlockEntryId01,
                setEntry.GenericUnlockEntryId02,
                setEntry.GenericUnlockEntryId03,
                setEntry.GenericUnlockEntryId04,
                setEntry.GenericUnlockEntryId05,
            };

            int granted      = 0;
            int alreadyOwned = 0;

            foreach (uint entryId in entryIds)
            {
                if (entryId == 0)
                    continue;

                GenericUnlockEntryEntry entryEntry = GameTableManager.Instance.GenericUnlockEntry.GetEntry(entryId);
                if (entryEntry == null)
                    continue;

                if (unlocks.ContainsKey(entryId))
                {
                    alreadyOwned++;
                    continue;
                }

                unlocks.Add(entryId, new GenericUnlock(account, entryEntry));
                SendUnlock((ushort)entryId);
                granted++;
            }

            if (granted == 0)
                SendUnlockResult(GenericUnlockResult.AlreadyUnlocked);
            else if (alreadyOwned > 0)
                SendUnlockResult(GenericUnlockResult.PartialUnlock);
            else
                SendUnlockResult(GenericUnlockResult.Granted);
        }

        /// <summary>
        /// Send <see cref="IGenericUnlock"/> with supplied id to client.
        /// </summary>
        public void SendUnlock(ushort genericUnlockEntryId)
        {
            account.Session.EnqueueMessageEncrypted(new ServerGenericUnlock
            {
                GenericUnlockEntryId = genericUnlockEntryId
            });
        }

        /// <summary>
        /// Send <see cref="GenericUnlockResult"/> to client.
        /// </summary>
        public void SendUnlockResult(GenericUnlockResult result)
        {
            account.Session.EnqueueMessageEncrypted(new ServerGenericUnlockResult
            {
                Result = result
            });
        }

        /// <summary>
        /// Send all <see cref="IGenericUnlock"/> entries to client.
        /// </summary>
        public void SendUnlockList()
        {
            account.Session.EnqueueMessageEncrypted(new ServerGenericUnlockAccountList
            {
                GenericUnlockEntryIds = unlocks.Keys.ToList()
            });
        }

        public IEnumerator<IGenericUnlock> GetEnumerator()
        {
            return unlocks.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
