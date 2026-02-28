using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Raid
{
    // Datascape (DataScape, WorldId 1333) — Boss Encounter Scripts
    // Spell IDs sourced from Jabbithole (zone 98).
    //
    // On death each script calls IContentMapInstance.TriggerBossDeath via EncounterBossScript.
    // DatascapeScript.OnBossDeath tracks kills and fires MatchFinish after all 11 required deaths.
    //
    // Rotation intervals are best-effort approximations; tune from sniff data once available.
    // Enrage timers default to 10 minutes (600s) — the DS standard enrage window.

    // ── e385 ─ System Daemons ─────────────────────────────────────────────────
    //
    // Dual-boss encounter: Null System Daemon and Binary System Daemon.
    // Spell IDs confirmed from Jabbithole (zone 98).
    //
    //   43443 | Data Drain   — targeted drain
    //   43370 | Memory Wipe  — raid-wide threat wipe
    //   44015 | Purge        — targeted cleanse/dispel
    //   72363 | Gouge        — melee stab
    //   43011 | Power Surge  — interruptible channel
    //   43012 | Overload     — triggered if Power Surge interrupted
    //   72362 | Slice        — melee cleave

    /// <summary>System Daemons — Null Boss. Creature2Id 30495.</summary>
    [ScriptFilterCreatureId(30495u)]
    public class DSSystemDaemonsNullScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 72362, initialDelay:  2.0, interval:  5.0); // Slice
            ScheduleSpell(spell4Id: 72363, initialDelay:  4.0, interval:  8.0); // Gouge
            ScheduleSpell(spell4Id: 43443, initialDelay:  7.0, interval: 12.0); // Data Drain
            ScheduleSpell(spell4Id: 44015, initialDelay: 10.0, interval: 16.0); // Purge
            ScheduleSpell(spell4Id: 43011, initialDelay: 15.0, interval: 22.0); // Power Surge

            AddPhase(healthPct: 50f, OnPhase2);

            SetEnrage(seconds: 600.0, enrageSpellId: 43012);
        }

        private void OnPhase2()
        {
            ScheduleSpell(spell4Id: 43370, initialDelay:  5.0, interval: 28.0); // Memory Wipe
            ScheduleSpell(spell4Id: 43012, initialDelay: 10.0, interval: 30.0); // Overload
        }
    }

    /// <summary>System Daemons — Binary Boss. Creature2Id 30496.</summary>
    [ScriptFilterCreatureId(30496u)]
    public class DSSystemDaemonsBinaryScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 72362, initialDelay:  3.0, interval:  5.0); // Slice
            ScheduleSpell(spell4Id: 72363, initialDelay:  5.0, interval:  8.0); // Gouge
            ScheduleSpell(spell4Id: 43443, initialDelay:  8.0, interval: 12.0); // Data Drain
            ScheduleSpell(spell4Id: 44015, initialDelay: 12.0, interval: 16.0); // Purge
            ScheduleSpell(spell4Id: 43011, initialDelay: 18.0, interval: 22.0); // Power Surge

            AddPhase(healthPct: 50f, OnPhase2);

            SetEnrage(seconds: 600.0, enrageSpellId: 43012);
        }

        private void OnPhase2()
        {
            ScheduleSpell(spell4Id: 43370, initialDelay:  5.0, interval: 28.0); // Memory Wipe
            ScheduleSpell(spell4Id: 43012, initialDelay: 12.0, interval: 30.0); // Overload
        }
    }

    // ── e390 ─ Maelstrom Authority ────────────────────────────────────────────
    //
    // Weather-phase encounter. Spell IDs confirmed from Jabbithole (zone 98).
    //
    //   66413 | Conduction        — electric chain / conductor mechanic
    //   44325 | Lightning Rod     — targeted chase telegraph
    //   64943 | Maelstrom Bolt    — primary ranged attack
    //   45506 | Shifting Currents — arena repositioning / leap

    /// <summary>Maelstrom Authority — Air Boss. Creature2Id 30497.</summary>
    [ScriptFilterCreatureId(30497u)]
    public class DSMaelstromAuthorityScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            // Primary rotation
            ScheduleSpell(spell4Id: 64943, initialDelay:  3.0, interval:  8.0); // Maelstrom Bolt
            ScheduleSpell(spell4Id: 66413, initialDelay:  7.0, interval: 14.0); // Conduction
            ScheduleSpell(spell4Id: 44325, initialDelay: 12.0, interval: 20.0); // Lightning Rod
            ScheduleSpell(spell4Id: 45506, initialDelay: 20.0, interval: 28.0); // Shifting Currents

            // Phase 2 — Intensified storms at 60%
            AddPhase(healthPct: 60f, OnStormPhase);
            // Phase 3 — Full fury at 25%
            AddPhase(healthPct: 25f, OnFuryPhase);

            SetEnrage(seconds: 600.0, enrageSpellId: 66413);
        }

        private void OnStormPhase()
        {
            ScheduleSpell(spell4Id: 66413, initialDelay: 3.0, interval: 10.0); // Conduction (faster)
            ScheduleSpell(spell4Id: 44325, initialDelay: 6.0, interval: 16.0); // Lightning Rod (faster)
        }

        private void OnFuryPhase()
        {
            ScheduleSpell(spell4Id: 64943, initialDelay: 2.0, interval:  5.0); // Maelstrom Bolt (rapid)
        }
    }

    // ── e393 ─ Gloomclaw ──────────────────────────────────────────────────────
    //
    // Corruption-themed multi-phase encounter.
    // Spell IDs confirmed from Jabbithole (zone 98).
    //
    //   44318 | Blight Blast    — ranged blight projectile
    //   44171 | Rupture         — large AoE burst
    //   43976 | Blight Blast    — variant / empowered form
    //   44322 | Corruption Pool — ground-targeted persistent AoE
    //   44043 | Plague Smash    — heavy frontal smash

    /// <summary>Gloomclaw. Creature2Id 30498.</summary>
    [ScriptFilterCreatureId(30498u)]
    public class DSGloomclawScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            // Primary rotation
            ScheduleSpell(spell4Id: 44318, initialDelay:  3.0, interval:  8.0); // Blight Blast
            ScheduleSpell(spell4Id: 44043, initialDelay:  6.0, interval: 12.0); // Plague Smash
            ScheduleSpell(spell4Id: 44322, initialDelay: 10.0, interval: 16.0); // Corruption Pool
            ScheduleSpell(spell4Id: 44171, initialDelay: 16.0, interval: 24.0); // Rupture

            // Phase 2 — Corruption intensifies at 60%
            AddPhase(healthPct: 60f, OnPhase2);
            // Phase 3 — Full corruption at 25%
            AddPhase(healthPct: 25f, OnPhase3);

            SetEnrage(seconds: 600.0, enrageSpellId: 44171);
        }

        private void OnPhase2()
        {
            ScheduleSpell(spell4Id: 43976, initialDelay:  3.0, interval: 10.0); // Blight Blast (empowered)
            ScheduleSpell(spell4Id: 44322, initialDelay:  8.0, interval: 12.0); // Corruption Pool (faster)
        }

        private void OnPhase3()
        {
            ScheduleSpell(spell4Id: 44171, initialDelay:  3.0, interval: 16.0); // Rupture (rapid)
            ScheduleSpell(spell4Id: 43976, initialDelay:  5.0, interval:  8.0); // Blight Blast (rapid)
        }
    }

    // ── e395 ─ Elemental Bosses ───────────────────────────────────────────────
    // Six elemental boss encounters in the Datascape. All six must be defeated.
    // Elementals fight in pairs with combo mechanics. Each has element-specific
    // abilities plus paired interaction spells.

    // ── Megalith / Earth Elemental ─────────────────────────────────────────────
    // Spell IDs confirmed from Jabbithole (zone 98).
    //   49884 | Fierce Swipe    — primary melee
    //   50305 | Rockfall        — ranged AoE
    //   73208 | Tectonic Steps  — PBAE
    //   50099 | Erupt           — ground eruption
    //   46322 | Pound           — heavy melee
    //   73205 | Superquake      — massive AoE

    /// <summary>Megalith — Earth Elemental Boss. Creature2Id 30499.</summary>
    [ScriptFilterCreatureId(30499u)]
    public class DSEarthElementalScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 49884, initialDelay:  2.0, interval:  5.0); // Fierce Swipe
            ScheduleSpell(spell4Id: 46322, initialDelay:  4.0, interval:  9.0); // Pound
            ScheduleSpell(spell4Id: 50305, initialDelay:  8.0, interval: 14.0); // Rockfall
            ScheduleSpell(spell4Id: 73208, initialDelay: 12.0, interval: 18.0); // Tectonic Steps
            ScheduleSpell(spell4Id: 50099, initialDelay: 18.0, interval: 24.0); // Erupt

            AddPhase(healthPct: 40f, OnEnrage);
            SetEnrage(seconds: 600.0, enrageSpellId: 73205);
        }

        private void OnEnrage()
        {
            ScheduleSpell(spell4Id: 73205, initialDelay: 2.0, interval: 15.0); // Superquake
        }
    }

    // ── Hydroflux / Water Elemental ────────────────────────────────────────────
    // Spell IDs confirmed from Jabbithole (zone 98).
    //   47069 | Geyser            — tracking AoE
    //   46975 | Sinkhole          — large ground AoE
    //   52911 | Crashing Wave     — wave telegraph
    //   53192 | Watery Grave      — threat wipe
    //   46324 | Pound             — heavy melee
    //   73288 | Tsunami           — massive wave AoE

    /// <summary>Hydroflux — Water Elemental Boss. Creature2Id 30500.</summary>
    [ScriptFilterCreatureId(30500u)]
    public class DSWaterElementalScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 46324, initialDelay:  3.0, interval:  8.0); // Pound
            ScheduleSpell(spell4Id: 47069, initialDelay:  6.0, interval: 12.0); // Geyser
            ScheduleSpell(spell4Id: 52911, initialDelay: 10.0, interval: 16.0); // Crashing Wave
            ScheduleSpell(spell4Id: 46975, initialDelay: 15.0, interval: 25.0); // Sinkhole
            ScheduleSpell(spell4Id: 53192, initialDelay: 20.0, interval: 30.0); // Watery Grave

            AddPhase(healthPct: 40f, OnTsunami);
            SetEnrage(seconds: 600.0, enrageSpellId: 73288);
        }

        private void OnTsunami()
        {
            ScheduleSpell(spell4Id: 73288, initialDelay: 5.0, interval: 45.0); // Tsunami
        }
    }

    // ── Visceralus / Life Elemental ────────────────────────────────────────────
    // Spell IDs confirmed from Jabbithole (zone 98).
    //   48520 | Detonate             — AoE explosion
    //   74368 | Life Force           — healing / life drain
    //   47593 | Blinding Light       — rotating cone AoE
    //   46326 | Pound                — heavy melee
    //   73178 | Primal Entanglement  — root / CC

    /// <summary>Visceralus — Life Elemental Boss. Creature2Id 30501.</summary>
    [ScriptFilterCreatureId(30501u)]
    public class DSLifeElementalScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 46326, initialDelay:  3.0, interval:  8.0); // Pound
            ScheduleSpell(spell4Id: 48520, initialDelay:  7.0, interval: 14.0); // Detonate
            ScheduleSpell(spell4Id: 47593, initialDelay: 12.0, interval: 20.0); // Blinding Light
            ScheduleSpell(spell4Id: 74368, initialDelay: 18.0, interval: 25.0); // Life Force

            AddPhase(healthPct: 40f, OnLowHealth);
            SetEnrage(seconds: 600.0, enrageSpellId: 47593);
        }

        private void OnLowHealth()
        {
            ScheduleSpell(spell4Id: 73178, initialDelay: 5.0, interval: 30.0); // Primal Entanglement
        }
    }

    // ── Aileron / Air Elemental ────────────────────────────────────────────────
    // Spell IDs confirmed from Jabbithole (zone 98).
    //   46329 | Elemental Bolt   — ranged projectile
    //   46802 | Surging Wind     — wind AoE
    //   46801 | Surging Wind     — variant
    //   74441 | Lightning Strike — targeted heavy hit

    /// <summary>Aileron — Air Elemental Boss. Creature2Id 30502.</summary>
    [ScriptFilterCreatureId(30502u)]
    public class DSAirElementalScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 46329, initialDelay:  2.0, interval:  6.0); // Elemental Bolt
            ScheduleSpell(spell4Id: 46802, initialDelay:  5.0, interval: 12.0); // Surging Wind
            ScheduleSpell(spell4Id: 46801, initialDelay: 10.0, interval: 18.0); // Surging Wind (variant)
            ScheduleSpell(spell4Id: 74441, initialDelay: 16.0, interval: 24.0); // Lightning Strike

            AddPhase(healthPct: 40f, OnStormPhase);
            SetEnrage(seconds: 600.0, enrageSpellId: 74441);
        }

        private void OnStormPhase()
        {
            ScheduleSpell(spell4Id: 74441, initialDelay: 3.0, interval: 16.0); // Lightning Strike (faster)
            ScheduleSpell(spell4Id: 46802, initialDelay: 6.0, interval:  9.0); // Surging Wind (faster)
        }
    }

    // ── Pyrobane / Fire Elemental ──────────────────────────────────────────────
    // Spell IDs confirmed from Jabbithole (zone 98).
    //   78631 | Eruption           — ground eruption AoE
    //   49869 | Flame Wave         — wave telegraph
    //   70702 | Meteor             — heavy targeted AoE
    //   73095 | Contagious Flames  — spreading fire DoT
    //   75059 | Heat Stroke        — targeted debuff
    //   52959 | Rock Pillar Explosion — pillar mechanic

    /// <summary>Pyrobane — Fire Elemental Boss. Creature2Id 30503.</summary>
    [ScriptFilterCreatureId(30503u)]
    public class DSFireElementalScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 78631, initialDelay:  2.0, interval:  5.0); // Eruption
            ScheduleSpell(spell4Id: 49869, initialDelay:  5.0, interval: 10.0); // Flame Wave
            ScheduleSpell(spell4Id: 73095, initialDelay:  9.0, interval: 16.0); // Contagious Flames
            ScheduleSpell(spell4Id: 70702, initialDelay: 14.0, interval: 22.0); // Meteor
            ScheduleSpell(spell4Id: 75059, initialDelay: 20.0, interval: 28.0); // Heat Stroke

            AddPhase(healthPct: 40f, OnRagnarok);
            SetEnrage(seconds: 600.0, enrageSpellId: 70702);
        }

        private void OnRagnarok()
        {
            ScheduleSpell(spell4Id: 52959, initialDelay: 3.0, interval: 30.0); // Rock Pillar Explosion
            ScheduleSpell(spell4Id: 70702, initialDelay: 8.0, interval: 16.0); // Meteor (faster)
        }
    }

    // ── Mnemesis / Logic Elemental ─────────────────────────────────────────────
    // Spell IDs confirmed from Jabbithole (zone 98).
    //   47544 | Vibromatic Synthesis          — fast melee
    //   74482 | Electrolyzed Defragmentation  — AoE burst
    //   47677 | Alphanumeric Hash             — targeted debuff
    //   74481 | Derez                         — destruction beam
    //   52007 | Defragment                    — puzzle mechanic
    //   74598 | Logic Leash                   — tether CC

    /// <summary>Mnemesis — Logic Elemental Boss. Creature2Id 30504.</summary>
    [ScriptFilterCreatureId(30504u)]
    public class DSLogicElementalScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 47544, initialDelay:  3.0, interval:  7.0); // Vibromatic Synthesis
            ScheduleSpell(spell4Id: 74482, initialDelay:  6.0, interval: 12.0); // Electrolyzed Defragmentation
            ScheduleSpell(spell4Id: 47677, initialDelay: 10.0, interval: 18.0); // Alphanumeric Hash
            ScheduleSpell(spell4Id: 74481, initialDelay: 16.0, interval: 24.0); // Derez
            ScheduleSpell(spell4Id: 74598, initialDelay: 22.0, interval: 30.0); // Logic Leash

            AddPhase(healthPct: 40f, OnPuzzlePhase);
            SetEnrage(seconds: 600.0, enrageSpellId: 74482);
        }

        private void OnPuzzlePhase()
        {
            ScheduleSpell(spell4Id: 52007, initialDelay: 5.0, interval: 35.0); // Defragment
            ScheduleSpell(spell4Id: 74482, initialDelay: 3.0, interval:  9.0); // Electrolyzed Defragmentation (faster)
        }
    }

    // ── e399 ─ Avatus — Final Boss ────────────────────────────────────────────
    //
    // Multi-phase encounter. Spell IDs confirmed from Jabbithole (zone 98).
    //
    //   46727 | Disintegration Sequence — targeted destruction beam
    //   46870 | Obliterate             — heavy AoE attack
    //   44785 | Annihilation           — double cannon tank spike
    //   45892 | Obliteration Beam      — channeled beam
    //   69858 | Phase Punch            — targeted AoE punch
    //   44546 | Disruption Beam        — disrupting beam
    //   45055 | Binding Strike         — CC / root
    //   44451 | Holo-Swipe             — Holo Hand melee cleave
    //   44710 | Swipe                  — Holo Hand basic melee
    //   44726 | Burning Exhaust        — Holo Hand fire trail
    //   44655 | Nullifying Rays        — Holo Hand ranged suppression
    //   69579 | Disintegration Sector  — AoE sector telegraph

    /// <summary>Avatus — final boss of Datascape. Creature2Id 30505.</summary>
    [ScriptFilterCreatureId(30505u)]
    public class DSAvatusScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            // Phase 1 — Showdown: melee + Holo Hand attacks
            ScheduleSpell(spell4Id: 44710, initialDelay:  2.0, interval:  6.0); // Swipe
            ScheduleSpell(spell4Id: 44451, initialDelay:  4.0, interval:  8.0); // Holo-Swipe
            ScheduleSpell(spell4Id: 44726, initialDelay:  7.0, interval: 14.0); // Burning Exhaust
            ScheduleSpell(spell4Id: 44655, initialDelay: 10.0, interval: 16.0); // Nullifying Rays
            ScheduleSpell(spell4Id: 44785, initialDelay: 14.0, interval: 20.0); // Annihilation (tank spike)
            ScheduleSpell(spell4Id: 45055, initialDelay: 20.0, interval: 26.0); // Binding Strike

            // Phase 2 — Ranged bombardment at 65%
            AddPhase(healthPct: 65f, OnRangedPhase);
            // Phase 3 — Final confrontation at 35%
            AddPhase(healthPct: 35f, OnFinalPhase);

            SetEnrage(seconds: 720.0, enrageSpellId: 46727); // 12-min enrage for final boss
        }

        private void OnRangedPhase()
        {
            ScheduleSpell(spell4Id: 44546, initialDelay:  3.0, interval: 14.0); // Disruption Beam
            ScheduleSpell(spell4Id: 69858, initialDelay:  8.0, interval: 18.0); // Phase Punch
            ScheduleSpell(spell4Id: 45892, initialDelay: 14.0, interval: 24.0); // Obliteration Beam
        }

        private void OnFinalPhase()
        {
            ScheduleSpell(spell4Id: 46727, initialDelay:  3.0, interval: 22.0); // Disintegration Sequence
            ScheduleSpell(spell4Id: 46870, initialDelay:  8.0, interval: 20.0); // Obliterate
            ScheduleSpell(spell4Id: 69579, initialDelay: 14.0, interval: 28.0); // Disintegration Sector
        }
    }
}
