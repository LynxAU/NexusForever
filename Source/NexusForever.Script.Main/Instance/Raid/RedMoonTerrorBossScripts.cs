using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Raid
{
    // Red Moon Terror (WorldId 3032 / 3102) — Boss Encounter Scripts
    // Source: Spell4.tbl "[RMT]" name tag search.
    //
    // These scripts are shared between the 20-man (WorldId 3032) and 40-man (WorldId 3102)
    // raids if the same creature IDs are used for both difficulties. If 40-man uses separate
    // creature IDs, additional scripts will be needed.
    //
    // All spell IDs sourced from Spell4.tbl "[RMT]" tagged entries.
    // Rotation intervals are best-effort approximations; tune from sniff data once available.

    // ── e4550 ─ Engineers ─────────────────────────────────────────────────────
    //
    // Dual-boss encounter: Hammer and Gun engineers.
    //   75240 | 3000ms | Gun - Blast - Base
    //   75315 |  500ms | Gun - Quick Shot (AA)
    //   75313 |  600ms | Hammer - Crush AA 1
    //   75314 |  733ms | Hammer - Crush AA 2
    //   84012 |10000ms | Jumpstart (shared channel)

    /// <summary>RMT Engineer — Hammer. Creature2Id 65758.</summary>
    [ScriptFilterCreatureId(65758u)]
    public class RMTEngineerHammerScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 75313, initialDelay:  2.0, interval:  5.0); // Crush AA 1
            ScheduleSpell(spell4Id: 75314, initialDelay:  4.0, interval:  5.0); // Crush AA 2
            ScheduleSpell(spell4Id: 84012, initialDelay: 20.0, interval: 40.0); // Jumpstart

            SetEnrage(seconds: 600.0, enrageSpellId: 84012);
        }
    }

    /// <summary>RMT Engineer — Gun. Creature2Id 65759.</summary>
    [ScriptFilterCreatureId(65759u)]
    public class RMTEngineerGunScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 75315, initialDelay:  2.0, interval:  4.0); // Quick Shot AA
            ScheduleSpell(spell4Id: 75240, initialDelay:  5.0, interval: 10.0); // Blast
            ScheduleSpell(spell4Id: 84012, initialDelay: 25.0, interval: 40.0); // Jumpstart

            SetEnrage(seconds: 600.0, enrageSpellId: 75240);
        }
    }

    // ── e4554 ─ Mordechai Redmoon ─────────────────────────────────────────────
    //
    // Pirate captain with turret mechanics and kinetic orb phase.
    //   75359 | 1900ms | Auto Attack #1 - Front/Back
    //   85575 | 3000ms | Kinetic Orb - Scale Up
    //   85543 |10000ms | MoO (Moment of Opportunity)
    //   75701 | 2000ms | Turret - Kinetic Discharge
    //   85622 |10500ms | Vicious Barrage

    /// <summary>Mordechai Redmoon. Creature2Id 65800.</summary>
    [ScriptFilterCreatureId(65800u)]
    public class RMTMordechaiRedmoonScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 75359, initialDelay:  2.0, interval:  5.0); // Auto Attack
            ScheduleSpell(spell4Id: 75701, initialDelay:  6.0, interval: 14.0); // Kinetic Discharge
            ScheduleSpell(spell4Id: 85575, initialDelay: 12.0, interval: 20.0); // Kinetic Orb
            ScheduleSpell(spell4Id: 85622, initialDelay: 25.0, interval: 35.0); // Vicious Barrage

            AddPhase(healthPct: 50f, OnPhase2);

            SetEnrage(seconds: 600.0, enrageSpellId: 85622);
        }

        private void OnPhase2()
        {
            ScheduleSpell(spell4Id: 85543, initialDelay: 5.0, interval: 45.0); // MoO
        }
    }

    // ── Laveka ────────────────────────────────────────────────────────────────
    //
    // Necromancer boss with dead realm / live realm phase mechanics.
    //   75439 |  400ms | Auto-Attack
    //   Laveka spells are mostly aura/proxy based (Soul Rip, Essence Void, etc.)
    //   No direct cast-time attacks found beyond auto-attack.
    //   Phase mechanics: Dead Realm / Live Realm transitions at health thresholds.

    /// <summary>Laveka. Creature2Id 65997.</summary>
    [ScriptFilterCreatureId(65997u)]
    public class RMTLavekaScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 75439, initialDelay: 2.0, interval: 3.0); // Auto-Attack

            SetEnrage(seconds: 600.0, enrageSpellId: 75439);
        }
    }

    // ── Robomination ─────────────────────────────────────────────────────────
    //
    // Multi-part construct with cannon arm, flail arm, and body segments.
    //   75618 | 2000ms | Laser - Base
    //   83821 |  433ms | Body Auto-Attack #1
    //   83822 |  433ms | Body Auto-Attack #2
    //   83817 |  533ms | Cannon Arm Auto-Attack #1
    //   83819 |  900ms | Flail Arm Auto-Attack #1
    //   Compactor mechanic: Slow Crush / Quick Crush / Safe Zone (all 0ms proxies)

    /// <summary>Robomination. Creature2Id 66085.</summary>
    [ScriptFilterCreatureId(66085u)]
    public class RMTRobominationScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 83821, initialDelay: 2.0, interval: 4.0); // Body Auto #1
            ScheduleSpell(spell4Id: 83822, initialDelay: 3.0, interval: 4.0); // Body Auto #2
            ScheduleSpell(spell4Id: 75618, initialDelay: 6.0, interval: 12.0); // Laser

            SetEnrage(seconds: 600.0, enrageSpellId: 75618);
        }
    }
}
