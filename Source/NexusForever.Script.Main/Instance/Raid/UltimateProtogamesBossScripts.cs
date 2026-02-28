using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Raid
{
    // Ultimate Protogames (WorldId 3041) — Boss Encounter Scripts
    // Source: Spell4.tbl "[UP]" name tag search.
    //
    // Despite the name, Ultimate Protogames is classified as a Dungeon (context=2)
    // with ilvl 80 Superb loot. Two main bosses + two miniboss encounters.
    //
    // All spell IDs sourced from Spell4.tbl "[UP] e26XX" tagged entries.
    // Rotation intervals are best-effort approximations; tune from sniff data once available.

    // ── e2675 ─ Hut-Hut (Football Boss) ─────────────────────────────────────────
    //
    // Football-themed Gorganoth boss with tackle and fumble mechanics.
    //   71107 |  auto  | Auto-Attack #1 L2R
    //   71108 |  auto  | Auto-Attack #2 R2L
    //   71240 |  cast  | Tackle
    //   72746 |  cast  | Fumble Random
    //   72072 |  cast  | Self Destruct - Slow
    //   72695 |  cast  | Absorb Shield
    //   75724 |  cast  | Extra Point Pull To Location
    //   71709 |  cast  | Celebration Dance (enrage flavor)

    /// <summary>Hut-Hut — Football Boss encounter. Creature2Id 61417.</summary>
    [ScriptFilterCreatureId(61417u)]
    public class UPHutHutScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 71107, initialDelay:  2.0, interval:  4.0); // Auto-Attack L2R
            ScheduleSpell(spell4Id: 71108, initialDelay:  3.5, interval:  4.0); // Auto-Attack R2L
            ScheduleSpell(spell4Id: 71240, initialDelay:  8.0, interval: 12.0); // Tackle
            ScheduleSpell(spell4Id: 72695, initialDelay: 15.0, interval: 30.0); // Absorb Shield
            ScheduleSpell(spell4Id: 72746, initialDelay: 20.0, interval: 25.0); // Fumble Random

            AddPhase(healthPct: 30f, OnDesperation);

            SetEnrage(seconds: 480.0, enrageSpellId: 72072); // Self Destruct - Slow
        }

        private void OnDesperation()
        {
            ScheduleSpell(spell4Id: 75724, initialDelay: 5.0, interval: 20.0); // Extra Point Pull
        }
    }

    // ── e2680 ─ Bev-O-Rage (Vending Machine Boss) ───────────────────────────────
    //
    // Rogue vending machine with spark attacks, barrel roll, and drink mechanics.
    //   71196 |  auto  | Auto-Attack - Stir
    //   71197 |  auto  | Auto-Attack - Shake
    //   71110 |  cast  | Spark - Base
    //   71111 |  cast  | Lazer Spark - Base
    //   71182 |  cast  | Tri Burst - Base
    //   71221 |  cast  | Barrel Roll - Base
    //   71512 |  cast  | Beverage Barrage - Base
    //   71294 |  cast  | Drink - Base (heal/buff phase)
    //   72390 |  cast  | Steam Blast - Base
    //   71285 |  cast  | MoO - Base (Moment of Opportunity)
    //   75110 |  cast  | Carbonated - Base

    /// <summary>Bev-O-Rage — Vending Machine Boss encounter. Creature2Id 61463.</summary>
    [ScriptFilterCreatureId(61463u)]
    public class UPBevORageScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 71196, initialDelay:  2.0, interval:  5.0); // Auto-Attack Stir
            ScheduleSpell(spell4Id: 71197, initialDelay:  3.5, interval:  5.0); // Auto-Attack Shake
            ScheduleSpell(spell4Id: 71110, initialDelay:  6.0, interval: 10.0); // Spark
            ScheduleSpell(spell4Id: 71111, initialDelay: 10.0, interval: 14.0); // Lazer Spark
            ScheduleSpell(spell4Id: 71182, initialDelay: 14.0, interval: 16.0); // Tri Burst
            ScheduleSpell(spell4Id: 71221, initialDelay: 20.0, interval: 25.0); // Barrel Roll
            ScheduleSpell(spell4Id: 71512, initialDelay: 30.0, interval: 35.0); // Beverage Barrage

            AddPhase(healthPct: 50f, OnDrinkPhase);
            AddPhase(healthPct: 25f, OnOvercharge);

            SetEnrage(seconds: 480.0, enrageSpellId: 72390); // Steam Blast
        }

        private void OnDrinkPhase()
        {
            ScheduleSpell(spell4Id: 71294, initialDelay: 5.0, interval: 40.0); // Drink
            ScheduleSpell(spell4Id: 72390, initialDelay: 8.0, interval: 20.0); // Steam Blast
        }

        private void OnOvercharge()
        {
            ScheduleSpell(spell4Id: 75110, initialDelay: 3.0, interval: 18.0); // Carbonated
            ScheduleSpell(spell4Id: 71285, initialDelay: 10.0, interval: 45.0); // MoO
        }
    }

    // ── Optional Minibosses ─────────────────────────────────────────────────────

    /// <summary>Miniboss — Crate Destruction (e2673). Creature2Id 62575.</summary>
    [ScriptFilterCreatureId(62575u)]
    public class UPCrateDestructionScript : EncounterBossScript { }

    /// <summary>Miniboss — Mixed Wave (e2674). Creature2Id 63319.</summary>
    [ScriptFilterCreatureId(63319u)]
    public class UPMixedWaveScript : EncounterBossScript { }
}
