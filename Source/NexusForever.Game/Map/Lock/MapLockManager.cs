using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Static.Map.Lock;
using NexusForever.Shared.Configuration;
using NexusForever.Shared;

namespace NexusForever.Game.Map.Lock
{
    // legacy singleton still required for guild operations which don't use dependency injection yet...
    public class MapLockManager : Singleton<IMapLockManager>, IMapLockManager
    {
        private readonly ConcurrentDictionary<Identity, IMapLockCollection> soloLocks = [];
        private readonly ConcurrentDictionary<Guid, IMapLockCollection> matchLocks = [];
        private readonly ConcurrentDictionary<ulong, IResidenceMapLock> residenceLocks = [];

        #region Dependency Injection

        private readonly IFactoryInterface<IMapLock> mapLockFactory;
        private readonly IFactory<IMapLockCollection> mapLockCollectionFactory;
        private readonly IPlayerManager playerManager;
        private readonly ILogger<MapLockManager> log;

        public MapLockManager(
            IFactoryInterface<IMapLock> mapLockFactory,
            IFactory<IMapLockCollection> mapLockCollectionFactory,
            IPlayerManager playerManager,
            ILogger<MapLockManager> log)
        {
            this.mapLockFactory           = mapLockFactory;
            this.mapLockCollectionFactory = mapLockCollectionFactory;
            this.playerManager            = playerManager;
            this.log                      = log;
        }

        #endregion

        public void Initialise()
        {
            // Hydrate currently-online player lock data from persisted instance state.
            foreach (IPlayer player in playerManager)
                HydratePersistedSoloLocks(player);
        }

        /// <summary>
        /// Create a new solo <see cref="IMapLock"/> for supplied character id and world id.
        /// </summary>
        public IMapLock CreateSoloLock(Identity identity, uint worldId)
        {
            IMapLock existing = GetSoloLock(identity, worldId);
            if (existing != null)
                return existing;

            IMapLock mapLock = CreateLock<IMapLock>(MapLockType.Solo, worldId);
            AssignSoloLock(identity, mapLock);
            return mapLock;
        }

        public IMapLock AssignSoloLock(Identity identity, IMapLock mapLock)
        {
            mapLock.AddCharacer(identity);

            if (!soloLocks.TryGetValue(identity, out IMapLockCollection mapLockCollection))
            {
                mapLockCollection = mapLockCollectionFactory.Resolve();
                soloLocks[identity] = mapLockCollection;
            }

            IMapLock existing = mapLockCollection.GetMapLock<IMapLock>(mapLock.WorldId);
            if (existing == null)
                mapLockCollection.AddMapLock(mapLock);

            SaveSoloInstance(identity, mapLock);
            return mapLock;
        }

        /// <summary>
        /// Create a new match <see cref="IMapLock"/> for supplied <see cref="IMatch"/>.
        /// </summary>
        public IMapLock CreateMatchLock(IMatch match)
        {
            IMapLock mapLock = CreateLock<IMapLock>(MapLockType.Match, match.MatchingMap.GameMapEntry.WorldId);

            foreach (IMatchTeam matchTeam in match.GetTeams())
                foreach (IMatchTeamMember matchTeamMember in matchTeam.GetMembers())
                    mapLock.AddCharacer(matchTeamMember.Identity);

            if (!matchLocks.TryGetValue(match.Guid, out IMapLockCollection mapLockCollection))
            {
                mapLockCollection = mapLockCollectionFactory.Resolve();
                matchLocks[match.Guid] = mapLockCollection;
            }

            mapLockCollection.AddMapLock(mapLock);
            return mapLock;
        }

        private T CreateLock<T>(MapLockType lockType, uint worldId) where T : IMapLock
        {
            IMapLock mapLock = mapLockFactory.Resolve<T>();
            mapLock.Initialise(lockType, worldId);
            return (T)mapLock;
        }

        private T CreateLock<T>(MapLockType lockType, uint worldId, Guid instanceId) where T : IMapLock
        {
            IMapLock mapLock = mapLockFactory.Resolve<T>();
            mapLock.Initialise(lockType, worldId, instanceId);
            return (T)mapLock;
        }

        /// <summary>
        /// Return <see cref="IMapLock"/> for supplied character id and world id.
        /// </summary>
        public IMapLock GetSoloLock(Identity identity, uint worldId)
        {
            if (soloLocks.TryGetValue(identity, out IMapLockCollection mapLockCollection))
            {
                IMapLock mapLock = mapLockCollection.GetMapLock<IMapLock>(worldId);
                if (mapLock != null)
                    return mapLock;
            }

            return TryHydratePersistedSoloLock(identity, worldId);
        }

        /// <summary>
        /// Return <see cref="IMapLock"/> for supplied match guid and world id.
        /// </summary>
        public IMapLock GetMatchLock(Guid guid, uint worldId)
        {
            return matchLocks.TryGetValue(guid, out IMapLockCollection mapLockCollection)
                ? mapLockCollection.GetMapLock<IMapLock>(worldId)
                : null;
        }

        /// <summary>
        /// Return <see cref="IResidenceMapLock"/> for supplied <see cref="IResidence"/>.
        /// </summary>
        public IResidenceMapLock GetResidenceLock(IResidence residence)
        {
            if (residenceLocks.TryGetValue(residence.Id, out IResidenceMapLock mapLock))
                return mapLock;

            mapLock = CreateLock<IResidenceMapLock>(MapLockType.Residence, 0u);
            mapLock.Initialise(residence.Id);
            residenceLocks[residence.Id] = mapLock;
            return mapLock;
        }

        private static ulong ConvertGuidToInstanceId(Guid guid)
        {
            return BitConverter.ToUInt64(guid.ToByteArray(), 0);
        }

        private static Guid ConvertInstanceIdToGuid(ulong instanceId)
        {
            var bytes = new byte[16];
            BitConverter.GetBytes(instanceId).CopyTo(bytes, 0);
            return new Guid(bytes);
        }

        private void HydratePersistedSoloLocks(IPlayer player)
        {
            foreach (CharacterInstanceModel instance in player.InstanceManager.GetInstances())
            {
                if (instance.InstanceId == 0)
                    continue;

                TryHydratePersistedSoloLock(player.Identity, instance.WorldId);
            }
        }

        private IMapLock TryHydratePersistedSoloLock(Identity identity, uint worldId)
        {
            IPlayer player = playerManager.GetPlayer(identity);
            CharacterInstanceModel persisted = player?.InstanceManager.GetInstance((ushort)worldId);
            if (persisted == null || persisted.InstanceId == 0)
                return null;

            if (!soloLocks.TryGetValue(identity, out IMapLockCollection mapLockCollection))
            {
                mapLockCollection = mapLockCollectionFactory.Resolve();
                soloLocks[identity] = mapLockCollection;
            }

            IMapLock existing = mapLockCollection.GetMapLock<IMapLock>(worldId);
            if (existing != null)
                return existing;

            Guid persistedId = ConvertInstanceIdToGuid(persisted.InstanceId);
            IMapLock mapLock = CreateLock<IMapLock>(MapLockType.Solo, worldId, persistedId);
            mapLock.AddCharacer(identity);
            mapLockCollection.AddMapLock(mapLock);

            log.LogTrace("Hydrated persisted solo lock for {Identity} world {WorldId} instance {InstanceId}", identity, worldId, persistedId);
            return mapLock;
        }

        private void SaveSoloInstance(Identity identity, IMapLock mapLock)
        {
            IPlayer player = playerManager.GetPlayer(identity);
            if (player == null)
                return;

            ulong persistedId = ConvertGuidToInstanceId(mapLock.InstanceId);
            CharacterInstanceModel existing = player.InstanceManager.GetInstance((ushort)mapLock.WorldId);
            if (existing?.InstanceId == persistedId)
                return;

            player.InstanceManager.SetInstance(new CharacterInstanceModel
            {
                CharacterId   = player.CharacterId,
                WorldId       = (ushort)mapLock.WorldId,
                InstanceId    = persistedId,
                LockoutExpiry = ResolveSoloLockExpiry(existing),
                Difficulty    = existing?.Difficulty ?? 0,
                PrimeLevel    = existing?.PrimeLevel ?? 0,
                PositionX     = existing?.PositionX ?? 0f,
                PositionY     = existing?.PositionY ?? 0f,
                PositionZ     = existing?.PositionZ ?? 0f,
                Rotation      = existing?.Rotation ?? 0f
            });
        }

        private DateTime ResolveSoloLockExpiry(CharacterInstanceModel existing)
        {
            if (existing != null && existing.LockoutExpiry != default)
                return existing.LockoutExpiry;

            double? hours = SharedConfiguration.Instance.Get<MapConfig>().SoloInstanceLockExpiryHours;
            if (hours.HasValue && hours.Value > 0d)
                return DateTime.UtcNow.AddHours(hours.Value);

            return DateTime.MaxValue;
        }
    }
}
