using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    // Skullcano (WorldId 1263) — Boss Encounter Scripts
    // Spell IDs sourced from Jabbithole (zone 20).

    // ── Stew-Shaman Tugga ───────────────────────────────────────────────────────
    //   63344 | Molten Rain   — AoE fire damage
    //   36172 | Blast Force   — melee knockback
    //   72508 | Burning       — fire DoT
    //   36097 | Fiery Bolt    — ranged fire attack

    public abstract class SkullcanoTuggaBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 36172, initialDelay:  2.0, interval:  6.0); // Blast Force
            ScheduleSpell(spell4Id: 36097, initialDelay:  4.0, interval:  8.0); // Fiery Bolt
            ScheduleSpell(spell4Id: 72508, initialDelay:  7.0, interval: 12.0); // Burning
            ScheduleSpell(spell4Id: 63344, initialDelay: 12.0, interval: 18.0); // Molten Rain

            AddPhase(healthPct: 40f, OnRagePhase);
            SetEnrage(seconds: 420.0, enrageSpellId: 63344);
        }

        private void OnRagePhase()
        {
            ScheduleSpell(spell4Id: 63344, initialDelay: 3.0, interval: 12.0); // Molten Rain (faster)
        }
    }

    /// <summary>Stew-Shaman Tugga — Normal. Creature2Id 24493.</summary>
    [ScriptFilterCreatureId(24493u)]
    public class SkullcanoTuggaNScript : SkullcanoTuggaBase { }

    /// <summary>Stew-Shaman Tugga — Veteran. Creature2Id 24898.</summary>
    [ScriptFilterCreatureId(24898u)]
    public class SkullcanoTuggaVScript : SkullcanoTuggaBase { }

    // ── Thunderfoot ─────────────────────────────────────────────────────────────
    //   36437 | Seismic Tremor  — large ground AoE
    //   36309 | Swipe           — frontal cleave
    //   40909 | Thunder Pound   — heavy ground slam
    //   36283 | Pulverize       — targeted heavy hit

    public abstract class SkullcanoThunderfootBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 36309, initialDelay:  2.0, interval:  5.0); // Swipe
            ScheduleSpell(spell4Id: 36283, initialDelay:  5.0, interval: 10.0); // Pulverize
            ScheduleSpell(spell4Id: 40909, initialDelay:  9.0, interval: 14.0); // Thunder Pound
            ScheduleSpell(spell4Id: 36437, initialDelay: 15.0, interval: 20.0); // Seismic Tremor

            AddPhase(healthPct: 35f, OnStompPhase);
            SetEnrage(seconds: 420.0, enrageSpellId: 36437);
        }

        private void OnStompPhase()
        {
            ScheduleSpell(spell4Id: 40909, initialDelay: 2.0, interval: 10.0); // Thunder Pound (faster)
            ScheduleSpell(spell4Id: 36437, initialDelay: 5.0, interval: 14.0); // Seismic Tremor (faster)
        }
    }

    /// <summary>Thunderfoot — Normal. Creature2Id 24475.</summary>
    [ScriptFilterCreatureId(24475u)]
    public class SkullcanoThunderfootNScript : SkullcanoThunderfootBase { }

    /// <summary>Thunderfoot — Veteran. Creature2Id 24893.</summary>
    [ScriptFilterCreatureId(24893u)]
    public class SkullcanoThunderfootVScript : SkullcanoThunderfootBase { }

    // ── Bosun Octog ─────────────────────────────────────────────────────────────
    //   41850 | Shred           — melee damage
    //   36614 | Noxious Ink     — poison AoE / DoT
    //   41443 | Scab Stab       — targeted stab
    //   31283 | Hookshot        — pull / gap-closer
    //   41455 | Monkey Drop Pod — summons monkey adds

    public abstract class SkullcanoOctogBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 41850, initialDelay:  2.0, interval:  5.0); // Shred
            ScheduleSpell(spell4Id: 41443, initialDelay:  4.0, interval:  8.0); // Scab Stab
            ScheduleSpell(spell4Id: 36614, initialDelay:  7.0, interval: 12.0); // Noxious Ink
            ScheduleSpell(spell4Id: 31283, initialDelay: 12.0, interval: 18.0); // Hookshot
            ScheduleSpell(spell4Id: 41455, initialDelay: 20.0, interval: 30.0); // Monkey Drop Pod

            AddPhase(healthPct: 50f, OnPhase2);
            SetEnrage(seconds: 420.0, enrageSpellId: 36614);
        }

        private void OnPhase2()
        {
            ScheduleSpell(spell4Id: 41455, initialDelay: 3.0, interval: 22.0); // Monkey Drop Pod (faster)
        }
    }

    /// <summary>Bosun Octog — Normal. Creature2Id 24486.</summary>
    [ScriptFilterCreatureId(24486u)]
    public class SkullcanoOctogNScript : SkullcanoOctogBase { }

    /// <summary>Bosun Octog — Veteran. Creature2Id 24894.</summary>
    [ScriptFilterCreatureId(24894u)]
    public class SkullcanoOctogVScript : SkullcanoOctogBase { }

    // ── Quartermaster Gruh ──────────────────────────────────────────────────────
    //   70210 | Shot                — primary ranged auto
    //   70211 | Blast               — ranged burst
    //   41899 | Dead Eye            — targeted heavy shot
    //   41901 | Kneecap             — CC / snare
    //   63677 | Fatal Shot          — execute-style heavy hit
    //   70217 | Debilitating Barrage — channeled multi-hit

    public abstract class SkullcanoGruhBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 70210, initialDelay:  2.0, interval:  5.0); // Shot
            ScheduleSpell(spell4Id: 70211, initialDelay:  4.0, interval:  8.0); // Blast
            ScheduleSpell(spell4Id: 41899, initialDelay:  7.0, interval: 12.0); // Dead Eye
            ScheduleSpell(spell4Id: 41901, initialDelay: 10.0, interval: 16.0); // Kneecap
            ScheduleSpell(spell4Id: 63677, initialDelay: 15.0, interval: 20.0); // Fatal Shot
            ScheduleSpell(spell4Id: 70217, initialDelay: 20.0, interval: 24.0); // Debilitating Barrage

            AddPhase(healthPct: 40f, OnDesperatePhase);
            SetEnrage(seconds: 420.0, enrageSpellId: 63677);
        }

        private void OnDesperatePhase()
        {
            ScheduleSpell(spell4Id: 63677, initialDelay: 3.0, interval: 14.0); // Fatal Shot (faster)
            ScheduleSpell(spell4Id: 70217, initialDelay: 6.0, interval: 18.0); // Debilitating Barrage (faster)
        }
    }

    /// <summary>Quartermaster Gruh — Normal. Creature2Id 24490.</summary>
    [ScriptFilterCreatureId(24490u)]
    public class SkullcanoGruhNScript : SkullcanoGruhBase { }

    /// <summary>Quartermaster Gruh — Veteran. Creature2Id 24896.</summary>
    [ScriptFilterCreatureId(24896u)]
    public class SkullcanoGruhVScript : SkullcanoGruhBase { }

    // ── Mordechai Redmoon (Final Boss) ──────────────────────────────────────────
    //   38458 | Cross Shot       — primary ranged auto
    //   39481 | Pin              — CC / root
    //   39493 | Turret Grenade   — ground-targeted AoE
    //   38461 | Vicious Barrage  — channeled multi-hit
    //   39491 | Big Bang         — heavy AoE explosion

    public abstract class SkullcanoRedmoonBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 38458, initialDelay:  2.0, interval:  5.0); // Cross Shot
            ScheduleSpell(spell4Id: 39481, initialDelay:  6.0, interval: 14.0); // Pin
            ScheduleSpell(spell4Id: 39493, initialDelay: 10.0, interval: 16.0); // Turret Grenade
            ScheduleSpell(spell4Id: 38461, initialDelay: 16.0, interval: 20.0); // Vicious Barrage
            ScheduleSpell(spell4Id: 39491, initialDelay: 24.0, interval: 28.0); // Big Bang

            AddPhase(healthPct: 50f, OnPhase2);
            AddPhase(healthPct: 20f, OnFinalStand);

            SetEnrage(seconds: 480.0, enrageSpellId: 39491);
        }

        private void OnPhase2()
        {
            ScheduleSpell(spell4Id: 39491, initialDelay: 3.0, interval: 22.0); // Big Bang (faster)
            ScheduleSpell(spell4Id: 39493, initialDelay: 6.0, interval: 12.0); // Turret Grenade (faster)
        }

        private void OnFinalStand()
        {
            ScheduleSpell(spell4Id: 38461, initialDelay: 2.0, interval: 14.0); // Vicious Barrage (rapid)
            ScheduleSpell(spell4Id: 39491, initialDelay: 5.0, interval: 16.0); // Big Bang (rapid)
        }
    }

    /// <summary>Mordechai Redmoon — final boss, Normal. Creature2Id 24489.</summary>
    [ScriptFilterCreatureId(24489u)]
    public class SkullcanoRedmoonNScript : SkullcanoRedmoonBase { }

    /// <summary>Mordechai Redmoon — final boss, Veteran. Creature2Id 24895.</summary>
    [ScriptFilterCreatureId(24895u)]
    public class SkullcanoRedmoonVScript : SkullcanoRedmoonBase { }
}
