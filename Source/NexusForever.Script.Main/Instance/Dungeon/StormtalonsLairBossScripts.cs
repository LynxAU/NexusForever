using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    // Stormtalon's Lair (EthnDunon, WorldId 382) — Boss Encounter Scripts
    // Spell IDs sourced from Jabbithole (zone 19).
    // Both Normal and Veteran variants share the same spell rotation base class.

    // ── Blade-Wind the Invoker ──────────────────────────────────────────────────
    //   40599 | Shock             — primary melee hit
    //   38818 | Arcane Bolt       — ranged attack
    //   39074 | Electrostatic Pulse — interrupt / CC burst
    //   39935 | Lightning Strike  — heavy targeted strike
    //   63231 | Thunder Cross     — cross-shaped AoE telegraph

    public abstract class StormtalonsLairInvokerBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 40599, initialDelay:  2.0, interval:  6.0); // Shock
            ScheduleSpell(spell4Id: 38818, initialDelay:  5.0, interval:  8.0); // Arcane Bolt
            ScheduleSpell(spell4Id: 63231, initialDelay:  8.0, interval: 14.0); // Thunder Cross
            ScheduleSpell(spell4Id: 39935, initialDelay: 14.0, interval: 18.0); // Lightning Strike
            ScheduleSpell(spell4Id: 39074, initialDelay: 20.0, interval: 22.0); // Electrostatic Pulse

            AddPhase(healthPct: 40f, OnLowHealth);
            SetEnrage(seconds: 420.0, enrageSpellId: 39935);
        }

        private void OnLowHealth()
        {
            ScheduleSpell(spell4Id: 63231, initialDelay: 2.0, interval: 10.0); // Thunder Cross (faster)
        }
    }

    /// <summary>Blade-Wind the Invoker — Normal. Creature2Id 17160.</summary>
    [ScriptFilterCreatureId(17160u)]
    public class StormtalonsLairInvokerNScript : StormtalonsLairInvokerBase { }

    /// <summary>Blade-Wind the Invoker — Veteran. Creature2Id 33405.</summary>
    [ScriptFilterCreatureId(33405u)]
    public class StormtalonsLairInvokerVScript : StormtalonsLairInvokerBase { }

    // ── Aethros ─────────────────────────────────────────────────────────────────
    //   70404 | Swing       — primary melee auto
    //   70405 | Swipe       — frontal cleave
    //   39662 | Thunderbolt — heavy ranged hit
    //   36042 | Torrent     — channeled cone
    //   39682 | Tempest     — AoE storm

    public abstract class StormtalonsLairAethrosBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 70404, initialDelay:  2.0, interval:  5.0); // Swing
            ScheduleSpell(spell4Id: 70405, initialDelay:  4.0, interval:  8.0); // Swipe
            ScheduleSpell(spell4Id: 39662, initialDelay:  7.0, interval: 12.0); // Thunderbolt
            ScheduleSpell(spell4Id: 36042, initialDelay: 12.0, interval: 18.0); // Torrent
            ScheduleSpell(spell4Id: 39682, initialDelay: 18.0, interval: 24.0); // Tempest

            AddPhase(healthPct: 50f, OnStormPhase);
            SetEnrage(seconds: 420.0, enrageSpellId: 39682);
        }

        private void OnStormPhase()
        {
            ScheduleSpell(spell4Id: 39682, initialDelay: 3.0, interval: 16.0); // Tempest (faster)
        }
    }

    /// <summary>Aethros — Normal. Creature2Id 17166.</summary>
    [ScriptFilterCreatureId(17166u)]
    public class StormtalonsLairAethrosNScript : StormtalonsLairAethrosBase { }

    /// <summary>Aethros — Veteran. Creature2Id 32703.</summary>
    [ScriptFilterCreatureId(32703u)]
    public class StormtalonsLairAethrosVScript : StormtalonsLairAethrosBase { }

    // ── Minibosses (no Jabbithole spell data) ───────────────────────────────────

    /// <summary>Arcanist Breeze-Binder — miniboss, Normal. Creature2Id 24474.</summary>
    [ScriptFilterCreatureId(24474u)]
    public class StormtalonsLairBreezebinderNScript : EncounterBossScript { }

    /// <summary>Arcanist Breeze-Binder — miniboss, Veteran. Creature2Id 34711.</summary>
    [ScriptFilterCreatureId(34711u)]
    public class StormtalonsLairBreezebinderVScript : EncounterBossScript { }

    /// <summary>Overseer Drift-Catcher — miniboss, Normal. Creature2Id 33361.</summary>
    [ScriptFilterCreatureId(33361u)]
    public class StormtalonsLairDriftcatcherNScript : EncounterBossScript { }

    /// <summary>Overseer Drift-Catcher — miniboss, Veteran. Creature2Id 33362.</summary>
    [ScriptFilterCreatureId(33362u)]
    public class StormtalonsLairDriftcatcherVScript : EncounterBossScript { }

    // ── Stormtalon (Final Boss) ─────────────────────────────────────────────────
    //   39826 | Lightning Storm  — large AoE storm damage
    //   70501 | Electric Charge  — movement / dash ability
    //   39633 | Thunder Call     — summoning mechanic
    //   39681 | Static Wave      — wave-shaped telegraph
    //   39596 | Lightning Strike — targeted heavy hit
    //   39580 | Chomp            — melee bite

    public abstract class StormtalonsLairStormtalonBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 39580, initialDelay:  2.0, interval:  5.0); // Chomp
            ScheduleSpell(spell4Id: 39681, initialDelay:  5.0, interval: 10.0); // Static Wave
            ScheduleSpell(spell4Id: 39596, initialDelay:  8.0, interval: 14.0); // Lightning Strike
            ScheduleSpell(spell4Id: 70501, initialDelay: 12.0, interval: 18.0); // Electric Charge
            ScheduleSpell(spell4Id: 39633, initialDelay: 16.0, interval: 22.0); // Thunder Call
            ScheduleSpell(spell4Id: 39826, initialDelay: 22.0, interval: 28.0); // Lightning Storm

            AddPhase(healthPct: 60f, OnStormPhase);
            AddPhase(healthPct: 25f, OnFuryPhase);

            SetEnrage(seconds: 480.0, enrageSpellId: 39826);
        }

        private void OnStormPhase()
        {
            ScheduleSpell(spell4Id: 39826, initialDelay: 3.0, interval: 20.0); // Lightning Storm (faster)
            ScheduleSpell(spell4Id: 39633, initialDelay: 8.0, interval: 18.0); // Thunder Call (faster)
        }

        private void OnFuryPhase()
        {
            ScheduleSpell(spell4Id: 39826, initialDelay: 2.0, interval: 14.0); // Lightning Storm (rapid)
        }
    }

    /// <summary>Stormtalon — final boss, Normal. Creature2Id 17163.</summary>
    [ScriptFilterCreatureId(17163u)]
    public class StormtalonsLairStormtalonNScript : StormtalonsLairStormtalonBase { }

    /// <summary>Stormtalon — final boss, Veteran. Creature2Id 33406.</summary>
    [ScriptFilterCreatureId(33406u)]
    public class StormtalonsLairStormtalonVScript : StormtalonsLairStormtalonBase { }
}
