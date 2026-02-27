using NexusForever.Database.Character;

namespace NexusForever.Game.Abstract.Entity
{
    public interface IPrimalMatrixManager : IDatabaseCharacter
    {
        /// <summary>
        /// Add essence to player's Primal Matrix.
        /// </summary>
        void AddEssence(uint essenceId, uint amount);

        /// <summary>
        /// Get the amount of a specific essence in the matrix.
        /// </summary>
        uint GetEssenceAmount(uint essenceId);
    }
}
