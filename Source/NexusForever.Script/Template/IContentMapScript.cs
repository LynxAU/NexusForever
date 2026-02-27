using NexusForever.Game.Abstract.Matching.Match;

namespace NexusForever.Script.Template
{
    public interface IContentMapScript : IInstancedMapScript
    {
        /// <summary>
        /// Invoked when the <see cref="IMatch"/> for the map finishes.
        /// </summary>
        /// <remarks>
        /// This is invoked for all match types, for PvP matches this is invoked in addition to <see cref="IContentPvpMapScript.OnPvpMatchFinish(Game.Static.Matching.MatchWinner, Game.Static.Matching.MatchEndReason)"/>.
        /// </remarks>
        void OnMatchFinish()
        {
        }

        /// <summary>
        /// Invoked when a tracked NPC with the specified creature id dies inside this instance.
        /// Use this to advance encounter state or trigger completion.
        /// </summary>
        void OnBossDeath(uint creatureId)
        {
        }

        /// <summary>
        /// Invoked when all players have left or died and the encounter should be reset to its initial state.
        /// </summary>
        void OnEncounterReset()
        {
        }
    }
}
