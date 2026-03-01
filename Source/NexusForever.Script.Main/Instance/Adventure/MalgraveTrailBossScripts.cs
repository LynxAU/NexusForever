using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    // Malgrave Trail Adventure (WorldId 1181)  Boss Encounter Scripts
    // Spell IDs sourced from Jabbithole (zone 24, Malgrave Trail named bosses).

    //  Murk (Soldier Setpiece 4 Boss)
    //   28657 | Sweeping Strike  wide frontal melee
    //   65953 | Smash           heavy overhead
    //   65952 | Clobber         melee stun
    //   77170 | Directed Rage   ranged charge/rage

    /// <summary>Murk  Soldier SP4 Boss. Creature2Id 20814.</summary>
    [ScriptFilterCreatureId(20814u)]
    public class MalgraveMurkBossScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 65952, initialDelay:  2.0, interval:  6.0); // Clobber
            ScheduleSpell(spell4Id: 65953, initialDelay:  4.0, interval:  8.0); // Smash
            ScheduleSpell(spell4Id: 28657, initialDelay:  8.0, interval: 12.0); // Sweeping Strike
            ScheduleSpell(spell4Id: 77170, initialDelay: 14.0, interval: 18.0); // Directed Rage

            AddPhase(healthPct: 40f, OnEnragedPhase);
            SetEnrage(seconds: 360.0, enrageSpellId: 77170);
        }

        private void OnEnragedPhase()
        {
            ScheduleSpell(spell4Id: 77170, initialDelay: 2.0, interval: 12.0); // Directed Rage (faster)
        }
    }

    //  Gurk (Soldier Setpiece 4 Boss)
    // Same spell set as Murk (they're a paired encounter).

    /// <summary>Gurk  Soldier SP4 Boss. Creature2Id 20828.</summary>
    [ScriptFilterCreatureId(20828u)]
    public class MalgraveGurkBossScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 65952, initialDelay:  3.0, interval:  6.0); // Clobber
            ScheduleSpell(spell4Id: 65953, initialDelay:  5.0, interval:  8.0); // Smash
            ScheduleSpell(spell4Id: 28657, initialDelay:  9.0, interval: 12.0); // Sweeping Strike
            ScheduleSpell(spell4Id: 77170, initialDelay: 16.0, interval: 18.0); // Directed Rage

            AddPhase(healthPct: 40f, OnEnragedPhase);
            SetEnrage(seconds: 360.0, enrageSpellId: 77170);
        }

        private void OnEnragedPhase()
        {
            ScheduleSpell(spell4Id: 77170, initialDelay: 3.0, interval: 12.0); // Directed Rage (faster)
        }
    }

    //  Elemental Horror (Scientist Setpiece 4)
    //   28543 | Elemental Strike    primary melee
    //   66392 | Muck Rake           frontal cleave
    //   66384 | Splattering Slime   AoE slime splash

    /// <summary>Elemental Horror  Scientist SP4 Boss. Creature2Id 20584.</summary>
    [ScriptFilterCreatureId(20584u)]
    public class MalgraveElementalHorrorBossScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 28543, initialDelay:  2.0, interval:  5.0); // Elemental Strike
            ScheduleSpell(spell4Id: 66392, initialDelay:  6.0, interval: 10.0); // Muck Rake
            ScheduleSpell(spell4Id: 66384, initialDelay: 10.0, interval: 14.0); // Splattering Slime

            SetEnrage(seconds: 360.0, enrageSpellId: 66384);
        }
    }

    //  Blisterbone Dreglord
    //   65717 | Plasma Shot     ranged primary
    //   66104 | Longshot        long-range snipe (Tier 3)
    //   66100 | Mortar          AoE mortar barrage (Tier 3)
    //   66112 | Shell Storm     AoE shell rain (Tier 3)
    //   69165 | Bleed           DoT debuff
    //   69164 | Burning Flames  fire DoT

    /// <summary>Blisterbone Dreglord  Group Boss. Creature2Id 19818.</summary>
    [ScriptFilterCreatureId(19818u)]
    public class MalgraveDreglordBossScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 65717, initialDelay:  2.0, interval:  5.0); // Plasma Shot
            ScheduleSpell(spell4Id: 66104, initialDelay:  6.0, interval: 10.0); // Longshot
            ScheduleSpell(spell4Id: 66100, initialDelay: 10.0, interval: 16.0); // Mortar
            ScheduleSpell(spell4Id: 66112, initialDelay: 16.0, interval: 22.0); // Shell Storm
            ScheduleSpell(spell4Id: 69165, initialDelay:  8.0, interval: 14.0); // Bleed
            ScheduleSpell(spell4Id: 69164, initialDelay: 12.0, interval: 18.0); // Burning Flames

            AddPhase(healthPct: 50f, OnDesperatePhase);
            SetEnrage(seconds: 360.0, enrageSpellId: 66112);
        }

        private void OnDesperatePhase()
        {
            ScheduleSpell(spell4Id: 66112, initialDelay: 3.0, interval: 14.0); // Shell Storm (faster)
            ScheduleSpell(spell4Id: 66100, initialDelay: 5.0, interval: 10.0); // Mortar (faster)
        }
    }

    //  Spirit of Clanlord Vezrek (Explorer 3)
    //   66277 | Double Strike  melee combo
    //   66105 | Onslaught      charge / heavy melee

    public abstract class MalgraveVezrekBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 66277, initialDelay: 2.0, interval:  6.0); // Double Strike
            ScheduleSpell(spell4Id: 66105, initialDelay: 8.0, interval: 14.0); // Onslaught

            SetEnrage(seconds: 360.0, enrageSpellId: 66105);
        }
    }

    /// <summary>Clanlord Vezrek  Normal. Creature2Id 32687.</summary>
    [ScriptFilterCreatureId(32687u)]
    public class MalgraveVezrekNScript : MalgraveVezrekBase { }

    /// <summary>Clanlord Vezrek  Veteran. Creature2Id 53250.</summary>
    [ScriptFilterCreatureId(53250u)]
    public class MalgraveVezrekVScript : MalgraveVezrekBase { }
}
