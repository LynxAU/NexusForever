using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Static.Map.Lock;
using NexusForever.Shared;

namespace NexusForever.Game.Map.Instance
{
    public class ContentInstancedMap<T> : InstancedMap<T> where T : class, IContentMapInstance
    {
        #region Dependency Injection

        private readonly IMapLockManager mapLockManager;
        private readonly IMatchManager matchManager;
        private readonly IPlayerManager playerManager;
        private readonly IFactory<T> instanceFactory;

        public ContentInstancedMap(
            IMapLockManager mapLockManager,
            IMatchManager matchManager,
            IPlayerManager playerManager,
            IFactory<T> instanceFactory)
        {
            this.mapLockManager = mapLockManager;
            this.matchManager = matchManager;
            this.playerManager = playerManager;
            this.instanceFactory = instanceFactory;
        }

        #endregion

        protected override IMapLock GetMapLock(IPlayer player)
        {
            IMatch match = matchManager.GetMatchCharacter(player.Identity).Match;
            if (match != null)
            {
                IMapLock matchMapLock = mapLockManager.GetMatchLock(match.Guid, Entry.Id);
                return matchMapLock ?? mapLockManager.CreateMatchLock(match);
            }

            if (player.GroupAssociation != 0)
            {
                foreach (IPlayer groupMember in playerManager)
                {
                    if (groupMember.CharacterId == player.CharacterId
                        || groupMember.GroupAssociation != player.GroupAssociation)
                        continue;

                    IMapLock groupLock = mapLockManager.GetSoloLock(groupMember.Identity, Entry.Id);
                    if (groupLock != null)
                    {
                        mapLockManager.AssignSoloLock(player.Identity, groupLock);
                        return groupLock;
                    }
                }
            }

            IMapLock soloMapLock = mapLockManager.GetSoloLock(player.Identity, Entry.Id);
            return soloMapLock ?? mapLockManager.CreateSoloLock(player.Identity, Entry.Id);
        }

        protected override T CreateInstance(IPlayer player, IMapLock mapLock)
        {
            T instance = instanceFactory.Resolve();
            instance.Initialise(Entry, mapLock);

            // it is possible for a content map lock to not be a match lock
            // this could occur if a player or party enters a map via the instance portal
            if (mapLock.Type == MapLockType.Match)
            {
                IMatch match = matchManager.GetMatchCharacter(player.Identity).Match;
                if (match != null)
                    instance.SetMatch(match);
            }

            return instance;
        }
    }
}
