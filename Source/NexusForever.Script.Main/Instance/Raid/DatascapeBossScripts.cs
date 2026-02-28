using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Raid
{
    // Datascape (DataScape, WorldId 1333) — Boss Encounter Scripts
    // Source: Spell4.tbl "[DS] eXXX" name tag search.
    //
    // On death each script calls IContentMapInstance.TriggerBossDeath via EncounterBossScript.
    // DatascapeScript.OnBossDeath tracks kills and fires MatchFinish after all 11 required deaths.
    //
    // All spell IDs sourced from Spell4.tbl "[DS] eXXX" tagged entries.
    // Rotation intervals are best-effort approximations; tune from sniff data once available.
    // Enrage timers default to 10 minutes (600s) — the DS standard enrage window.

    // ── e385 ─ System Daemons ─────────────────────────────────────────────────
    //
    // Dual-boss encounter: Null System Daemon and Binary System Daemon.
    // They share the Eldan Sentinels spell family. Key mechanics:
    //   Syncing/Synchronized — bosses link up and share damage
    //   Upload/Download     — data transfer phase between bosses
    //   Power Surge         — interruptible channel, Overload if interrupted
    //   Meltdown            — raid-wide damage (escalating tiers)
    //   Purge               — targeted cleanse/dispel
    //   Disconnect          — removes a player from combat temporarily
    //   Memory Wipe         — raid-wide threat wipe
    //
    //   43011 | 3000ms | Power Surge
    //   43370 | 4500ms | Memory Wipe
    //   43377 | 5200ms | Disconnect (Sentinel)
    //   43477 | 7000ms | Download
    //   43478 | 7000ms | Upload
    //   43583 | 3500ms | Meltdown T1
    //   43889 | 5000ms | Laser
    //   44006 | 2000ms | Purge
    //   64821 | 3000ms | Digitize

    /// <summary>System Daemons — Null Boss. Creature2Id 30495.</summary>
    [ScriptFilterCreatureId(30495u)]
    public class DSSystemDaemonsNullScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 43889, initialDelay:  3.0, interval: 10.0); // Laser
            ScheduleSpell(spell4Id: 44006, initialDelay:  6.0, interval: 14.0); // Purge
            ScheduleSpell(spell4Id: 43011, initialDelay: 10.0, interval: 20.0); // Power Surge
            ScheduleSpell(spell4Id: 43583, initialDelay: 18.0, interval: 25.0); // Meltdown T1
            ScheduleSpell(spell4Id: 43477, initialDelay: 30.0, interval: 45.0); // Download

            AddPhase(healthPct: 50f, OnPhase2);

            SetEnrage(seconds: 600.0, enrageSpellId: 43583);
        }

        private void OnPhase2()
        {
            ScheduleSpell(spell4Id: 64821, initialDelay:  3.0, interval: 16.0); // Digitize
            ScheduleSpell(spell4Id: 43370, initialDelay: 10.0, interval: 30.0); // Memory Wipe
        }
    }

    /// <summary>System Daemons — Binary Boss. Creature2Id 30496.</summary>
    [ScriptFilterCreatureId(30496u)]
    public class DSSystemDaemonsBinaryScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 43889, initialDelay:  4.0, interval: 10.0); // Laser
            ScheduleSpell(spell4Id: 44006, initialDelay:  8.0, interval: 14.0); // Purge
            ScheduleSpell(spell4Id: 43011, initialDelay: 12.0, interval: 20.0); // Power Surge
            ScheduleSpell(spell4Id: 43583, initialDelay: 20.0, interval: 25.0); // Meltdown T1
            ScheduleSpell(spell4Id: 43478, initialDelay: 32.0, interval: 45.0); // Upload

            AddPhase(healthPct: 50f, OnPhase2);

            SetEnrage(seconds: 600.0, enrageSpellId: 43583);
        }

        private void OnPhase2()
        {
            ScheduleSpell(spell4Id: 64821, initialDelay:  5.0, interval: 16.0); // Digitize
            ScheduleSpell(spell4Id: 43377, initialDelay: 12.0, interval: 28.0); // Disconnect
        }
    }

    // ── e390 ─ Maelstrom Authority ────────────────────────────────────────────
    //
    // Weather-phase encounter. Maelstrom Authority cycles through weather
    // patterns that change the arena. Key mechanics:
    //   Galeforce        — targeted knockback beam
    //   Typhoon          — large AoE knockback + slow fall
    //   Static Bombshell — ground-targeted stun
    //   Wind Wall        — rotating wall of wind (8s cast)
    //   Ice Breath       — channeled frontal cone (20s)
    //   Lightning Rod    — long-duration chase telegraph (20s)
    //   Shifting Currents — arena repositioning (intro + leap)
    //   Shatter          — ice-phase burst AoE
    //
    //   44414 | 1750ms | Galeforce
    //   44392 | 2000ms | Static Bombshell
    //   44525 | 3000ms | Typhoon
    //   45505 | 1500ms | Shifting Currents
    //   78322 | 8000ms | Wind Wall
    //   45503 | 8000ms | Shatter
    //   44510 |20000ms | Ice Breath (channel)
    //   44323 |20000ms | Lightning Rod (channel)

    /// <summary>Maelstrom Authority — Air Boss. Creature2Id 30497.</summary>
    [ScriptFilterCreatureId(30497u)]
    public class DSMaelstromAuthorityScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            // Phase 1 — Wind phase
            ScheduleSpell(spell4Id: 44414, initialDelay:  3.0, interval:  8.0); // Galeforce
            ScheduleSpell(spell4Id: 44392, initialDelay:  6.0, interval: 12.0); // Static Bombshell
            ScheduleSpell(spell4Id: 44525, initialDelay: 12.0, interval: 20.0); // Typhoon
            ScheduleSpell(spell4Id: 78322, initialDelay: 20.0, interval: 35.0); // Wind Wall
            ScheduleSpell(spell4Id: 45505, initialDelay: 28.0, interval: 25.0); // Shifting Currents

            // Phase 2 — Ice phase at 65%
            AddPhase(healthPct: 65f, OnIcePhase);
            // Phase 3 — Storm phase at 30%
            AddPhase(healthPct: 30f, OnStormPhase);

            SetEnrage(seconds: 600.0, enrageSpellId: 44525);
        }

        private void OnIcePhase()
        {
            ScheduleSpell(spell4Id: 44510, initialDelay:  5.0, interval: 40.0); // Ice Breath (channel)
            ScheduleSpell(spell4Id: 45503, initialDelay: 15.0, interval: 30.0); // Shatter
        }

        private void OnStormPhase()
        {
            ScheduleSpell(spell4Id: 44323, initialDelay:  5.0, interval: 35.0); // Lightning Rod (channel)
        }
    }

    // ── e393 ─ Gloomclaw ──────────────────────────────────────────────────────
    //
    // Corruption-themed multi-phase encounter. Gloomclaw alternates between
    // physical melee and corruption phases. Key mechanics:
    //   Corrupting Smash     — heavy frontal cone
    //   AE Fear              — raid-wide fear
    //   Corruption Pool      — targeted ground AoE
    //   Fear Base            — escalating fear (T1–T4 tiers)
    //   Rupture              — large AoE burst
    //   Line AE              — sweeping line telegraph
    //   Burrow Move          — repositioning burrow (8.5s)
    //   Manifest Corruption  — large delayed explosion
    //
    //   34769 | 1500ms | Corrupting Smash
    //   44018 | 1000ms | AE Fear
    //   44043 | 1500ms | Line AE
    //   44171 | 4000ms | Rupture
    //   44281 | 8500ms | Burrow Move
    //   44326 | 2000ms | Corruption Pool
    //   44375 | 1500ms | Fear Base T1
    //   44442 |10000ms | Manifest Corruption - Explode

    /// <summary>Gloomclaw. Creature2Id 30498.</summary>
    [ScriptFilterCreatureId(30498u)]
    public class DSGloomclawScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            // Primary rotation
            ScheduleSpell(spell4Id: 34769, initialDelay:  3.0, interval:  8.0); // Corrupting Smash
            ScheduleSpell(spell4Id: 44043, initialDelay:  6.0, interval: 12.0); // Line AE
            ScheduleSpell(spell4Id: 44326, initialDelay: 10.0, interval: 16.0); // Corruption Pool
            ScheduleSpell(spell4Id: 44375, initialDelay: 15.0, interval: 22.0); // Fear Base T1
            ScheduleSpell(spell4Id: 44171, initialDelay: 22.0, interval: 30.0); // Rupture

            // Phase 2 — Corruption intensifies at 60%
            AddPhase(healthPct: 60f, OnPhase2);
            // Phase 3 — Full corruption at 25%
            AddPhase(healthPct: 25f, OnPhase3);

            SetEnrage(seconds: 600.0, enrageSpellId: 44171);
        }

        private void OnPhase2()
        {
            ScheduleSpell(spell4Id: 44018, initialDelay:  3.0, interval: 20.0); // AE Fear
            ScheduleSpell(spell4Id: 44281, initialDelay: 10.0, interval: 35.0); // Burrow Move
        }

        private void OnPhase3()
        {
            ScheduleSpell(spell4Id: 44442, initialDelay:  5.0, interval: 40.0); // Manifest Corruption Explode
        }
    }

    // ── e395 ─ Elemental Bosses ───────────────────────────────────────────────
    // Six elemental boss encounters in the Datascape. All six must be defeated.
    // Elementals fight in pairs with combo mechanics. Each has element-specific
    // abilities plus paired interaction spells.

    // ── Earth Elemental ──────────────────────────────────────────────────────
    //   49884 |  700ms | Fierce Swipe 1
    //   50079 |  800ms | Rock Smash
    //   53076 | 2000ms | Superquake
    //   73208 |  500ms | Tectonic Steps - PBAE
    //   74468 | 1000ms | Gronyx Adds - Proxy

    /// <summary>Earth Elemental Boss. Creature2Id 30499.</summary>
    [ScriptFilterCreatureId(30499u)]
    public class DSEarthElementalScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 49884, initialDelay:  2.0, interval:  5.0); // Fierce Swipe
            ScheduleSpell(spell4Id: 50079, initialDelay:  4.0, interval:  9.0); // Rock Smash
            ScheduleSpell(spell4Id: 73208, initialDelay:  8.0, interval: 14.0); // Tectonic Steps
            ScheduleSpell(spell4Id: 53076, initialDelay: 15.0, interval: 22.0); // Superquake
            ScheduleSpell(spell4Id: 74468, initialDelay: 25.0, interval: 35.0); // Gronyx Adds

            AddPhase(healthPct: 40f, OnEnrage);
            SetEnrage(seconds: 600.0, enrageSpellId: 53076);
        }

        private void OnEnrage()
        {
            ScheduleSpell(spell4Id: 73205, initialDelay: 2.0, interval: 15.0); // Superquake Raw Power
        }
    }

    // ── Water Elemental ──────────────────────────────────────────────────────
    //   46968 | 5000ms | Sinkhole
    //   70781 | 2000ms | Sinkhole - Single
    //   47068 | 2000ms | Geyser - Tracking Proxy
    //   52530 |    0ms | Freezing Cleave (auto proxy)
    //   53192 | 1500ms | Wet Wipe - Threat Wipe

    /// <summary>Water Elemental Boss. Creature2Id 30500.</summary>
    [ScriptFilterCreatureId(30500u)]
    public class DSWaterElementalScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 47068, initialDelay:  3.0, interval:  8.0); // Geyser Tracking
            ScheduleSpell(spell4Id: 70781, initialDelay:  6.0, interval: 12.0); // Sinkhole Single
            ScheduleSpell(spell4Id: 46968, initialDelay: 15.0, interval: 25.0); // Sinkhole
            ScheduleSpell(spell4Id: 53192, initialDelay: 20.0, interval: 30.0); // Wet Wipe

            AddPhase(healthPct: 40f, OnTsunami);
            SetEnrage(seconds: 600.0, enrageSpellId: 46968);
        }

        private void OnTsunami()
        {
            ScheduleSpell(spell4Id: 72101, initialDelay: 5.0, interval: 45.0); // Tsunami Base
        }
    }

    // ── Life Elemental ──────────────────────────────────────────────────────
    //   48518 | 1500ms | Orbzzz - Base
    //   47593 | 8500ms | Blind Rotating Cones
    //   54036 |35000ms | Signal Annihilation Orbs (long channel)
    //   74619 | 1500ms | Healing Aura
    //   73176 | 3000ms | Area Heal - Aura

    /// <summary>Life Elemental Boss. Creature2Id 30501.</summary>
    [ScriptFilterCreatureId(30501u)]
    public class DSLifeElementalScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 48518, initialDelay:  3.0, interval:  8.0); // Orbzzz
            ScheduleSpell(spell4Id: 74619, initialDelay:  7.0, interval: 14.0); // Healing Aura
            ScheduleSpell(spell4Id: 47593, initialDelay: 12.0, interval: 20.0); // Blind Rotating Cones
            ScheduleSpell(spell4Id: 73176, initialDelay: 18.0, interval: 25.0); // Area Heal

            AddPhase(healthPct: 40f, OnLowHealth);
            SetEnrage(seconds: 600.0, enrageSpellId: 47593);
        }

        private void OnLowHealth()
        {
            ScheduleSpell(spell4Id: 54036, initialDelay: 5.0, interval: 60.0); // Annihilation Orbs
        }
    }

    // ── Air Elemental ──────────────────────────────────────────────────────
    //   73244 |  500ms | Galeforce
    //   74383 | 3000ms | Tempest - Cast
    //   83232 | 2500ms | Supercell - Cast
    //   46874 |  750ms | Walls of Wind
    //   74429 |10000ms | Lightning Strike - Tracker
    //   70440 | 2000ms | Twirl

    /// <summary>Air Elemental Boss. Creature2Id 30502.</summary>
    [ScriptFilterCreatureId(30502u)]
    public class DSAirElementalScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 73244, initialDelay:  2.0, interval:  6.0); // Galeforce
            ScheduleSpell(spell4Id: 46874, initialDelay:  5.0, interval: 10.0); // Walls of Wind
            ScheduleSpell(spell4Id: 74383, initialDelay: 10.0, interval: 18.0); // Tempest
            ScheduleSpell(spell4Id: 70440, initialDelay: 16.0, interval: 22.0); // Twirl
            ScheduleSpell(spell4Id: 83232, initialDelay: 24.0, interval: 30.0); // Supercell

            AddPhase(healthPct: 40f, OnStormPhase);
            SetEnrage(seconds: 600.0, enrageSpellId: 74383);
        }

        private void OnStormPhase()
        {
            ScheduleSpell(spell4Id: 74429, initialDelay: 5.0, interval: 25.0); // Lightning Strike
        }
    }

    // ── Fire Elemental ──────────────────────────────────────────────────────
    //   69900 |  500ms | Eruption
    //   49869 | 1500ms | Flame Wave
    //   70702 | 3000ms | Meteor (AOE)
    //   54214 | 2000ms | Lava Pool (AOE)
    //   50234 | 2500ms | Lava Mine Trigger
    //   53196 | 1500ms | Funeral Pyre - Threat Wipe

    /// <summary>Fire Elemental Boss. Creature2Id 30503.</summary>
    [ScriptFilterCreatureId(30503u)]
    public class DSFireElementalScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 69900, initialDelay:  2.0, interval:  5.0); // Eruption
            ScheduleSpell(spell4Id: 49869, initialDelay:  5.0, interval: 10.0); // Flame Wave
            ScheduleSpell(spell4Id: 54214, initialDelay:  9.0, interval: 16.0); // Lava Pool
            ScheduleSpell(spell4Id: 70702, initialDelay: 14.0, interval: 22.0); // Meteor
            ScheduleSpell(spell4Id: 53196, initialDelay: 20.0, interval: 28.0); // Funeral Pyre

            AddPhase(healthPct: 40f, OnRagnarok);
            SetEnrage(seconds: 600.0, enrageSpellId: 70702);
        }

        private void OnRagnarok()
        {
            ScheduleSpell(spell4Id: 49890, initialDelay: 3.0, interval: 45.0); // Ragnarok Lava Floor
            ScheduleSpell(spell4Id: 50234, initialDelay: 8.0, interval: 18.0); // Lava Mine Trigger
        }
    }

    // ── Logic Elemental ──────────────────────────────────────────────────────
    //   52006 | 1000ms | Spread Out and Explode
    //   52483 | 9000ms | Slow Field Missile - Multiple
    //   51745 | 3000ms | Destructible Prison
    //   70001 | 8000ms | Defragment - Puzzle Blocks
    //   47544 |  600ms | Snake Make - Vibromatic Synthesis

    /// <summary>Logic Elemental Boss. Creature2Id 30504.</summary>
    [ScriptFilterCreatureId(30504u)]
    public class DSLogicElementalScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 47544, initialDelay:  3.0, interval:  7.0); // Vibromatic Synthesis
            ScheduleSpell(spell4Id: 52006, initialDelay:  6.0, interval: 12.0); // Spread Out and Explode
            ScheduleSpell(spell4Id: 51745, initialDelay: 14.0, interval: 22.0); // Destructible Prison
            ScheduleSpell(spell4Id: 52483, initialDelay: 22.0, interval: 30.0); // Slow Field Missile

            AddPhase(healthPct: 40f, OnPuzzlePhase);
            SetEnrage(seconds: 600.0, enrageSpellId: 52006);
        }

        private void OnPuzzlePhase()
        {
            ScheduleSpell(spell4Id: 70001, initialDelay: 5.0, interval: 35.0); // Defragment Puzzle
        }
    }

    // ── e399 ─ Avatus — Final Boss ────────────────────────────────────────────
    //
    // Multi-phase encounter with Holo Hands, Gun Grids, and Danger Rooms.
    // Three main phases plus intermission sub-phases:
    //   Phase 1 (100–65%): Showdown — melee + Holo Hand attacks
    //   Intermission 1:    Soldier/Explorer/Settler/Scientist challenges
    //   Phase 2 (65–35%):  Gun Grid — ranged bombardment phase
    //   Intermission 2:    Harder path challenges
    //   Phase 3 (35–0%):   Final confrontation — all abilities
    //
    // Key Avatus direct abilities:
    //   44785 | 2800ms | Annihilation - Double Cannon - Tank Spike
    //   47186 |12000ms | Convergent Force (ring mechanic)
    //   45897 | 4000ms | Obliteration Beam - Initial Cast
    //   45371 | 1800ms | Phase Punch
    //   69858 | 3300ms | Phase Punch 2 - Targeted AOE
    //   44537 | 2000ms | Holo Hand - Crushing Blow
    //   44519 | 2000ms | Holo Hand - Strikethrough
    //   44473 |  400ms | Holo Hand - Electric Field
    //   44799 | 2000ms | Charged Beam - Gun Grid
    //   45162 | 2000ms | Energized Bombardment
    //   44913 | 1000ms | Invalidate - One Shot

    /// <summary>Avatus — final boss of Datascape. Creature2Id 30505.</summary>
    [ScriptFilterCreatureId(30505u)]
    public class DSAvatusScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            // Phase 1 — Showdown: melee + Holo Hand attacks
            ScheduleSpell(spell4Id: 44537, initialDelay:  3.0, interval:  8.0); // Crushing Blow
            ScheduleSpell(spell4Id: 44519, initialDelay:  6.0, interval: 12.0); // Strikethrough
            ScheduleSpell(spell4Id: 44473, initialDelay: 10.0, interval: 16.0); // Electric Field
            ScheduleSpell(spell4Id: 45371, initialDelay: 15.0, interval: 18.0); // Phase Punch
            ScheduleSpell(spell4Id: 44785, initialDelay: 20.0, interval: 24.0); // Annihilation (tank spike)

            // Phase 2 — Gun Grid at 65%
            AddPhase(healthPct: 65f, OnGunGridPhase);
            // Phase 3 — Final confrontation at 35%
            AddPhase(healthPct: 35f, OnFinalPhase);

            SetEnrage(seconds: 720.0, enrageSpellId: 44913); // 12-min enrage for final boss
        }

        private void OnGunGridPhase()
        {
            ScheduleSpell(spell4Id: 44799, initialDelay:  3.0, interval: 14.0); // Charged Beam
            ScheduleSpell(spell4Id: 45162, initialDelay:  8.0, interval: 18.0); // Energized Bombardment
            ScheduleSpell(spell4Id: 69858, initialDelay: 14.0, interval: 22.0); // Phase Punch 2
        }

        private void OnFinalPhase()
        {
            ScheduleSpell(spell4Id: 47186, initialDelay:  5.0, interval: 35.0); // Convergent Force
            ScheduleSpell(spell4Id: 45897, initialDelay: 12.0, interval: 28.0); // Obliteration Beam
            ScheduleSpell(spell4Id: 44913, initialDelay: 20.0, interval: 40.0); // Invalidate One Shot
        }
    }
}
