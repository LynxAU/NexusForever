using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Abstract.Spell
{
    public interface IDamageDescription
    {
        DamageType DamageType { get; set; }
        uint RawDamage { get; set; }
        uint RawScaledDamage { get; set; }
        uint AbsorbedAmount { get; set; }
        uint ShieldAbsorbAmount { get; set; }
        uint AdjustedDamage { get; set; }
        uint GlanceAmount { get; set; }
        uint MultiHitAmount { get; set; }
        uint OverkillAmount { get; set; }
        bool KilledTarget { get; set; }
        CombatResult CombatResult { get; set; }

        /// <summary>
        /// Damage reflected back to the attacker during this hit.
        /// Populated by the damage calculator when the victim has reflect stats.
        /// </summary>
        uint ReflectedDamage { get; set; }

        /// <summary>
        /// Separate multi-hit bonus damage amount, stored independently from the base hit
        /// so the caller can emit a dedicated <c>CombatLogMultiHit</c> entry.
        /// </summary>
        uint MultiHitDamage { get; set; }
    }
}
