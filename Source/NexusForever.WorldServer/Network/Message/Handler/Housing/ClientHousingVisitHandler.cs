using System;
using System.Linq;
using NexusForever.Game;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Map;
using NexusForever.Game.Static.Housing;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Housing
{
    public class ClientHousingVisitHandler : IMessageHandler<IWorldSession, ClientHousingVisit>
    {
        #region Dependency Injection

        private readonly IGlobalResidenceManager globalResidenceManager;
        private readonly IGlobalGuildManager globalGuildManager;
        private readonly IMapLockManager mapLockManager;

        public ClientHousingVisitHandler(
            IGlobalResidenceManager globalResidenceManager,
            IGlobalGuildManager globalGuildManager,
            IMapLockManager mapLockManager)
        {
            this.globalResidenceManager = globalResidenceManager;
            this.globalGuildManager     = globalGuildManager;
            this.mapLockManager         = mapLockManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientHousingVisit housingVisit)
        {
            if (session.Player.Map is not IResidenceMapInstance)
                throw new InvalidPacketValueException();

            if (!session.Player.CanTeleport())
                return;

            IResidence residence;
            if (!string.IsNullOrEmpty(housingVisit.TargetResidenceName))
                residence = globalResidenceManager.GetResidenceByOwner(housingVisit.TargetResidenceName);
            else if (!string.IsNullOrEmpty(housingVisit.TargetCommunityName))
                residence = globalResidenceManager.GetCommunityByOwner(housingVisit.TargetCommunityName);
            else if (housingVisit.TargetResidence.ResidenceId != 0ul)
                residence = globalResidenceManager.GetResidence(housingVisit.TargetResidence.ResidenceId);
            else if (housingVisit.TargetCommunity.NeighbourhoodId != 0ul)
            {
                ulong residenceId = globalGuildManager.GetGuild<ICommunity>(housingVisit.TargetCommunity.NeighbourhoodId)?.Residence?.Id ?? 0ul;
                residence = globalResidenceManager.GetResidence(residenceId);
            }
            else
                throw new InvalidPacketValueException();

            if (residence == null)
            {
                session.Player.Session.EnqueueMessageEncrypted(new ServerHousingResult
                {
                    Result = HousingResult.InvalidResidence
                });
                return;
            }

            switch (residence.PrivacyLevel)
            {
                case ResidencePrivacyLevel.Private:
                    session.Player.Session.EnqueueMessageEncrypted(new ServerHousingResult
                    {
                        ResidenceId = residence.Id,
                        Result      = HousingResult.Visit_Private
                    });
                    return;

                case ResidencePrivacyLevel.NeighborsOnly:
                {
                    // Allow if the visiting player is a neighbour of the residence owner.
                    IResidence visitorResidence = globalResidenceManager.GetResidenceByOwner(session.Player.CharacterId);
                    bool isNeighbour = visitorResidence != null
                        && residence.GetNeighbors().Any(n => n.ResidenceId == visitorResidence.Id && !n.IsPending);
                    if (!isNeighbour)
                    {
                        session.Player.Session.EnqueueMessageEncrypted(new ServerHousingResult
                        {
                            ResidenceId = residence.Id,
                            Result      = HousingResult.InvalidPermissions
                        });
                        return;
                    }
                    break;
                }

                case ResidencePrivacyLevel.RoommatesOnly:
                {
                    // Allow if the visiting player shares a community plot with the residence owner.
                    IResidence parent = residence.Parent ?? residence;
                    bool isRoommate = parent.GetChild(session.Player.CharacterId) != null;
                    if (!isRoommate)
                    {
                        session.Player.Session.EnqueueMessageEncrypted(new ServerHousingResult
                        {
                            ResidenceId = residence.Id,
                            Result      = HousingResult.InvalidPermissions
                        });
                        return;
                    }
                    break;
                }
            }

            IMapLock mapLock = mapLockManager.GetResidenceLock(residence.Parent ?? residence);

            // teleport player to correct residence instance
            IResidenceEntrance entrance = globalResidenceManager.GetResidenceEntrance(residence.PropertyInfoId);
            session.Player.Rotation = entrance.Rotation.ToEuler();
            session.Player.TeleportTo(new MapPosition
            {
                Info = new MapInfo
                {
                    Entry   = entrance.Entry,
                    MapLock = mapLock
                },
                Position = entrance.Position
            });
        }
    }
}
