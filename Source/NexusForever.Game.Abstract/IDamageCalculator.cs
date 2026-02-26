using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;

namespace NexusForever.Game.Abstract
{
    public interface IDamageCalculator
    {
        void CalculateDamage(IUnitEntity attacker, IUnitEntity victim, ISpell spell, ISpellTargetEffectInfo info);

        /// <summary>
        /// Calculate a raw melee auto-attack hit from <paramref name="attacker"/> against <paramref name="victim"/>.
        /// Uses AssaultRating as the damage base and runs the standard deflect, crit, glance, armor, and shield pipeline.
        /// Returns null if the attack was deflected.
        /// </summary>
        IDamageDescription CalculateMeleeDamage(IUnitEntity attacker, IUnitEntity victim);
    }
}
