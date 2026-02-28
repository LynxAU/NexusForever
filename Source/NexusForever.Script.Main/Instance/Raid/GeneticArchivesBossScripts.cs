using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Raid
{
    // Genetic Archives (GeneticArchives, WorldId 1462) — Boss Encounter Scripts
    //
    // All spell IDs sourced from Spell4.tbl "[GA] e4XX" tagged entries.
    // Rotation intervals are best-effort approximations from cast times + observed WildStar
    // encounter cadences; tune from sniff data once available.
    //
    // Enrage timers default to 8 minutes (480s) — the GA standard enrage window.

    // ── e410 ─ Experiment X-89 (Strain Mauler) ────────────────────────────────
    //   47351 | 2000ms | Repugnant Spew     — frontal cone DoT
    //   47279 | 2750ms | Resounding Shout   — PBAE knockback
    //   47271 | 2000ms | Shattering Shockwave — ground AoE
    //   47285 |10000ms | Strain Bomb        — long-fuse bomb
    //   47316 | 5000ms | Corruption Globule — channeled pool
    //   58855 | 3000ms | Shockwave (bridge variant) — used in bridge phase

    /// <summary>Experiment X-89 (Strain Mauler). Creature2Id 49198.</summary>
    [ScriptFilterCreatureId(49198u)]
    public class GAExperimentX89Script : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            // Primary rotation
            ScheduleSpell(spell4Id: 47351, initialDelay:  3.0, interval:  8.0); // Repugnant Spew
            ScheduleSpell(spell4Id: 47271, initialDelay:  6.0, interval: 15.0); // Shattering Shockwave
            ScheduleSpell(spell4Id: 47279, initialDelay: 12.0, interval: 22.0); // Resounding Shout (KB)
            ScheduleSpell(spell4Id: 47316, initialDelay: 18.0, interval: 28.0); // Corruption Globule
            ScheduleSpell(spell4Id: 47285, initialDelay: 25.0, interval: 35.0); // Strain Bomb

            // Phase 2: bridge destruction variant shockwave at 50% health
            AddPhase(healthPct: 50f, OnPhase2);

            SetEnrage(seconds: 480.0, enrageSpellId: 47271); // fallback: heavier shockwave spam
        }

        private void OnPhase2()
        {
            ScheduleSpell(spell4Id: 58855, initialDelay: 2.0, interval: 20.0); // Bridge Shockwave
        }
    }

    // ── e411 ─ Phage Maw (Metal Maw) ──────────────────────────────────────────
    //   56517 | 2300ms | Bombardment        — primary ranged attack
    //   57344 | 2000ms | Break Chains       — initiates 45s harpoon channel
    //   60612 | 3000ms | Detonation Bombs   — proximity bomb placement
    //   60437 | 2000ms | Laser Blast        — targeted beam
    //   60835 |  500ms | Laser Blast Field  — persistent 15s field
    //   57215 |45000ms | Aerial Bombardment — enrage-style wipe mechanic
    //   60442 | 7500ms | Raid Wipe Smash    — lethal fallback if enrage ignored

    /// <summary>Phage Maw (Metal Maw). Creature2Id 52974.</summary>
    [ScriptFilterCreatureId(52974u)]
    public class GAPhageMawScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 56517, initialDelay:  2.0, interval:  5.0); // Bombardment
            ScheduleSpell(spell4Id: 60437, initialDelay:  5.0, interval:  9.0); // Laser Blast
            ScheduleSpell(spell4Id: 60612, initialDelay: 15.0, interval: 30.0); // Detonation Bombs
            ScheduleSpell(spell4Id: 57344, initialDelay: 30.0, interval: 90.0); // Break Chains (harpoon phase)

            // At 30% Phage Maw enrages and begins aerial bombardment
            AddPhase(healthPct: 30f, OnEnrage);

            SetEnrage(seconds: 480.0, enrageSpellId: 60442); // Raid Wipe Smash
        }

        private void OnEnrage()
        {
            ScheduleSpell(spell4Id: 57215, initialDelay: 5.0, interval: 45.0); // Aerial Bombardment
        }
    }

    // ── e412 ─ Kuralak the Defiler (Genetic Architect) ────────────────────────
    //   56649 | 5000ms | Chromosome Corruption — stacking debuff on tank
    //   56589 |      — | DNA Siphon (Tank)   — channel on current tank
    //   60446 | 5000ms | Cultivate           — summons Cultivated Abomination add
    //   57837 | 6000ms | Putrid Discharge    — targeted AoE
    //   60623 | 8000ms | Outbreak            — raid-wide disease
    //   60366 | 3000ms | Tainted Ventilation — environmental hazard
    //   57729 | 1500ms | Vileness GTAE       — ground-targeted AoE
    //   60397 |100100ms| Spin                — phase transition (door-opening mechanic)
    //   60159 |      — | Contaminate         — channeled AoE field

    /// <summary>Kuralak the Defiler (Genetic Architect). Creature2Id 52969.</summary>
    [ScriptFilterCreatureId(52969u)]
    public class GAKuralakTheDefilerScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 57729, initialDelay:  3.0, interval: 10.0); // Vileness GTAE
            ScheduleSpell(spell4Id: 56649, initialDelay:  5.0, interval: 15.0); // Chromosome Corruption
            ScheduleSpell(spell4Id: 57837, initialDelay:  8.0, interval: 20.0); // Putrid Discharge
            ScheduleSpell(spell4Id: 56589, initialDelay: 12.0, interval: 30.0); // DNA Siphon (tank)
            ScheduleSpell(spell4Id: 60366, initialDelay: 15.0, interval: 22.0); // Tainted Ventilation
            ScheduleSpell(spell4Id: 60446, initialDelay: 25.0, interval: 35.0); // Cultivate (summon add)
            ScheduleSpell(spell4Id: 60623, initialDelay: 40.0, interval: 45.0); // Outbreak (raid disease)

            // Phase 2 — Spin (door mechanic) at 50%
            AddPhase(healthPct: 50f, OnPhase2);

            SetEnrage(seconds: 480.0, enrageSpellId: 60623);
        }

        private void OnPhase2()
        {
            ScheduleSpell(spell4Id: 60397, initialDelay:  5.0, interval: 100.0); // Spin
            ScheduleSpell(spell4Id: 60159, initialDelay: 10.0, interval:  40.0); // Contaminate
        }
    }

    // ── e413 ─ Phagetech Prototypes (Four Gho-bots) ───────────────────────────

    /// <summary>Gho-bot Phagetech Augmentor. Creature2Id 54029.</summary>
    [ScriptFilterCreatureId(54029u)]
    public class GAGhobotAugmentorScript : BossEncounterScript
    {
        // 64839 | Drill Telegraph — ground drill (6s cast)
        // 59758 | Summon Repair Bot — spawns healing add
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 64839, initialDelay:  5.0, interval: 25.0); // Drill Telegraph
            ScheduleSpell(spell4Id: 59758, initialDelay: 20.0, interval: 45.0); // Summon Repair Bot
            SetEnrage(seconds: 480.0, enrageSpellId: 64839);
        }
    }

    /// <summary>Gho-bot Phagetech Fabricator. Creature2Id 54030.</summary>
    [ScriptFilterCreatureId(54030u)]
    public class GAGhobotFabricatorScript : BossEncounterScript
    {
        // 59709 | Destructo Bot Self Destruct — 12s long-fuse (suicide bomber)
        // 59757 | Summon Destructo Bot — spawns bomb add
        // 66487 | Fabricator Basketball — thrown projectile mechanic
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 66487, initialDelay:  4.0, interval: 20.0); // Basketball
            ScheduleSpell(spell4Id: 59757, initialDelay: 15.0, interval: 22.0); // Summon Destructo Bot
            ScheduleSpell(spell4Id: 59709, initialDelay: 25.0, interval: 60.0); // Destructo Bot Self-Destruct
            SetEnrage(seconds: 480.0, enrageSpellId: 66487);
        }
    }

    /// <summary>Gho-bot Phagetech Protector. Creature2Id 54031.</summary>
    [ScriptFilterCreatureId(54031u)]
    public class GAGhobotProtectorScript : BossEncounterScript
    {
        // 59719 | Pulse Waves — knockback rings (5s cast)
        // 64842 | Position Swap — teleports target
        // 59707 | Wave Aura — persistent pulsing field (20s, 500ms pulse)
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 64842, initialDelay:  5.0, interval: 25.0); // Position Swap
            ScheduleSpell(spell4Id: 59719, initialDelay: 10.0, interval: 20.0); // Pulse Waves
            ScheduleSpell(spell4Id: 59707, initialDelay: 25.0, interval: 40.0); // Wave Aura
            SetEnrage(seconds: 480.0, enrageSpellId: 59719);
        }
    }

    /// <summary>Gho-bot Phagetech Commander. Creature2Id 54032.</summary>
    [ScriptFilterCreatureId(54032u)]
    public class GAGhobotCommanderScript : BossEncounterScript
    {
        // 59662 | Pound Tank — heavy tank hit (4.5s cast)
        // 60810 | Forced Production — raid-wide damage ramp (6s cast)
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 59662, initialDelay:  3.0, interval:  8.0); // Pound Tank
            ScheduleSpell(spell4Id: 60810, initialDelay: 20.0, interval: 30.0); // Forced Production
            SetEnrage(seconds: 480.0, enrageSpellId: 60810);
        }
    }

    // ── e413 supplemental ─ Phagetech Guardians (Probebots) ───────────────────

    /// <summary>Phagetech Guardian C-148 (Probebot #1). Creature2Id 54055.</summary>
    [ScriptFilterCreatureId(54055u)]
    public class GAPhageGuardianC148Script : BossEncounterScript
    {
        // TODO: Extract specific Probebot #1 Spell4 IDs from Spell4.tbl once [GA] C-148 tag confirmed.
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 64839, initialDelay: 5.0, interval: 20.0); // Shared Drill from Augmentor set
            SetEnrage(seconds: 480.0, enrageSpellId: 64839);
        }
    }

    /// <summary>Phagetech Guardian C-432 (Probebot #2). Creature2Id 54056.</summary>
    [ScriptFilterCreatureId(54056u)]
    public class GAPhageGuardianC432Script : BossEncounterScript
    {
        // TODO: Extract specific Probebot #2 Spell4 IDs.
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 59719, initialDelay: 5.0, interval: 20.0); // Shared Pulse from Protector set
            SetEnrage(seconds: 480.0, enrageSpellId: 59719);
        }
    }

    // ── Phageborn Convergence ─ Five-Member Council ────────────────────────────
    //
    // Role mapping to creature IDs is unconfirmed; abilities are assigned by inferred role.
    // TODO: Confirm creature-to-role mapping via in-game testing.
    //
    //   Leader:    58423 Essence Rot | 57232 Equalize HP | 60399 Piercing Vision
    //   DPS:       57862 Leap        | 57686/57838 MegaCast (20s enrage pair)
    //   Healer:    57623 DOT Cast    | 57412 HOT PBAE    | 60377 MegaCast Reconstruct Sinew
    //   Control:   56983 Foul Scourge| 56962 Time Bomb   | 56978/60349 MegaCast

    /// <summary>TMNS council — Leader role (unconfirmed). Creature2Id 52963.</summary>
    [ScriptFilterCreatureId(52963u)]
    public class GATMNSMember1Script : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 57232, initialDelay:  8.0, interval: 20.0); // Equalize HP
            ScheduleSpell(spell4Id: 58423, initialDelay: 12.0, interval: 18.0); // Essence Rot
            ScheduleSpell(spell4Id: 60399, initialDelay: 30.0, interval: 55.0); // Piercing Vision (55s channel)
        }
    }

    /// <summary>TMNS council — DPS role (unconfirmed). Creature2Id 52964.</summary>
    [ScriptFilterCreatureId(52964u)]
    public class GATMNSMember2Script : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 57862, initialDelay:  5.0, interval: 15.0); // Leap
            ScheduleSpell(spell4Id: 57686, initialDelay: 60.0, interval: 90.0); // MegaCast Gathering Energy
        }
    }

    /// <summary>TMNS council — Healer role (unconfirmed). Creature2Id 52968.</summary>
    [ScriptFilterCreatureId(52968u)]
    public class GATMNSMember3Script : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 57623, initialDelay:  5.0, interval: 10.0); // DOT Cast
            ScheduleSpell(spell4Id: 57412, initialDelay: 10.0, interval: 20.0); // HOT PBAE
            ScheduleSpell(spell4Id: 57419, initialDelay: 15.0, interval: 30.0); // Field PBAE
            ScheduleSpell(spell4Id: 60377, initialDelay: 60.0, interval: 90.0); // MegaCast Reconstruct Sinew
        }
    }

    /// <summary>TMNS council — Controller role (unconfirmed). Creature2Id 52970.</summary>
    [ScriptFilterCreatureId(52970u)]
    public class GATMNSMember4Script : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 56983, initialDelay:  6.0, interval: 18.0); // Foul Scourge (CC)
            ScheduleSpell(spell4Id: 56962, initialDelay: 15.0, interval: 25.0); // Time Bomb Tether
            ScheduleSpell(spell4Id: 56978, initialDelay: 60.0, interval: 90.0); // MegaCast Gathering Energy
        }
    }

    /// <summary>TMNS council — fifth member (role unconfirmed). Creature2Id 52971.</summary>
    [ScriptFilterCreatureId(52971u)]
    public class GATMNSMember5Script : BossEncounterScript
    {
        // TODO: Identify fifth council member's role and extract Spell4 IDs.
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 58423, initialDelay: 10.0, interval: 22.0); // Placeholder: Essence Rot
        }
    }

    // ── e415 ─ Dreadphage Ohmna ────────────────────────────────────────────────
    //
    // Three-phase fight:
    //   Phase 1 (100–70%): Normal form, tier-1 abilities.
    //   Phase 2 (70–30%):  Empowered form, tier-2 abilities added.
    //   Phase 3 (30–0%):   Giant form via Gene Splice; tier-3 abilities.
    //
    //   47359/72662/75717 | 3000ms | Body Slam (T1/T2/T3)
    //   47361/59764/72661 | 4000ms | Erupt     (T1/T2/T3)
    //   47364             | 4000ms | Genetic Torrent — sweeping beam
    //   60933             |  900ms | Corner Prey     — tether + chase
    //   60925             | 5000ms | Devour and Consume — large AoE
    //   59632             |  250ms | Ravage           — fast melee cleave
    //   47745             |      — | Sap Power        — 30s drain field
    //   47494             |      — | Gene Splice      — phase transformation
    //   47887             |      — | Moment of Opportunity — phase transition

    /// <summary>Dreadphage Ohmna — final boss of Genetic Archives. Creature2Id 49395.</summary>
    [ScriptFilterCreatureId(49395u)]
    public class GADreadphageOhmnaScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            // Phase 1 rotation (starts at combat begin)
            ScheduleSpell(spell4Id: 59632, initialDelay:  2.0, interval:  5.0); // Ravage (fast cleave)
            ScheduleSpell(spell4Id: 47359, initialDelay:  4.0, interval: 15.0); // Body Slam T1
            ScheduleSpell(spell4Id: 47361, initialDelay:  8.0, interval: 20.0); // Erupt T1
            ScheduleSpell(spell4Id: 47364, initialDelay: 20.0, interval: 40.0); // Genetic Torrent
            ScheduleSpell(spell4Id: 60933, initialDelay: 25.0, interval: 30.0); // Corner Prey
            ScheduleSpell(spell4Id: 60925, initialDelay: 35.0, interval: 35.0); // Devour and Consume

            AddPhase(healthPct: 70f, OnPhase2);
            AddPhase(healthPct: 30f, OnPhase3);

            SetEnrage(seconds: 600.0, enrageSpellId: 47364); // Extended 10-min enrage for final boss
        }

        private void OnPhase2()
        {
            // Moment of Opportunity transition, then empowered tier-2 abilities
            ScheduleSpell(spell4Id: 47887, initialDelay:  1.0, interval: 999.0); // MoO (fires once)
            ScheduleSpell(spell4Id: 72662, initialDelay:  5.0, interval: 14.0);  // Body Slam T2
            ScheduleSpell(spell4Id: 59764, initialDelay:  9.0, interval: 18.0);  // Erupt T2
            ScheduleSpell(spell4Id: 47745, initialDelay: 30.0, interval: 60.0);  // Sap Power (30s drain field)
        }

        private void OnPhase3()
        {
            // Gene Splice transforms Ohmna into giant form
            ScheduleSpell(spell4Id: 47494, initialDelay:  1.0, interval: 999.0); // Gene Splice (fires once)
            ScheduleSpell(spell4Id: 75717, initialDelay:  5.0, interval: 12.0);  // Body Slam Giant Phase
            ScheduleSpell(spell4Id: 72661, initialDelay:  9.0, interval: 16.0);  // Erupt T3
        }
    }

    // ── Optional Minibosses ────────────────────────────────────────────────────

    /// <summary>Miniboss — Genetic Monstrosity. Creature2Id 54968.</summary>
    [ScriptFilterCreatureId(54968u)]
    public class GAGeneticMonstrosityScript : BossEncounterScript
    {
        // 69962 | Noxious Belch   — poison breath
        // 61378 | Radiation Bath  — channeled AoE (2s cast)
        // 61341 | Foul Rupture    — quick burst (0.5s)
        // 61357 | Repulsive Cultivation — ground AoE (4s)
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 61341, initialDelay:  3.0, interval:  8.0); // Foul Rupture
            ScheduleSpell(spell4Id: 69962, initialDelay:  6.0, interval: 12.0); // Noxious Belch
            ScheduleSpell(spell4Id: 61357, initialDelay: 12.0, interval: 20.0); // Repulsive Cultivation
            ScheduleSpell(spell4Id: 61378, initialDelay: 20.0, interval: 30.0); // Radiation Bath
        }
    }

    /// <summary>Miniboss — Gravitron Operator. Creature2Id 56184.</summary>
    [ScriptFilterCreatureId(56184u)]
    public class GAGravitronOperatorScript : BossEncounterScript
    {
        // 62652 | Gravity Crush - Base
        // 62731 | Gravity Spike - Base
        // 70439 | Gravity Reversal - Base
        // 70448 | Gravity Flux - Base
        // 69770 | Anti-Gravity Bubble - Base
        // 62736 | Gravity Swell
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 62652, initialDelay:  4.0, interval: 10.0); // Gravity Crush
            ScheduleSpell(spell4Id: 62731, initialDelay:  8.0, interval: 16.0); // Gravity Spike
            ScheduleSpell(spell4Id: 70439, initialDelay: 14.0, interval: 24.0); // Gravity Reversal
            ScheduleSpell(spell4Id: 70448, initialDelay: 20.0, interval: 28.0); // Gravity Flux
            ScheduleSpell(spell4Id: 69770, initialDelay: 28.0, interval: 35.0); // Anti-Gravity Bubble
            ScheduleSpell(spell4Id: 62736, initialDelay: 36.0, interval: 40.0); // Gravity Swell
            SetEnrage(seconds: 480.0, enrageSpellId: 62731);
        }
    }

    /// <summary>Miniboss — Hideously Malformed Mutant. Creature2Id 56178.</summary>
    [ScriptFilterCreatureId(56178u)]
    public class GAHideouslyMalformedMutantScript : BossEncounterScript
    {
        // 62657 | Cancerous Breeding - Base
        // 62936 | Cellular Leech - Base
        // 62733 | Consume - Base
        // 62732 | Mitosis
        // 69923 | Cellular Breakdown
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 62657, initialDelay:  3.0, interval: 14.0); // Cancerous Breeding
            ScheduleSpell(spell4Id: 62936, initialDelay:  7.0, interval: 18.0); // Cellular Leech
            ScheduleSpell(spell4Id: 62733, initialDelay: 12.0, interval: 22.0); // Consume
            ScheduleSpell(spell4Id: 62732, initialDelay: 20.0, interval: 30.0); // Mitosis
            ScheduleSpell(spell4Id: 69923, initialDelay: 26.0, interval: 34.0); // Cellular Breakdown
            SetEnrage(seconds: 480.0, enrageSpellId: 62733);
        }
    }

    /// <summary>Miniboss — Fetid Miscreation (internal: Ravenok). Creature2Id 56377.</summary>
    [ScriptFilterCreatureId(56377u)]
    public class GAFetidMiscreationScript : BossEncounterScript
    {
        // 62947 |20000ms | Genetic Decomposition (interrupt!) — long cast
        // 62946 |  3500ms | Ravage — targeted proxy attack
        // 62940 | — | Stacking Buff
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 62946, initialDelay:  3.0, interval:  8.0); // Ravage
            ScheduleSpell(spell4Id: 62947, initialDelay: 20.0, interval: 30.0); // Genetic Decomposition
        }
    }

    /// <summary>Miniboss — Guardian East. Creature2Id 54785.</summary>
    [ScriptFilterCreatureId(54785u)]
    public class GAGuardianEastScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 61418, initialDelay:  5.0, interval: 16.0); // Hookshot
            ScheduleSpell(spell4Id: 61227, initialDelay: 10.0, interval: 24.0); // Banish
            ScheduleSpell(spell4Id: 61494, initialDelay: 16.0, interval: 28.0); // Laser - Extermination Beam
            ScheduleSpell(spell4Id: 61419, initialDelay: 24.0, interval: 34.0); // Eradicate
            ScheduleSpell(spell4Id: 61287, initialDelay: 32.0, interval: 45.0); // Maintenance Protocol - East Signal
            ScheduleSpell(spell4Id: 61321, initialDelay: 40.0, interval: 60.0); // Genetic Overload 3
            SetEnrage(seconds: 480.0, enrageSpellId: 61321);
        }
    }

    /// <summary>Miniboss — Guardian West. Creature2Id 54787.</summary>
    [ScriptFilterCreatureId(54787u)]
    public class GAGuardianWestScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 61418, initialDelay:  5.0, interval: 16.0); // Hookshot
            ScheduleSpell(spell4Id: 61227, initialDelay: 10.0, interval: 24.0); // Banish
            ScheduleSpell(spell4Id: 61494, initialDelay: 16.0, interval: 28.0); // Laser - Extermination Beam
            ScheduleSpell(spell4Id: 61419, initialDelay: 24.0, interval: 34.0); // Eradicate
            ScheduleSpell(spell4Id: 61293, initialDelay: 32.0, interval: 45.0); // Maintenance Protocol - West Signal
            ScheduleSpell(spell4Id: 61321, initialDelay: 40.0, interval: 60.0); // Genetic Overload 3
            SetEnrage(seconds: 480.0, enrageSpellId: 61321);
        }
    }

    /// <summary>Miniboss — Malfunctioning Battery. Creature2Id 56174.</summary>
    [ScriptFilterCreatureId(56174u)]
    public class GAMalfunctioningBatteryScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 62637, initialDelay:  4.0, interval: 12.0); // Over-Charge
            ScheduleSpell(spell4Id: 62638, initialDelay:  9.0, interval: 15.0); // Re-Charge
            ScheduleSpell(spell4Id: 62640, initialDelay: 14.0, interval: 18.0); // Resistor-Charge
            ScheduleSpell(spell4Id: 62691, initialDelay: 20.0, interval: 24.0); // Magnetized Cell
            ScheduleSpell(spell4Id: 62683, initialDelay: 30.0, interval: 32.0); // Voltage Storm
            ScheduleSpell(spell4Id: 70241, initialDelay: 36.0, interval: 40.0); // Power Siphon
            SetEnrage(seconds: 480.0, enrageSpellId: 62683);
        }
    }

    /// <summary>Miniboss — Malfunctioning Dynamo. Creature2Id 54935.</summary>
    [ScriptFilterCreatureId(54935u)]
    public class GAMalfunctioningDynamoScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 61322, initialDelay:  4.0, interval: 10.0); // Direct Current
            ScheduleSpell(spell4Id: 62890, initialDelay:  8.0, interval: 15.0); // Arc Thrower
            ScheduleSpell(spell4Id: 61340, initialDelay: 14.0, interval: 20.0); // Static Discharge
            ScheduleSpell(spell4Id: 70111, initialDelay: 20.0, interval: 24.0); // Static Shock
            ScheduleSpell(spell4Id: 61449, initialDelay: 28.0, interval: 32.0); // Critical Mass
            ScheduleSpell(spell4Id: 61439, initialDelay: 34.0, interval: 38.0); // Overload
            ScheduleSpell(spell4Id: 61385, initialDelay: 42.0, interval: 50.0); // Overdrive
            SetEnrage(seconds: 480.0, enrageSpellId: 61439);
        }
    }

    /// <summary>Miniboss — Malfunctioning Piston. Creature2Id 56106.</summary>
    [ScriptFilterCreatureId(56106u)]
    public class GAMalfunctioningPistonScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 62556, initialDelay:  4.0, interval: 10.0); // Dash
            ScheduleSpell(spell4Id: 62552, initialDelay:  8.0, interval: 14.0); // Leap
            ScheduleSpell(spell4Id: 62579, initialDelay: 12.0, interval: 18.0); // Burst Gasket
            ScheduleSpell(spell4Id: 62554, initialDelay: 16.0, interval: 20.0); // Electrical Current
            ScheduleSpell(spell4Id: 62566, initialDelay: 24.0, interval: 28.0); // Shatter Coupling
            ScheduleSpell(spell4Id: 62568, initialDelay: 30.0, interval: 34.0); // Shear Casing
            SetEnrage(seconds: 480.0, enrageSpellId: 62568);
        }
    }

    /// <summary>Miniboss — Malfunctioning Gear. Creature2Id 55066.</summary>
    [ScriptFilterCreatureId(55066u)]
    public class GAMalfunctioningGearScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 70948, initialDelay:  5.0, interval: 14.0); // Corrosive Fluid
            ScheduleSpell(spell4Id: 62521, initialDelay: 10.0, interval: 20.0); // Leaking Coolant
            ScheduleSpell(spell4Id: 61496, initialDelay: 20.0, interval: 26.0); // Repeat
            ScheduleSpell(spell4Id: 62506, initialDelay: 30.0, interval: 40.0); // Cleanup
            SetEnrage(seconds: 480.0, enrageSpellId: 61496);
        }
    }
}
