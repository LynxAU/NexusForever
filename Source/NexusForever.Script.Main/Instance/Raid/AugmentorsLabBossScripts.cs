using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Raid
{
    // Augmentors' Lab (WorldId 3040) — Boss Encounter Scripts
    // Source: Spell4.tbl "[IC]" (Infinite Crimelabs) name tag search.
    //
    // All bosses belong to encounter e2681 — Augmentors. This is a multi-phase
    // encounter with strain/corruption mechanics.
    //
    // All spell IDs sourced from Spell4.tbl "[IC] e2681" tagged entries.
    // Rotation intervals are best-effort approximations; tune from sniff data once available.

    // ── Augmenters God Unit (Final Boss) ─────────────────────────────────────────
    //
    // Large construct with laser and fallout AoE mechanics.
    //   48699 | cast | Augmentation Beam - Base
    //   75258 | cast | Laser - Beam Cast
    //   75291 | cast | Laser - Spin
    //   75194 | cast | Fallout - Cast Base
    //   79895 | cast | Reinforcement
    //   80412 | cast | System Reboot

    /// <summary>Augmenters God Unit — final boss. Creature2Id 50979.</summary>
    [ScriptFilterCreatureId(50979u)]
    public class ICAugmentersGodUnitScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 48699, initialDelay:  3.0, interval:  8.0);  // Augmentation Beam
            ScheduleSpell(spell4Id: 75258, initialDelay:  8.0, interval: 14.0);  // Laser Beam
            ScheduleSpell(spell4Id: 75194, initialDelay: 15.0, interval: 25.0);  // Fallout
            ScheduleSpell(spell4Id: 79895, initialDelay: 25.0, interval: 40.0);  // Reinforcement

            AddPhase(healthPct: 40f, OnCriticalPhase);

            SetEnrage(seconds: 600.0, enrageSpellId: 75291); // Laser Spin
        }

        private void OnCriticalPhase()
        {
            ScheduleSpell(spell4Id: 75291, initialDelay: 3.0, interval: 20.0);  // Laser Spin
            ScheduleSpell(spell4Id: 80412, initialDelay: 8.0, interval: 45.0);  // System Reboot
        }
    }

    // ── Prime Evolutionary Operant ──────────────────────────────────────────────
    //
    // Strain-infused operant with radiation and injection abilities.
    //   48599 | cast | Strain Injection - Base
    //   71164 | cast | Radiation Bath - Base
    //   75326 | cast | Spread - Base
    //   50075 | cast | Nanostrain Infusion - Base

    /// <summary>Prime Evolutionary Operant. Creature2Id 50472.</summary>
    [ScriptFilterCreatureId(50472u)]
    public class ICPrimeEvolutionaryOperantScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 48599, initialDelay:  3.0, interval: 10.0); // Strain Injection
            ScheduleSpell(spell4Id: 71164, initialDelay:  8.0, interval: 16.0); // Radiation Bath
            ScheduleSpell(spell4Id: 50075, initialDelay: 12.0, interval: 20.0); // Nanostrain Infusion
            ScheduleSpell(spell4Id: 75326, initialDelay: 20.0, interval: 30.0); // Spread

            SetEnrage(seconds: 600.0, enrageSpellId: 71164); // Radiation Bath
        }
    }

    // ── Phaged Evolutionary Operant ─────────────────────────────────────────────
    //
    // Corruption-focused operant with spike and augmenter-corruption abilities.
    //   71282 | cast | Corruption Spike - Base
    //   48735 | cast | Corrupt Augmenter - Base
    //   71262 | cast | Circuit Corruption
    //   49303 | cast | Strain Incubation - Base

    /// <summary>Phaged Evolutionary Operant. Creature2Id 50423.</summary>
    [ScriptFilterCreatureId(50423u)]
    public class ICPhagedEvolutionaryOperantScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 71282, initialDelay:  3.0, interval: 10.0); // Corruption Spike
            ScheduleSpell(spell4Id: 48735, initialDelay:  8.0, interval: 18.0); // Corrupt Augmenter
            ScheduleSpell(spell4Id: 49303, initialDelay: 14.0, interval: 22.0); // Strain Incubation
            ScheduleSpell(spell4Id: 71262, initialDelay: 22.0, interval: 30.0); // Circuit Corruption

            SetEnrage(seconds: 600.0, enrageSpellId: 71282); // Corruption Spike
        }
    }

    // ── Chestacabra ─────────────────────────────────────────────────────────────
    //
    // Strain-spawned creature with burst and bite abilities.
    //   48608 | cast | Chest Burst - Base
    //   50077 | cast | Nano Bite - Base
    //   71423 | cast | Ruptured Insides - Base

    /// <summary>Chestacabra. Creature2Id 50425.</summary>
    [ScriptFilterCreatureId(50425u)]
    public class ICChestacabraScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 50077, initialDelay: 2.0, interval:  6.0); // Nano Bite
            ScheduleSpell(spell4Id: 48608, initialDelay: 5.0, interval: 14.0); // Chest Burst
            ScheduleSpell(spell4Id: 71423, initialDelay: 10.0, interval: 18.0); // Ruptured Insides

            SetEnrage(seconds: 600.0, enrageSpellId: 48608); // Chest Burst
        }
    }

    // ── Circuit Breaker ─────────────────────────────────────────────────────────
    //
    // Mechanical boss with transmission and pull mechanics.
    //   71356 | cast | Transmission
    //   71470 | cast | Transmission - Pull
    //   71286 | cast | Loose Change - Base (borrowed from shared pool)

    /// <summary>Circuit Breaker. Creature2Id 61597.</summary>
    [ScriptFilterCreatureId(61597u)]
    public class ICCircuitBreakerScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 71356, initialDelay: 3.0, interval: 12.0); // Transmission
            ScheduleSpell(spell4Id: 71470, initialDelay: 8.0, interval: 18.0); // Transmission Pull

            SetEnrage(seconds: 600.0, enrageSpellId: 71356); // Transmission
        }
    }
}
