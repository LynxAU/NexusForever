using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    // Sanctuary of the Swordmaiden (TorineDungeon, WorldId 1271) — Boss Encounter Scripts
    // Spell IDs sourced from Jabbithole (zone 85).

    // ── Deadringer Shallaos ───────────────────────────────────────────────────────
    //   42008 | Impetuous Rend     — melee hit
    //   42263 | Echo               — AoE echo damage
    //   42345 | Sonic Barrier      — defensive shield
    //   42104 | Phonic Concordance — heavy AoE burst

    public abstract class SotSMShallaosBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 42008, initialDelay:  2.0, interval:  5.0); // Impetuous Rend
            ScheduleSpell(spell4Id: 42263, initialDelay:  5.0, interval: 10.0); // Echo
            ScheduleSpell(spell4Id: 42345, initialDelay: 10.0, interval: 18.0); // Sonic Barrier
            ScheduleSpell(spell4Id: 42104, initialDelay: 16.0, interval: 22.0); // Phonic Concordance

            AddPhase(healthPct: 40f, OnResonance);
            SetEnrage(seconds: 420.0, enrageSpellId: 42104);
        }

        private void OnResonance()
        {
            ScheduleSpell(spell4Id: 42104, initialDelay: 3.0, interval: 16.0); // Phonic Concordance (faster)
            ScheduleSpell(spell4Id: 42263, initialDelay: 6.0, interval:  8.0); // Echo (faster)
        }
    }

    /// <summary>Deadringer Shallaos — Normal. Creature2Id 28600.</summary>
    [ScriptFilterCreatureId(28600u)]
    public class SotSMShallaosNScript : SotSMShallaosBase { }

    /// <summary>Deadringer Shallaos — Veteran. Creature2Id 28599.</summary>
    [ScriptFilterCreatureId(28599u)]
    public class SotSMShallaosVScript : SotSMShallaosBase { }

    // ── Ondu Lifeweaver ───────────────────────────────────────────────────────────
    //   34236 | Swipe             — frontal cleave
    //   43372 | Corrupting Shout  — AoE shout
    //   42420 | Plague Smash      — heavy AoE ground slam

    public abstract class SotSMLifeweaverBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 34236, initialDelay:  2.0, interval:  5.0); // Swipe
            ScheduleSpell(spell4Id: 43372, initialDelay:  6.0, interval: 12.0); // Corrupting Shout
            ScheduleSpell(spell4Id: 42420, initialDelay: 10.0, interval: 16.0); // Plague Smash

            AddPhase(healthPct: 45f, OnCorruption);
            SetEnrage(seconds: 420.0, enrageSpellId: 42420);
        }

        private void OnCorruption()
        {
            ScheduleSpell(spell4Id: 42420, initialDelay: 3.0, interval: 12.0); // Plague Smash (faster)
            ScheduleSpell(spell4Id: 43372, initialDelay: 5.0, interval:  9.0); // Corrupting Shout (faster)
        }
    }

    /// <summary>Ondu Lifeweaver — Normal. Creature2Id 28721.</summary>
    [ScriptFilterCreatureId(28721u)]
    public class SotSMLifeweaverNScript : SotSMLifeweaverBase { }

    /// <summary>Ondu Lifeweaver — Veteran. Creature2Id 28720.</summary>
    [ScriptFilterCreatureId(28720u)]
    public class SotSMLifeweaverVScript : SotSMLifeweaverBase { }

    // ── Moldwood Overlord Skash ───────────────────────────────────────────────────
    //   42063 | Smash                — primary melee hit
    //   42064 | Crush                — heavy melee
    //   42070 | Tentacle Wrath       — tentacle attack
    //   42149 | Corruption Heartseeker — targeted ranged
    //   63476 | Corrupting Revolver  — rotating AoE telegraph

    public abstract class SotSMSkashBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 42063, initialDelay:  2.0, interval:  5.0); // Smash
            ScheduleSpell(spell4Id: 42064, initialDelay:  4.0, interval:  8.0); // Crush
            ScheduleSpell(spell4Id: 42070, initialDelay:  8.0, interval: 14.0); // Tentacle Wrath
            ScheduleSpell(spell4Id: 42149, initialDelay: 12.0, interval: 18.0); // Corruption Heartseeker
            ScheduleSpell(spell4Id: 63476, initialDelay: 18.0, interval: 24.0); // Corrupting Revolver

            AddPhase(healthPct: 50f, OnTentaclePhase);
            AddPhase(healthPct: 20f, OnFrenzy);

            SetEnrage(seconds: 480.0, enrageSpellId: 63476);
        }

        private void OnTentaclePhase()
        {
            ScheduleSpell(spell4Id: 42070, initialDelay: 3.0, interval: 10.0); // Tentacle Wrath (faster)
            ScheduleSpell(spell4Id: 63476, initialDelay: 6.0, interval: 18.0); // Corrupting Revolver (faster)
        }

        private void OnFrenzy()
        {
            ScheduleSpell(spell4Id: 42070, initialDelay: 2.0, interval:  8.0); // Tentacle Wrath (rapid)
            ScheduleSpell(spell4Id: 42149, initialDelay: 4.0, interval: 12.0); // Corruption Heartseeker (rapid)
        }
    }

    /// <summary>Moldwood Overlord Skash — Normal. Creature2Id 28727.</summary>
    [ScriptFilterCreatureId(28727u)]
    public class SotSMSkashNScript : SotSMSkashBase { }

    /// <summary>Moldwood Overlord Skash — Veteran. Creature2Id 28728.</summary>
    [ScriptFilterCreatureId(28728u)]
    public class SotSMSkashVScript : SotSMSkashBase { }

    // ── Rayna Darkspeaker ─────────────────────────────────────────────────────────
    //   42051 | Fireball       — ranged fire attack
    //   42059 | Raging Lava    — ground AoE lava pool
    //   42125 | Flame Geyser   — eruption AoE
    //   42322 | Smoldering     — fire DoT
    //   54394 | Molten Wave    — wave telegraph

    public abstract class SotSMDarkspeakerBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 42051, initialDelay:  2.0, interval:  5.0); // Fireball
            ScheduleSpell(spell4Id: 42322, initialDelay:  5.0, interval: 10.0); // Smoldering
            ScheduleSpell(spell4Id: 42059, initialDelay:  8.0, interval: 14.0); // Raging Lava
            ScheduleSpell(spell4Id: 42125, initialDelay: 13.0, interval: 18.0); // Flame Geyser
            ScheduleSpell(spell4Id: 54394, initialDelay: 20.0, interval: 24.0); // Molten Wave

            AddPhase(healthPct: 50f, OnInferno);
            AddPhase(healthPct: 20f, OnMeltdown);

            SetEnrage(seconds: 480.0, enrageSpellId: 54394);
        }

        private void OnInferno()
        {
            ScheduleSpell(spell4Id: 42125, initialDelay: 3.0, interval: 12.0); // Flame Geyser (faster)
            ScheduleSpell(spell4Id: 54394, initialDelay: 6.0, interval: 18.0); // Molten Wave (faster)
        }

        private void OnMeltdown()
        {
            ScheduleSpell(spell4Id: 54394, initialDelay: 2.0, interval: 14.0); // Molten Wave (rapid)
            ScheduleSpell(spell4Id: 42059, initialDelay: 4.0, interval: 10.0); // Raging Lava (rapid)
        }
    }

    /// <summary>Rayna Darkspeaker — Normal. Creature2Id 28733.</summary>
    [ScriptFilterCreatureId(28733u)]
    public class SotSMDarkspeakerNScript : SotSMDarkspeakerBase { }

    /// <summary>Rayna Darkspeaker — Veteran. Creature2Id 28732.</summary>
    [ScriptFilterCreatureId(28732u)]
    public class SotSMDarkspeakerVScript : SotSMDarkspeakerBase { }

    // ── Spiritmother Selene the Corrupted (Final Boss) ────────────────────────────
    //   42378 | Malevolent Slash   — primary melee
    //   42379 | Vicious Rend       — heavy melee
    //   42434 | Creeping Shadows   — shadow AoE
    //   42290 | Shade Prison       — CC — imprisons target
    //   42540 | Blackout           — darkness AoE burst
    //   42675 | Lights Out!        — room-wide darkness mechanic
    //   63437 | Nightmare Ripple   — ripple wave telegraph

    public abstract class SotSMSeleneBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 42378, initialDelay:  2.0, interval:  5.0); // Malevolent Slash
            ScheduleSpell(spell4Id: 42379, initialDelay:  4.0, interval:  8.0); // Vicious Rend
            ScheduleSpell(spell4Id: 42434, initialDelay:  8.0, interval: 14.0); // Creeping Shadows
            ScheduleSpell(spell4Id: 42290, initialDelay: 12.0, interval: 20.0); // Shade Prison
            ScheduleSpell(spell4Id: 42540, initialDelay: 18.0, interval: 24.0); // Blackout
            ScheduleSpell(spell4Id: 63437, initialDelay: 22.0, interval: 26.0); // Nightmare Ripple
            ScheduleSpell(spell4Id: 42675, initialDelay: 30.0, interval: 35.0); // Lights Out!

            AddPhase(healthPct: 60f, OnShadowPhase);
            AddPhase(healthPct: 25f, OnDarkness);

            SetEnrage(seconds: 540.0, enrageSpellId: 42675);
        }

        private void OnShadowPhase()
        {
            ScheduleSpell(spell4Id: 42540, initialDelay: 3.0, interval: 18.0); // Blackout (faster)
            ScheduleSpell(spell4Id: 63437, initialDelay: 6.0, interval: 20.0); // Nightmare Ripple (faster)
            ScheduleSpell(spell4Id: 42675, initialDelay: 12.0, interval: 28.0); // Lights Out! (faster)
        }

        private void OnDarkness()
        {
            ScheduleSpell(spell4Id: 42675, initialDelay: 2.0, interval: 22.0); // Lights Out! (rapid)
            ScheduleSpell(spell4Id: 42540, initialDelay: 5.0, interval: 14.0); // Blackout (rapid)
            ScheduleSpell(spell4Id: 63437, initialDelay: 8.0, interval: 16.0); // Nightmare Ripple (rapid)
        }
    }

    /// <summary>Spiritmother Selene the Corrupted — final boss, Normal. Creature2Id 28735.</summary>
    [ScriptFilterCreatureId(28735u)]
    public class SotSMSeleneNScript : SotSMSeleneBase { }

    /// <summary>Spiritmother Selene the Corrupted — final boss, Veteran. Creature2Id 28736.</summary>
    [ScriptFilterCreatureId(28736u)]
    public class SotSMSeleneVScript : SotSMSeleneBase { }

    // ── Minibosses (no Jabbithole spell data available) ───────────────────────────

    /// <summary>Corrupted Edgesmith Torian — miniboss, Normal. Creature2Id 28985.</summary>
    [ScriptFilterCreatureId(28985u)]
    public class SotSMTorianNScript : EncounterBossScript { }

    /// <summary>Corrupted Edgesmith Torian — miniboss, Veteran. Creature2Id 28986.</summary>
    [ScriptFilterCreatureId(28986u)]
    public class SotSMTorianVScript : EncounterBossScript { }

    /// <summary>Corrupted Lifecaller Khalee — miniboss, Normal. Creature2Id 28992.</summary>
    [ScriptFilterCreatureId(28992u)]
    public class SotSMKhaleeNScript : EncounterBossScript { }

    /// <summary>Corrupted Lifecaller Khalee — miniboss, Veteran. Creature2Id 28993.</summary>
    [ScriptFilterCreatureId(28993u)]
    public class SotSMKhaleeVScript : EncounterBossScript { }

    /// <summary>Corrupted Deathbringer Koroll — miniboss, Normal. Creature2Id 28995.</summary>
    [ScriptFilterCreatureId(28995u)]
    public class SotSMKorollNScript : EncounterBossScript { }

    /// <summary>Corrupted Deathbringer Dareia — miniboss, Veteran. Creature2Id 28996.</summary>
    [ScriptFilterCreatureId(28996u)]
    public class SotSMDareiaVScript : EncounterBossScript { }
}
