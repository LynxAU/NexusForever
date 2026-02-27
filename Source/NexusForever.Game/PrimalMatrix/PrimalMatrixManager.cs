using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NLog;

namespace NexusForever.Game.PrimalMatrix
{
    /// <summary>
    /// Manages the player's Primal Matrix - a progression system for earning essence rewards.
    /// </summary>
    public class PrimalMatrixManager : IPrimalMatrixManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private readonly IPlayer player;
        private readonly Dictionary<uint, uint> essences = new();

        public PrimalMatrixManager(IPlayer player)
        {
            this.player = player;
        }

        /// <summary>
        /// Add essence to player's Primal Matrix.
        /// </summary>
        public void AddEssence(uint essenceId, uint amount)
        {
            if (essenceId == 0 || amount == 0)
                return;

            // Validate the essence ID against game tables
            PrimalMatrixRewardEntry entry = GameTableManager.Instance.PrimalMatrixReward.GetEntry(essenceId);
            if (entry == null)
            {
                log.Warn($"Player {player.CharacterId} received invalid essence {essenceId}");
                return;
            }

            essences.TryGetValue(essenceId, out uint currentAmount);
            essences[essenceId] = currentAmount + amount;

            log.Trace($"Player {player.CharacterId} received {amount} essence {essenceId}, total now {essences[essenceId]}");

            // TODO: Send network packet to client to update matrix UI
            // TODO: Check for any unlock thresholds
        }

        /// <summary>
        /// Get the amount of a specific essence in the matrix.
        /// </summary>
        public uint GetEssenceAmount(uint essenceId)
        {
            return essences.TryGetValue(essenceId, out uint val) ? val : 0u;
        }

        public void Save(CharacterContext context)
        {
            // TODO: Implement database persistence when CharacterPrimalMatrix table is added
            // This would save the essences dictionary to the database
        }
    }
}
