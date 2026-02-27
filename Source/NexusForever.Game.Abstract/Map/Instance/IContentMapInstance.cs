using NexusForever.Game.Abstract.Matching.Match;

namespace NexusForever.Game.Abstract.Map.Instance
{
    public interface IContentMapInstance : IMapInstance
    {
        /// <summary>
        /// Active <see cref="IMatch"/> for map.
        /// </summary>
        IMatch Match { get; }

        /// <summary>
        /// Add <see cref="IMatch"/> for map.
        /// </summary>
        void SetMatch(IMatch match);

        /// <summary>
        /// Remove <see cref="IMatch"/> for map.
        /// </summary>
        void RemoveMatch();

        /// <summary>
        /// Invoked when the <see cref="IMatch"/> for the map finishes.
        /// </summary>
        void OnMatchFinish();

        /// <summary>
        /// Notify the instance script that an NPC with the given creature id has died.
        /// Called by boss encounter scripts to advance encounter state.
        /// </summary>
        void TriggerBossDeath(uint creatureId);

        /// <summary>
        /// Notify the instance script that the encounter should reset.
        /// Called when all players leave or are removed from the instance.
        /// </summary>
        void TriggerEncounterReset();
    }
}
