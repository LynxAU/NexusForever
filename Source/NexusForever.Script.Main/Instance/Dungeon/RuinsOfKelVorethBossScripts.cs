using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    // Ruins of Kel Voreth (OsunDungeon, WorldId 1336) — Boss Encounter Scripts
    // Spell IDs sourced from Jabbithole (zone 21).

    // ── Grond the Corpsemaker ───────────────────────────────────────────────────
    //   40118 | Buck       — knockback
    //   37281 | Bite       — primary melee auto
    //   37585 | Bellow     — AoE fear / knockback
    //   37223 | Mutilate   — heavy melee hit
    //   37282 | Chomp      — heavy melee bite
    //   40117 | Headbutt   — stun
    //   48829 | Sand Blast — frontal cone

    public abstract class KelVorethGrondBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 37281, initialDelay:  2.0, interval:  5.0); // Bite
            ScheduleSpell(spell4Id: 37282, initialDelay:  4.0, interval:  8.0); // Chomp
            ScheduleSpell(spell4Id: 40117, initialDelay:  7.0, interval: 14.0); // Headbutt
            ScheduleSpell(spell4Id: 48829, initialDelay: 10.0, interval: 16.0); // Sand Blast
            ScheduleSpell(spell4Id: 37223, initialDelay: 15.0, interval: 20.0); // Mutilate
            ScheduleSpell(spell4Id: 37585, initialDelay: 20.0, interval: 24.0); // Bellow
            ScheduleSpell(spell4Id: 40118, initialDelay: 26.0, interval: 28.0); // Buck

            AddPhase(healthPct: 40f, OnFrenzy);
            SetEnrage(seconds: 420.0, enrageSpellId: 37223);
        }

        private void OnFrenzy()
        {
            ScheduleSpell(spell4Id: 37223, initialDelay: 2.0, interval: 12.0); // Mutilate (faster)
            ScheduleSpell(spell4Id: 37585, initialDelay: 5.0, interval: 16.0); // Bellow (faster)
        }
    }

    /// <summary>Grond the Corpsemaker — Normal. Creature2Id 32534.</summary>
    [ScriptFilterCreatureId(32534u)]
    public class KelVorethGrondNScript : KelVorethGrondBase { }

    /// <summary>Grond the Corpsemaker — Veteran. Creature2Id 32535.</summary>
    [ScriptFilterCreatureId(32535u)]
    public class KelVorethGrondVScript : KelVorethGrondBase { }

    // ── Forgemaster Trogun ──────────────────────────────────────────────────────
    //   38793 | Clobber             — primary melee hit
    //   38794 | Smash               — heavy melee
    //   63107 | Exanite Cuts        — frontal cleave
    //   63106 | Volcanic Strike     — ground fire AoE
    //   51860 | Exanite Shards      — ranged shard projectiles
    //   52521 | Exanite Weapon      — empowered melee
    //   58767 | Surge of Primal Fire — AoE fire burst
    //   52512 | Forgemaster's Call  — summon mechanic

    public abstract class KelVorethTrogunBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 38793, initialDelay:  2.0, interval:  5.0); // Clobber
            ScheduleSpell(spell4Id: 38794, initialDelay:  4.0, interval:  8.0); // Smash
            ScheduleSpell(spell4Id: 63107, initialDelay:  7.0, interval: 12.0); // Exanite Cuts
            ScheduleSpell(spell4Id: 63106, initialDelay: 10.0, interval: 16.0); // Volcanic Strike
            ScheduleSpell(spell4Id: 51860, initialDelay: 14.0, interval: 18.0); // Exanite Shards
            ScheduleSpell(spell4Id: 52521, initialDelay: 18.0, interval: 22.0); // Exanite Weapon
            ScheduleSpell(spell4Id: 58767, initialDelay: 24.0, interval: 26.0); // Surge of Primal Fire
            ScheduleSpell(spell4Id: 52512, initialDelay: 30.0, interval: 35.0); // Forgemaster's Call

            AddPhase(healthPct: 50f, OnForgePhase);
            AddPhase(healthPct: 20f, OnMeltdown);

            SetEnrage(seconds: 480.0, enrageSpellId: 58767);
        }

        private void OnForgePhase()
        {
            ScheduleSpell(spell4Id: 58767, initialDelay: 3.0, interval: 20.0); // Surge of Primal Fire (faster)
            ScheduleSpell(spell4Id: 52521, initialDelay: 6.0, interval: 16.0); // Exanite Weapon (faster)
        }

        private void OnMeltdown()
        {
            ScheduleSpell(spell4Id: 58767, initialDelay: 2.0, interval: 14.0); // Surge of Primal Fire (rapid)
            ScheduleSpell(spell4Id: 63106, initialDelay: 5.0, interval: 10.0); // Volcanic Strike (rapid)
        }
    }

    /// <summary>Forgemaster Trogun — Normal. Creature2Id 32531.</summary>
    [ScriptFilterCreatureId(32531u)]
    public class KelVorethTrogunNScript : KelVorethTrogunBase { }

    /// <summary>Forgemaster Trogun — Veteran. Creature2Id 32533.</summary>
    [ScriptFilterCreatureId(32533u)]
    public class KelVorethTrogunVScript : KelVorethTrogunBase { }

    // ── Slavemaster Drokk (Final Boss) ──────────────────────────────────────────
    //   51563 | Suppression Wave   — AoE wave telegraph
    //   39184 | Bash               — melee auto
    //   39293 | Tracking Beacon    — marks target for mechanic
    //   39183 | Smash              — heavy melee hit
    //   39342 | Homing Barrage     — ranged AoE barrage
    //   39201 | Enslave            — CC — enslaves player temporarily

    public abstract class KelVorethDrokkBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 39184, initialDelay:  2.0, interval:  5.0); // Bash
            ScheduleSpell(spell4Id: 39183, initialDelay:  5.0, interval:  9.0); // Smash
            ScheduleSpell(spell4Id: 51563, initialDelay:  9.0, interval: 14.0); // Suppression Wave
            ScheduleSpell(spell4Id: 39293, initialDelay: 14.0, interval: 20.0); // Tracking Beacon
            ScheduleSpell(spell4Id: 39342, initialDelay: 18.0, interval: 22.0); // Homing Barrage
            ScheduleSpell(spell4Id: 39201, initialDelay: 25.0, interval: 30.0); // Enslave

            AddPhase(healthPct: 60f, OnSuppressionPhase);
            AddPhase(healthPct: 25f, OnDesperation);

            SetEnrage(seconds: 480.0, enrageSpellId: 51563);
        }

        private void OnSuppressionPhase()
        {
            ScheduleSpell(spell4Id: 51563, initialDelay: 3.0, interval: 10.0); // Suppression Wave (faster)
            ScheduleSpell(spell4Id: 39201, initialDelay: 8.0, interval: 24.0); // Enslave (faster)
        }

        private void OnDesperation()
        {
            ScheduleSpell(spell4Id: 39342, initialDelay: 2.0, interval: 14.0); // Homing Barrage (rapid)
            ScheduleSpell(spell4Id: 51563, initialDelay: 5.0, interval:  8.0); // Suppression Wave (rapid)
        }
    }

    /// <summary>Slavemaster Drokk — final boss, Normal. Creature2Id 32536.</summary>
    [ScriptFilterCreatureId(32536u)]
    public class KelVorethDrokkNScript : KelVorethDrokkBase { }

    /// <summary>Slavemaster Drokk — final boss, Veteran. Creature2Id 32539.</summary>
    [ScriptFilterCreatureId(32539u)]
    public class KelVorethDrokkVScript : KelVorethDrokkBase { }

    // ── Darkwitch Gurka (optional miniboss — no Jabbithole spell data) ───────

    /// <summary>Darkwitch Gurka — optional miniboss, Normal. Creature2Id 33049.</summary>
    [ScriptFilterCreatureId(33049u)]
    public class KelVorethGurkaNScript : EncounterBossScript { }

    /// <summary>Darkwitch Gurka — optional miniboss, Veteran. Creature2Id 33050.</summary>
    [ScriptFilterCreatureId(33050u)]
    public class KelVorethGurkaVScript : EncounterBossScript { }
}
