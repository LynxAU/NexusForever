using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;

namespace NexusForever.Game.Entity
{
    public class InstancePortalEntity : WorldEntity, IInstancePortalEntity
    {
        public override EntityType Type => EntityType.InstancePortal;

        #region Dependency Injection

        private readonly IMatchingDataManager matchingDataManager;

        public InstancePortalEntity(
            IMovementManager movementManager,
            IMatchingDataManager matchingDataManager)
            : base(movementManager)
        {
            this.matchingDataManager = matchingDataManager;
        }

        #endregion

        protected override IEntityModel BuildEntityModel()
        {
            return new InstancePortalEntityModel
            {
                CreatureId = CreatureId
            };
        }

        public override void OnActivate(IPlayer activator)
        {
            if (WorldSocketId == 0)
                return;

            WorldSocketEntry socketEntry = GameTableManager.Instance.WorldSocket.GetEntry(WorldSocketId);
            if (socketEntry == null)
                return;

            if (!activator.CanTeleport())
                return;

            IMapEntrance mapEntrance = matchingDataManager.GetMapEntrance(socketEntry.WorldId, 0);
            if (mapEntrance == null)
                return;

            activator.Rotation = mapEntrance.Rotation;
            activator.TeleportTo(mapEntrance.MapId, mapEntrance.Position.X, mapEntrance.Position.Y, mapEntrance.Position.Z);
        }
    }
}
