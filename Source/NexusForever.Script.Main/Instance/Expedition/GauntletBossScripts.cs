using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Expedition
{
    // The Gauntlet (WorldId 2183)  Boss Encounter Scripts
    // Spell IDs sourced from Jabbithole (zone Gauntlet named bosses).
    // Boss-to-creature mapping from .tbl extraction:
    //   69254 = Rockstar Yeti (Arena 2, faction 218)
    //   69255 = The Championator (Arena 2, faction 988)
    //   69308 = Showtime / The Morticianatrix (PE446, faction 219)
    //   69305 = Shock King (PE446, faction 219)

    //  Rockstar Yeti
    //   33251 | Maul                primary melee
    //   33252 | Slam                frontal smash
    //   74985 | Cumulative Strikes  stacking melee combo
    //   74991 | Ionized Exhaust     AoE exhaust cloud
    //   74992 | Forced Aggression   enrage/charge ability

    /// <summary>Rockstar Yeti  Creature2Id 69254 (veteran).</summary>
    [ScriptFilterCreatureId(69254u)]
    public class GauntletBoss1Script : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 33251, initialDelay:  2.0, interval:  5.0); // Maul
            ScheduleSpell(spell4Id: 33252, initialDelay:  4.0, interval:  8.0); // Slam
            ScheduleSpell(spell4Id: 74985, initialDelay:  7.0, interval: 10.0); // Cumulative Strikes
            ScheduleSpell(spell4Id: 74991, initialDelay: 12.0, interval: 16.0); // Ionized Exhaust
            ScheduleSpell(spell4Id: 74992, initialDelay: 18.0, interval: 22.0); // Forced Aggression

            AddPhase(healthPct: 40f, OnRagingPhase);
            SetEnrage(seconds: 300.0, enrageSpellId: 74992);
        }

        private void OnRagingPhase()
        {
            ScheduleSpell(spell4Id: 74992, initialDelay: 2.0, interval: 14.0); // Forced Aggression (faster)
        }
    }

    /// <summary>Rockstar Yeti  Creature2Id 48491 (normal equivalent).</summary>
    [ScriptFilterCreatureId(48491u)]
    public class GauntletBoss1NormalScript : GauntletBoss1Script { }

    //  The Championator
    //   5649  | Punch             primary melee
    //   5652  | Jab               quick melee
    //   34552 | Flourishing Combo  melee combo chain
    //   52448 | Haymaker          heavy knockout punch
    //   60653 | Fire Bomb         ranged AoE bomb

    /// <summary>The Championator  Creature2Id 69255 (veteran).</summary>
    [ScriptFilterCreatureId(69255u)]
    public class GauntletBoss2Script : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id:  5649, initialDelay:  2.0, interval:  5.0); // Punch
            ScheduleSpell(spell4Id:  5652, initialDelay:  3.0, interval:  6.0); // Jab
            ScheduleSpell(spell4Id: 34552, initialDelay:  6.0, interval: 10.0); // Flourishing Combo
            ScheduleSpell(spell4Id: 52448, initialDelay: 12.0, interval: 16.0); // Haymaker
            ScheduleSpell(spell4Id: 60653, initialDelay: 10.0, interval: 14.0); // Fire Bomb

            AddPhase(healthPct: 40f, OnChampionPhase);
            SetEnrage(seconds: 300.0, enrageSpellId: 52448);
        }

        private void OnChampionPhase()
        {
            ScheduleSpell(spell4Id: 52448, initialDelay: 2.0, interval: 10.0); // Haymaker (faster)
            ScheduleSpell(spell4Id: 60653, initialDelay: 4.0, interval:  8.0); // Fire Bomb (faster)
        }
    }

    /// <summary>The Championator  Creature2Id 48529 (normal equivalent).</summary>
    [ScriptFilterCreatureId(48529u)]
    public class GauntletBoss2NormalScript : GauntletBoss2Script { }

    //  The Morticianatrix (Showtime)
    //   34062 | Arcane Bolt     ranged spell attack
    //   55595 | Ring of Thorns  PBAoE damage ring

    /// <summary>The Morticianatrix / Showtime  Creature2Id 69308 (veteran).</summary>
    [ScriptFilterCreatureId(69308u)]
    public class GauntletBoss3Script : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 34062, initialDelay: 2.0, interval:  6.0); // Arcane Bolt
            ScheduleSpell(spell4Id: 55595, initialDelay: 8.0, interval: 14.0); // Ring of Thorns

            SetEnrage(seconds: 300.0, enrageSpellId: 55595);
        }
    }

    /// <summary>The Morticianatrix / Showtime  Creature2Id 48579 (normal equivalent).</summary>
    [ScriptFilterCreatureId(48579u)]
    public class GauntletBoss3NormalScript : GauntletBoss3Script { }

    //  Shock King
    //   34062 | Arcane Bolt     ranged spell attack
    //   74700 | Sandstorm       AoE sand/wind ability
    //   74724 | Ground Support  summoning / area control

    /// <summary>Shock King  Creature2Id 69305 (veteran final boss).</summary>
    [ScriptFilterCreatureId(69305u)]
    public class GauntletBoss4Script : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 34062, initialDelay:  2.0, interval:  6.0); // Arcane Bolt
            ScheduleSpell(spell4Id: 74700, initialDelay:  8.0, interval: 14.0); // Sandstorm
            ScheduleSpell(spell4Id: 74724, initialDelay: 14.0, interval: 20.0); // Ground Support

            AddPhase(healthPct: 50f, OnStormPhase);
            SetEnrage(seconds: 300.0, enrageSpellId: 74700);
        }

        private void OnStormPhase()
        {
            ScheduleSpell(spell4Id: 74700, initialDelay: 2.0, interval: 10.0); // Sandstorm (faster)
        }
    }

    /// <summary>Shock King  Creature2Id 48554 (normal final boss equivalent).</summary>
    [ScriptFilterCreatureId(48554u)]
    public class GauntletBoss4NormalScript : GauntletBoss4Script { }
}
