using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Expedition
{
    // Deep Space Exploration (WorldId 2188)  Final Boss: Riptide the Stone Serpent
    // Spell IDs sourced from Jabbithole (/npcs/riptide-the-stone-serpent-143-veteran).
    //
    //   35385 | Slice           melee auto-attack
    //   35386 | Slash           melee cleave
    //   71966 | Tidal Surge     heavy melee strike
    //   71903 | Tsunami Strike  frontal cone knockback
    //   71977 | Drown           targeted DoT debuff
    //   71920 | Water Spout     AoE telegraph

    public abstract class DeepSpaceExplorationBossBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 35385, initialDelay:  2.0, interval:  5.0); // Slice
            ScheduleSpell(spell4Id: 35386, initialDelay:  4.0, interval:  6.0); // Slash
            ScheduleSpell(spell4Id: 71966, initialDelay:  8.0, interval: 12.0); // Tidal Surge
            ScheduleSpell(spell4Id: 71903, initialDelay: 14.0, interval: 18.0); // Tsunami Strike
            ScheduleSpell(spell4Id: 71977, initialDelay: 10.0, interval: 16.0); // Drown
            ScheduleSpell(spell4Id: 71920, initialDelay: 18.0, interval: 22.0); // Water Spout

            AddPhase(healthPct: 50f, OnTidalPhase);
            SetEnrage(seconds: 300.0, enrageSpellId: 71920);
        }

        private void OnTidalPhase()
        {
            ScheduleSpell(spell4Id: 71920, initialDelay: 3.0, interval: 14.0); // Water Spout (faster)
            ScheduleSpell(spell4Id: 71977, initialDelay: 2.0, interval: 10.0); // Drown (faster)
        }
    }

    /// <summary>Riptide the Stone Serpent  veteran. Creature2Id 69299.</summary>
    [ScriptFilterCreatureId(69299u)]
    public class DeepSpaceExplorationBossScript : DeepSpaceExplorationBossBase { }

    /// <summary>Riptide the Stone Serpent  scaling variant. Creature2Id 48704.</summary>
    [ScriptFilterCreatureId(48704u)]
    public class DeepSpaceExplorationBossScalingScript : DeepSpaceExplorationBossBase { }
}
