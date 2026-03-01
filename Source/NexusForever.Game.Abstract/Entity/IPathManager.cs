using NexusForever.Database.Character;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Abstract.Entity
{
    public interface IPathManager : IDatabaseCharacter, IEnumerable<IPathEntry>
    {
        /// <summary>
        /// Checks to see if supplied <see cref="Static.Entity.Path"/> is active.
        /// </summary>
        bool IsPathActive(Static.Entity.Path pathToCheck);

        /// <summary>
        /// Attempts to activate supplied <see cref="Static.Entity.Path"/>. 
        /// </summary>
        void ActivatePath(Static.Entity.Path pathToActivate);

        /// <summary>
        /// Checks if supplied <see cref="Static.Entity.Path"/> is unlocked. 
        /// </summary>
        bool IsPathUnlocked(Static.Entity.Path pathToUnlock);

        /// <summary>
        /// Attemps to unlock supplied <see cref="Static.Entity.Path"/>.
        /// </summary>
        void UnlockPath(Static.Entity.Path pathToUnlock);

        /// <summary>
        /// Add XP to the current <see cref="Static.Entity.Path"/>.
        /// </summary>
        void AddXp(uint xp);
        void HandleSettlerBuildEvent(uint pathSettlerImprovementGroupId, uint buildTier);

        /// <summary>
        /// Notify the path manager that a creature was killed (used by Soldier Assassinate/SWAT missions).
        /// </summary>
        void HandleSoldierKillEvent(uint creatureId, IEnumerable<uint> targetGroupIds);

        /// <summary>
        /// Notify the path manager that an Explorer node was reached (sent by client).
        /// </summary>
        void HandleExplorerProgressReport(uint pathMissionId, uint nodeIndex);

        /// <summary>
        /// Handle a Scientist experimentation attempt (sent by client with pattern choices).
        /// </summary>
        void HandleScientistExperimentation(List<uint> choices);

        void SendInitialPackets();
        void SendSetUnitPathTypePacket();
        void SendServerPathActivateResult(GenericError result = GenericError.Ok);
        void SendServerPathUnlockResult(GenericError result = GenericError.Ok);
    }
}
