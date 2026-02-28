using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    // War of the Wilds / Northern Wilds Adventure (WorldId 1393)  Boss Encounter Scripts
    // Spell IDs sourced from Jabbithole (zone 32, War of the Wilds named bosses).
    // Each boss has a Normal (level 25-50) and Veteran (level 50, Tier 3) variant.

    //  Lord Hoarfrost
    //   65937 | Pound              primary melee
    //   65938 | Swipe              frontal cleave
    //   66137 | Unearthing Smash   ground pound AoE
    //   66135 | Devastation        heavy AoE slam
    //   69192 | Rock Throw         ranged boulder toss

    public abstract class NorthernWildsHoarfrostBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 65937, initialDelay:  2.0, interval:  5.0); // Pound
            ScheduleSpell(spell4Id: 65938, initialDelay:  4.0, interval:  7.0); // Swipe
            ScheduleSpell(spell4Id: 66137, initialDelay:  8.0, interval: 14.0); // Unearthing Smash
            ScheduleSpell(spell4Id: 66135, initialDelay: 14.0, interval: 20.0); // Devastation
            ScheduleSpell(spell4Id: 69192, initialDelay: 10.0, interval: 16.0); // Rock Throw

            AddPhase(healthPct: 40f, OnRagingPhase);
            SetEnrage(seconds: 360.0, enrageSpellId: 66135);
        }

        private void OnRagingPhase()
        {
            ScheduleSpell(spell4Id: 66135, initialDelay: 3.0, interval: 12.0); // Devastation (faster)
            ScheduleSpell(spell4Id: 69192, initialDelay: 5.0, interval: 10.0); // Rock Throw (faster)
        }
    }

    /// <summary>Lord Hoarfrost  Normal. Creature2Id 25967.</summary>
    [ScriptFilterCreatureId(25967u)]
    public class NorthernWildsHoarfrostNScript : NorthernWildsHoarfrostBase { }

    /// <summary>Lord Hoarfrost  Veteran. Creature2Id 52497.</summary>
    [ScriptFilterCreatureId(52497u)]
    public class NorthernWildsHoarfrostVScript : NorthernWildsHoarfrostBase { }

    //  Glaciax
    //   65769 | Smash         primary melee
    //   65770 | Crush         heavy melee
    //   66096 | Ground Slam   frontal ground smash (Tier 2)
    //   66167 | Heavy Rain    AoE rain damage
    //   66133 | Maelstrom     large AoE vortex

    public abstract class NorthernWildsGlaciaxBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 65769, initialDelay:  2.0, interval:  5.0); // Smash
            ScheduleSpell(spell4Id: 65770, initialDelay:  4.0, interval:  8.0); // Crush
            ScheduleSpell(spell4Id: 66096, initialDelay:  7.0, interval: 12.0); // Ground Slam
            ScheduleSpell(spell4Id: 66167, initialDelay: 12.0, interval: 18.0); // Heavy Rain
            ScheduleSpell(spell4Id: 66133, initialDelay: 18.0, interval: 24.0); // Maelstrom

            AddPhase(healthPct: 50f, OnStormPhase);
            SetEnrage(seconds: 360.0, enrageSpellId: 66133);
        }

        private void OnStormPhase()
        {
            ScheduleSpell(spell4Id: 66133, initialDelay: 3.0, interval: 16.0); // Maelstrom (faster)
        }
    }

    /// <summary>Glaciax  Normal. Creature2Id 25968.</summary>
    [ScriptFilterCreatureId(25968u)]
    public class NorthernWildsGlaciaxNScript : NorthernWildsGlaciaxBase { }

    /// <summary>Glaciax  Veteran. Creature2Id 52498.</summary>
    [ScriptFilterCreatureId(52498u)]
    public class NorthernWildsGlaciaxVScript : NorthernWildsGlaciaxBase { }

    //  The Frozen Corrupter
    //   66181 | Swipe              primary melee cleave
    //   66180 | Ravage             heavy melee flurry
    //   62887 | Strain Overgrowth  AoE corruption field

    public abstract class NorthernWildsCorrupterBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 66181, initialDelay:  2.0, interval:  5.0); // Swipe
            ScheduleSpell(spell4Id: 66180, initialDelay:  6.0, interval: 10.0); // Ravage
            ScheduleSpell(spell4Id: 62887, initialDelay: 10.0, interval: 16.0); // Strain Overgrowth

            AddPhase(healthPct: 40f, OnCorruptedPhase);
            SetEnrage(seconds: 360.0, enrageSpellId: 62887);
        }

        private void OnCorruptedPhase()
        {
            ScheduleSpell(spell4Id: 62887, initialDelay: 2.0, interval: 10.0); // Strain Overgrowth (faster)
            ScheduleSpell(spell4Id: 66180, initialDelay: 3.0, interval:  7.0); // Ravage (faster)
        }
    }

    /// <summary>The Frozen Corrupter  Normal. Creature2Id 25970.</summary>
    [ScriptFilterCreatureId(25970u)]
    public class NorthernWildsCorrupterNScript : NorthernWildsCorrupterBase { }

    /// <summary>The Frozen Corrupter  Veteran. Creature2Id 52499.</summary>
    [ScriptFilterCreatureId(52499u)]
    public class NorthernWildsCorrupterVScript : NorthernWildsCorrupterBase { }
}
