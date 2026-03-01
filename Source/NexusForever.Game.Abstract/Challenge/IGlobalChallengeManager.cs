using NexusForever.Game.Static.Challenges;

namespace NexusForever.Game.Abstract.Challenge
{
    public interface IGlobalChallengeManager
    {
        void Initialise();

        /// <summary>
        /// Return <see cref="IChallengeInfo"/> for the supplied challenge id.
        /// </summary>
        IChallengeInfo GetChallengeInfo(uint challengeId);

        /// <summary>
        /// Return all Combat-type <see cref="IChallengeInfo"/> entries whose target matches the supplied creature id.
        /// </summary>
        IEnumerable<IChallengeInfo> GetCombatChallengesForTarget(uint creatureId);

        /// <summary>
        /// Return all <see cref="IChallengeInfo"/> entries for the supplied <see cref="ChallengeType"/>.
        /// </summary>
        IEnumerable<IChallengeInfo> GetChallengesByType(ChallengeType type);
    }
}
