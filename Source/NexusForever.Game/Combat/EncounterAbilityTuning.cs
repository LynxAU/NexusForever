using System;
using System.Collections.Generic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Combat
{
    /// <summary>
    /// Data-driven encounter spell hit baselines derived from WildStarLogs P75 samples.
    /// Applies to scripted NPC encounter casts only (NPC -> player).
    /// </summary>
    internal static class EncounterAbilityTuning
    {
        // Spell4Id -> target hit amount (post-mitigation approximation).
        private static readonly Dictionary<uint, uint> TargetHitBySpell4Id = new()
        {
            // e410 - Experiment X-89
            [47351u] = 56603u,   // Repugnant Spew
            [47271u] = 75450u,   // Shattering Shockwave
            [47279u] = 49650u,   // Resounding Shout
            [47316u] = 67400u,   // Corruption Globule
            [47285u] = 48175u,   // Strain Bomb

            // e411 - Phage Maw
            [56517u] = 54175u,   // Bombardment
            [60437u] = 22183u,   // Laser Blast
            [60612u] = 59700u,   // Detonation Bombs

            // e412 - Kuralak the Defiler
            [57729u] = 40975u,   // Vileness
            [56649u] = 18400u,   // Chromosome Corruption
            [57837u] = 6025u,    // Putrid Discharge
            [56589u] = 23300u,   // DNA Siphon
            [60366u] = 112200u,  // Tainted Ventilation
            [60623u] = 70000u,   // Outbreak

            // e414 - Phageborn Convergence
            [58423u] = 23500u,   // Essence Rot
            [60399u] = 29800u,   // Piercing Vision
            [56983u] = 53500u,   // Foul Scourge

            // e415 - Dreadphage Ohmna
            [59632u] = 22600u,   // Ravage
            [47359u] = 62775u,   // Body Slam T1
            [72662u] = 62300u,   // Body Slam T2
            [75717u] = 64075u,   // Body Slam T3
            [47361u] = 37625u,   // Erupt T1
            [59764u] = 52200u,   // Erupt T2
            [72661u] = 43200u,   // Erupt T3
            [47364u] = 55600u,   // Genetic Torrent
            [47745u] = 9500u,    // Sap Power

            // Act minibosses
            [61341u] = 51375u,   // Foul Rupture
            [69962u] = 68875u,   // Noxious Belch
            [61357u] = 74375u,   // Repulsive Cultivation
            [61378u] = 48500u,   // Radiation Bath
            [62652u] = 93700u,   // Gravity Crush
            [62731u] = 68125u,   // Gravity Spike
            [70448u] = 160500u,  // Gravity Flux
            [62736u] = 63575u,   // Gravity Swell
            [62936u] = 17250u,   // Cellular Leech
            [62946u] = 22600u,   // Ravage
            [62947u] = 39050u,   // Genetic Decomposition

            // Guardian / generator minibosses
            [61418u] = 27675u,   // Hookshot
            [61419u] = 19750u,   // Eradicate
            [62691u] = 39725u,   // Magnetized Cell
            [62683u] = 51850u,   // Voltaic Storm
            [70241u] = 22600u,   // Power Siphon
            [61322u] = 29450u,   // Direct Current
            [62890u] = 21375u,   // Arc Thrower
            [61340u] = 49100u,   // Static Discharge
            [70111u] = 67625u,   // Static Shock
            [62556u] = 46250u,   // Dash
            [62554u] = 19400u,   // Electrical Current
            [62568u] = 43450u,   // Shear Casing
            [70948u] = 50275u,   // Corrosive Fluid
            [61496u] = 86800u    // Repeat
        };

        // Per-hit jitter so combat does not feel static.
        private const double LowerRoll = 0.92d;
        private const double UpperRoll = 1.08d;

        public static bool TryResolveTunedDamage(IUnitEntity attacker, IUnitEntity victim, ISpell spell, out uint tunedDamage)
        {
            tunedDamage = 0u;

            if (attacker == null || victim == null || spell?.Parameters?.SpellInfo?.Entry == null)
                return false;

            if (attacker.Type == EntityType.Player || victim.Type != EntityType.Player)
                return false;

            uint spell4Id = spell.Parameters.SpellInfo.Entry.Id;
            if (!TargetHitBySpell4Id.TryGetValue(spell4Id, out uint targetHit))
                return false;

            double roll = LowerRoll + (Random.Shared.NextDouble() * (UpperRoll - LowerRoll));
            double tuned = targetHit * roll;
            tunedDamage = (uint)Math.Max(1d, Math.Round(tuned));
            return true;
        }
    }
}
