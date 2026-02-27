using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Raid
{
    // Genetic Archives (GeneticArchives, WorldId 1462) — Boss Encounter Scripts
    // Source: Creature2.tbl "[GA] e4XX" name tags + Spell4.tbl ability data.
    //
    // All scripts are stubs extending EncounterBossScript, which calls
    // IContentMapInstance.TriggerBossDeath on death. GeneticArchivesScript.OnBossDeath
    // tracks kills and triggers MatchFinish when all 13 required bosses are defeated.

    // ── e410 ─ Experiment X-89 (Strain Mauler) ───────────────────────────────
    // Key abilities from Spell4.tbl:
    //   47351 | 2000ms | Repugnant Spew — frontal cone DoT
    //   47279 | 2750ms | Resounding Shout — PBAE knockback
    //   47271 | 2000ms | Shattering Shockwave — ground AoE
    //   47285 |10000ms | Strain Bomb — long-fuse placed bomb
    //   47316 | 5000ms | Corruption Globule — channeled pool placement
    //   58855 | 3000ms | Shattering Shockwave — bridge destruction variant

    /// <summary>Experiment X-89 (Strain Mauler). Creature2Id 49198.</summary>
    [ScriptFilterCreatureId(49198u)]
    public class GAExperimentX89Script : EncounterBossScript { }

    // ── e411 ─ Phage Maw (Metal Maw) ─────────────────────────────────────────
    // Key abilities from Spell4.tbl:
    //   56517 | 2300ms | Bombardment — primary attack
    //   57344 | 2000ms | Break Chains — initiates harpoon phase (45s channel)
    //   60612 | 3000ms | Detonation Bombs — places proximity bombs
    //   60437 | 2000ms | Laser Blast — targeted beam
    //   60835 |  500ms | Laser Blast Field — persistent field (15s)
    //   57215 |45000ms | Aerial Bombardment — enrage-style wipe mechanic
    //   60442 | 7500ms | Raid Wipe Smash — lethal fallback

    /// <summary>Phage Maw (Metal Maw). Creature2Id 52974.</summary>
    [ScriptFilterCreatureId(52974u)]
    public class GAPhageMawScript : EncounterBossScript { }

    // ── e412 ─ Kuralak the Defiler (Genetic Architect) ───────────────────────
    // Key abilities from Spell4.tbl:
    //   56649 | 5000ms | Chromosome Corruption — stacking debuff
    //   60159 |      — | Contaminate — channeled AoE field (cast)
    //   56589 |      — | DNA Siphon (Tank) — channel on current tank
    //   60446 | 5000ms | Cultivate — summons add
    //   57837 | 6000ms | Putrid Discharge — targeted AoE
    //   60623 | 8000ms | Outbreak — raid-wide disease
    //   60366 | 3000ms | Tainted Ventilation — environmental hazard
    //   57729 | 1500ms | Vileness GTAE — ground-targeted AoE
    //   60397 |100100ms| Spin — phase transition ability

    /// <summary>Kuralak the Defiler (Genetic Architect). Creature2Id 52969.</summary>
    [ScriptFilterCreatureId(52969u)]
    public class GAKuralakTheDefilerScript : EncounterBossScript { }

    // ── e413 ─ Phagetech Prototypes (Four Gho-bots) + Phagetech Guardians ───────
    // "Phagetech Prototypes" is the player-facing encounter name for the Gho-bots fight.
    // Phagetech Guardian C-148 and C-432 are SEPARATE encounters in the same wing.
    // All six creatures (4 bots + 2 guardians) must be defeated; each triggers its own
    // TriggerBossDeath call. GeneticArchivesScript counts all six individually.
    //
    // Key abilities per robot (Spell4.tbl):
    //   Augmentor (54029):
    //     64839 | 6000ms | Drill Telegraph — targeted ground drill
    //     59758 | 1500ms | Summon Repair Bot — spawns healing add
    //   Fabricator (54030):
    //     59709 |12000ms | Destructo Bot Self Destruct — long-fuse suicide bomber
    //     59757 | 1500ms | Summon Destructo Bot — spawns bomb add
    //     66487 | 4500ms | Fabricator Basketball — thrown projectile mechanic
    //   Protector (54031):
    //     59719 | 5000ms | Pulse Waves — series of knockback rings
    //     64842 | 2500ms | Position Swap — teleports target
    //     59707 | 3000ms | Wave Aura — persistent pulsing field (20s, 500ms pulse)
    //   Commander (54032):
    //     59662 | 4500ms | Pound Tank — heavy tank hit
    //     60810 | 6000ms | Forced Production — raid-wide damage ramp

    /// <summary>Gho-bot Phagetech Augmentor. Creature2Id 54029.</summary>
    [ScriptFilterCreatureId(54029u)]
    public class GAGhobotAugmentorScript : EncounterBossScript { }

    /// <summary>Gho-bot Phagetech Fabricator. Creature2Id 54030.</summary>
    [ScriptFilterCreatureId(54030u)]
    public class GAGhobotFabricatorScript : EncounterBossScript { }

    /// <summary>Gho-bot Phagetech Protector. Creature2Id 54031.</summary>
    [ScriptFilterCreatureId(54031u)]
    public class GAGhobotProtectorScript : EncounterBossScript { }

    /// <summary>Gho-bot Phagetech Commander. Creature2Id 54032.</summary>
    [ScriptFilterCreatureId(54032u)]
    public class GAGhobotCommanderScript : EncounterBossScript { }

    // Phagetech Guardian C-148 and C-432 — internal names Probebot #1 and #2.
    // These are separate encounters from the Gho-bots (confirmed via WildStarLogs encounter list).
    // Spell data from [GA] e413 - Phagetech Prototypes tag covers both Probebots and Gho-bots.

    /// <summary>Phagetech Guardian C-148 (Probebot #1). Creature2Id 54055.</summary>
    [ScriptFilterCreatureId(54055u)]
    public class GAPhageGuardianC148Script : EncounterBossScript { }

    /// <summary>Phagetech Guardian C-432 (Probebot #2). Creature2Id 54056.</summary>
    [ScriptFilterCreatureId(54056u)]
    public class GAPhageGuardianC432Script : EncounterBossScript { }

    // ── Phageborn Convergence ─ Five-Member Council ───────────────────────────
    // Player-facing encounter name: "Phageborn Convergence". Internal Spell4 tag: [GA] TMNS.
    // Five Eldan creations. Roles from Spell4.tbl: Leader, DPS, Healer, Controller (+1).
    // All five must die for encounter completion.
    //
    // Key abilities per role (Spell4.tbl):
    //   Leader (role):
    //     58423 | 3000ms | Essence Rot — targeted ally debuff
    //     57232 | 1000ms | Equalize HP — redistributes HP across council
    //     60399 | 2000ms | Piercing Vision — long DoT channel (55s)
    //   DPS (role):
    //     57862 | 1000ms | Leap — closes gap to target
    //     58387 |      — | Shrapnel / Symbiotic Pustules — DoT field (5s, 5s pulse)
    //     57686 |20000ms | MegaCast Gathering Energy — 20s cast enrage
    //     57838 |10000ms | MegaCast Channel — 20s channel wipe
    //   Healer (role):
    //     57623 |  900ms | DOT Cast — applies spreading disease
    //     57412 | 4000ms | HOT PBAE — heal-over-time pulse
    //     57419 | 2200ms | Field PBAE — persistent heal zone
    //     60377 |10000ms | MegaCast Reconstruct Sinew — 20s channel (full raid heal)
    //   Controller (role):
    //     56983 | 2000ms | Foul Scourge — channeled CC (2s cast, 2s channel)
    //     56962 | 2000ms | Time Bomb Tether — attached time bomb
    //     56978 |20000ms | MegaCast Gathering Energy — 20s cast enrage
    //     60349 |20000ms | MegaCast Channel — 20s channel wipe
    //
    // NOTE: Exact creature-to-role mapping requires in-game verification.
    //       IDs sourced from Creature2.tbl "[GA] TMNS" name search.

    /// <summary>TMNS council member (creature 52963). Role unconfirmed.</summary>
    [ScriptFilterCreatureId(52963u)]
    public class GATMNSMember1Script : EncounterBossScript { }

    /// <summary>TMNS council member (creature 52964). Role unconfirmed.</summary>
    [ScriptFilterCreatureId(52964u)]
    public class GATMNSMember2Script : EncounterBossScript { }

    /// <summary>TMNS council member (creature 52968). Role unconfirmed.</summary>
    [ScriptFilterCreatureId(52968u)]
    public class GATMNSMember3Script : EncounterBossScript { }

    /// <summary>TMNS council member (creature 52970). Role unconfirmed.</summary>
    [ScriptFilterCreatureId(52970u)]
    public class GATMNSMember4Script : EncounterBossScript { }

    /// <summary>TMNS council member (creature 52971). Role unconfirmed.</summary>
    [ScriptFilterCreatureId(52971u)]
    public class GATMNSMember5Script : EncounterBossScript { }

    // ── e415 ─ Dreadphage Ohmna ───────────────────────────────────────────────
    // Final boss of Genetic Archives. Three-phase fight.
    // Key abilities from Spell4.tbl:
    //   47359 | 3000ms | Body Slam — targeted slam (tier 1)
    //   72662 | 3000ms | Body Slam — tier 2 variant
    //   75717 | 3000ms | Body Slam Giant Phase — phase 3 (giant form)
    //   47361 | 2000ms | Erupt — targeted ground burst (tier 1)
    //   59764 | 4000ms | Erupt — tier 2 variant
    //   72661 | 2500ms | Erupt — tier 3 variant
    //   47364 | 4000ms | Genetic Torrent — sweeping beam rotation
    //   60933 |  900ms | Corner Prey — tether + chase mechanic
    //   60925 | 5000ms | Devour and Consume — large targeted AoE
    //   59632 |  250ms | Ravage — fast melee cleave
    //   47745 |      — | Sap Power — drain channel (30s field)
    //   47494 |      — | Gene Splice — instant transformation ability
    //   47887 |      — | Moment of Opportunity — phase transition (MOO)

    /// <summary>Dreadphage Ohmna — final boss of Genetic Archives. Creature2Id 49395.</summary>
    [ScriptFilterCreatureId(49395u)]
    public class GADreadphageOhmnaScript : EncounterBossScript { }

    // ── Optional Minibosses ───────────────────────────────────────────────────
    // Optional encounters scattered through the raid wings. Deaths trigger
    // TriggerBossDeath but GeneticArchivesScript ignores them (IDs not in
    // BossCreatureIds). Scripts included for future ability hooks.
    //
    // Genetic Monstrosity key abilities:
    //   69962 |      — | Noxious Belch — poison breath
    //   61378 | 2000ms | Radiation Bath — channeled AoE field
    //   61341 |  500ms | Foul Rupture — quick burst
    //   61357 | 4000ms | Repulsive Cultivation — ground-target AoE

    /// <summary>Miniboss — Genetic Monstrosity. Creature2Id 54968.</summary>
    [ScriptFilterCreatureId(54968u)]
    public class GAGeneticMonstrosityScript : EncounterBossScript { }

    // Gravitron Operator: 56184 is the actual boss entity ("[GA] Miniboss - Act 1 - Gravition - Boss").
    // 56163 is the gravitron machine object ("[GA] Miniboss - Act 1 - Gravition - Gravitron").

    /// <summary>Miniboss — Gravitron Operator (boss entity). Creature2Id 56184.</summary>
    [ScriptFilterCreatureId(56184u)]
    public class GAGravitronOperatorScript : EncounterBossScript { }

    /// <summary>Miniboss — Hideously Malformed Mutant. Creature2Id 56178.</summary>
    [ScriptFilterCreatureId(56178u)]
    public class GAHideouslyMalformedMutantScript : EncounterBossScript { }

    // Fetid Miscreation: internal Creature2 name is "[GA] Miniboss - Act1 - Ravenok" (56377).
    // The player-facing display name "Fetid Miscreation" comes from the localized text table.
    // Confirmed via WildStar Logs combat data: Ravage (62946/62950) matches log output.
    // Key abilities (Spell4 internal → WildStar Logs display name):
    //   62947 |20000ms | Genetic Decomposition (Must Interrupt) — long-cast interrupt mechanic
    //   62946 |  3500ms | Ravage — targeted proxy attack
    //   62950 |      — | Ravage Base — auto-attack variant
    //   70294 |      — | Auto-Attack #1 (Swipe in combat log)
    //   70295 |      — | Auto-Attack #2 (Tear in combat log)
    //   62940 |      — | Stacking Buff
    //   62943 |      — | Stacking Debuff

    /// <summary>Miniboss — Fetid Miscreation (internal: Ravenok). Creature2Id 56377.</summary>
    [ScriptFilterCreatureId(56377u)]
    public class GAFetidMiscreationScript : EncounterBossScript { }

    // Guardian East/West — two additional optional guardian minibosses found in Creature2.tbl.
    // "[GA] Miniboss - Guardian - East" (54785) and "[GA] Miniboss - Guardian - West" (54787).
    // Relationship to Phagetech Guardian C-148/C-432 requires in-game verification.

    /// <summary>Miniboss — Guardian East. Creature2Id 54785.</summary>
    [ScriptFilterCreatureId(54785u)]
    public class GAGuardianEastScript : EncounterBossScript { }

    /// <summary>Miniboss — Guardian West. Creature2Id 54787.</summary>
    [ScriptFilterCreatureId(54787u)]
    public class GAGuardianWestScript : EncounterBossScript { }

    /// <summary>Miniboss — Malfunctioning Battery. Creature2Id 56174.</summary>
    [ScriptFilterCreatureId(56174u)]
    public class GAMalfunctioningBatteryScript : EncounterBossScript { }

    /// <summary>Miniboss — Malfunctioning Dynamo. Creature2Id 54935.</summary>
    [ScriptFilterCreatureId(54935u)]
    public class GAMalfunctioningDynamoScript : EncounterBossScript { }

    /// <summary>Miniboss — Malfunctioning Piston. Creature2Id 56106.</summary>
    [ScriptFilterCreatureId(56106u)]
    public class GAMalfunctioningPistonScript : EncounterBossScript { }

    /// <summary>Miniboss — Malfunctioning Gear. Creature2Id 55066.</summary>
    [ScriptFilterCreatureId(55066u)]
    public class GAMalfunctioningGearScript : EncounterBossScript { }
}
