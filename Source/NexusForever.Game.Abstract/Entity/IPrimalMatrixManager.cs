using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Abstract.Entity
{
    public interface IPrimalMatrixManager : IDatabaseCharacter
    {
        /// <summary>
        /// Load Primal Matrix data from database model.
        /// </summary>
        void Load(CharacterModel model);

        /// <summary>
        /// Add essence to player's Primal Matrix.
        /// </summary>
        void AddEssence(uint essenceId, uint amount);

        /// <summary>
        /// Get the amount of a specific essence in the matrix.
        /// </summary>
        uint GetEssenceAmount(uint essenceId);

        /// <summary>
        /// Activate a primal matrix node for the player.
        /// </summary>
        bool ActivateNode(uint nodeId);

        /// <summary>
        /// Send initial primal matrix packets to client.
        /// </summary>
        void SendInitialPackets();
    }
}
