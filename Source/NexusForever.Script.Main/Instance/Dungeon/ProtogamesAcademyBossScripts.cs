using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    // Protogames Academy (UltimateProtogamesJuniors, WorldId 3173) — Boss Encounter Scripts
    // Spell IDs sourced from Spell4.tbl ([UPJ] prefix, encounters e4342–e4499).

    // ── Invulnotron ─────────────────────────────────────────────────────────────
    //   81701 | Discharge        — quick electric burst (500ms)
    //   81835 | Corroded Armor   — armor debuff on target
    //   80556 | Disintegrate     — heavy beam damage (3s cast)
    //   81825 | Build Up         — stacking damage buff (self)
    //   81673 | Shield & Destroy — wipe mechanic / interrupt check (25s cast)

    public abstract class ProtogamesInvulnotronBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 81701, initialDelay:  2.0, interval:  5.0); // Discharge
            ScheduleSpell(spell4Id: 81835, initialDelay:  5.0, interval: 12.0); // Corroded Armor
            ScheduleSpell(spell4Id: 80556, initialDelay:  8.0, interval: 14.0); // Disintegrate
            ScheduleSpell(spell4Id: 81825, initialDelay: 12.0, interval: 18.0); // Build Up

            AddPhase(healthPct: 40f, OnOverloadPhase);
            SetEnrage(seconds: 300.0, enrageSpellId: 81673); // Shield and Destroy
        }

        private void OnOverloadPhase()
        {
            ScheduleSpell(spell4Id: 81701, initialDelay: 1.0, interval:  4.0); // Discharge (faster)
            ScheduleSpell(spell4Id: 80556, initialDelay: 3.0, interval: 10.0); // Disintegrate (faster)
        }
    }

    /// <summary>Invulnotron, Normal. Creature2Id 67475, TG 12356.</summary>
    [ScriptFilterCreatureId(67475u)]
    public class ProtogamesInvulnotronNScript : ProtogamesInvulnotronBase { }

    /// <summary>Invulnotron, Veteran. Creature2Id 71082, TG 12356.</summary>
    [ScriptFilterCreatureId(71082u)]
    public class ProtogamesInvulnotronVScript : ProtogamesInvulnotronBase { }

    // ── Gromka the Flamewitch (Osun Witch) ──────────────────────────────────────
    //   82189 | Eruption      — fire burst (500ms)
    //   76209 | Flamethrower  — frontal fire cone
    //   76221 | Flame Wave    — AoE fire damage
    //   81984 | Demon's Step  — charge / teleport (3s)
    //   81987 | Inferno       — heavy fire burst (2s)
    //   82114 | Hellfire      — fire damage

    public abstract class ProtogamesGromkaBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 82189, initialDelay:  2.0, interval:  5.0); // Eruption
            ScheduleSpell(spell4Id: 76209, initialDelay:  4.0, interval:  8.0); // Flamethrower
            ScheduleSpell(spell4Id: 76221, initialDelay:  7.0, interval: 12.0); // Flame Wave
            ScheduleSpell(spell4Id: 81984, initialDelay: 10.0, interval: 16.0); // Demon's Step
            ScheduleSpell(spell4Id: 81987, initialDelay: 14.0, interval: 18.0); // Inferno
            ScheduleSpell(spell4Id: 82114, initialDelay: 18.0, interval: 22.0); // Hellfire

            AddPhase(healthPct: 40f, OnInfernoPhase);
            SetEnrage(seconds: 360.0, enrageSpellId: 76221); // Flame Wave
        }

        private void OnInfernoPhase()
        {
            ScheduleSpell(spell4Id: 81987, initialDelay: 2.0, interval: 12.0); // Inferno (faster)
            ScheduleSpell(spell4Id: 82114, initialDelay: 5.0, interval: 14.0); // Hellfire (faster)
        }
    }

    /// <summary>Gromka the Flamewitch (Osun Witch) — Normal. Creature2Id 67594, TG 12357.</summary>
    [ScriptFilterCreatureId(67594u)]
    public class ProtogamesOsunWitchNScript : ProtogamesGromkaBase { }

    /// <summary>Gromka the Flamewitch (Osun Witch) — Veteran. Creature2Id 71319, TG 12357.</summary>
    [ScriptFilterCreatureId(71319u)]
    public class ProtogamesOsunWitchVScript : ProtogamesGromkaBase { }

    // ── Iruki Boldbeard ─────────────────────────────────────────────────────────
    //   82274 | Meteor Fire        — tracking fire circles (500ms)
    //   82265 | Static Instability — lightning damage
    //   82906 | Matter Distortion  — distortion field
    //   82912 | Anti-Matter Field  — defensive shield (250ms)
    //   82235 | Annihilate         — safe-zone mechanic (20s cast)

    public abstract class ProtogamesIrukiBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 82274, initialDelay:  2.0, interval:  6.0); // Meteor Fire
            ScheduleSpell(spell4Id: 82265, initialDelay:  5.0, interval: 10.0); // Static Instability
            ScheduleSpell(spell4Id: 82906, initialDelay:  8.0, interval: 14.0); // Matter Distortion
            ScheduleSpell(spell4Id: 82912, initialDelay: 14.0, interval: 20.0); // Anti-Matter Field
            ScheduleSpell(spell4Id: 82235, initialDelay: 20.0, interval: 35.0); // Annihilate

            AddPhase(healthPct: 40f, OnMatterPhase);
            SetEnrage(seconds: 360.0, enrageSpellId: 82235); // Annihilate
        }

        private void OnMatterPhase()
        {
            ScheduleSpell(spell4Id: 82274, initialDelay: 1.0, interval:  4.0); // Meteor Fire (faster)
            ScheduleSpell(spell4Id: 82265, initialDelay: 3.0, interval:  8.0); // Static Instability (faster)
        }
    }

    /// <summary>Iruki Boldbeard — Normal. Creature2Id 67663, TG 12358.</summary>
    [ScriptFilterCreatureId(67663u)]
    public class ProtogamesIrukiNScript : ProtogamesIrukiBase { }

    /// <summary>Iruki Boldbeard — Veteran. Creature2Id 71538, TG 12358.</summary>
    [ScriptFilterCreatureId(71538u)]
    public class ProtogamesIrukiVScript : ProtogamesIrukiBase { }

    // ── Seek-N-Slaughter ────────────────────────────────────────────────────────
    //   81647 | Barrage         — channeled multi-hit (4.5s channel, 600ms pulse)
    //   81702 | Homing Missile  — tracking projectile (3.5s)
    //   76282 | Battlebot Missile — area missile barrage (6s)
    //   76342 | Bomb Explosion  — summon bombs (20s)

    public abstract class ProtogamesSeekNSlaughterBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 81647, initialDelay:  3.0, interval:  8.0); // Barrage
            ScheduleSpell(spell4Id: 81702, initialDelay:  6.0, interval: 12.0); // Homing Missile
            ScheduleSpell(spell4Id: 76282, initialDelay: 10.0, interval: 18.0); // Battlebot Missile
            ScheduleSpell(spell4Id: 76342, initialDelay: 16.0, interval: 30.0); // Bomb Explosion

            AddPhase(healthPct: 35f, OnOverdrivePhase);
            SetEnrage(seconds: 360.0, enrageSpellId: 76342); // Bomb Explosion
        }

        private void OnOverdrivePhase()
        {
            ScheduleSpell(spell4Id: 81647, initialDelay: 2.0, interval:  6.0); // Barrage (faster)
            ScheduleSpell(spell4Id: 81702, initialDelay: 4.0, interval:  9.0); // Homing Missile (faster)
        }
    }

    /// <summary>Seek-N-Slaughter — Normal. Creature2Id 67668, TG 12359.</summary>
    [ScriptFilterCreatureId(67668u)]
    public class ProtogamesSeekNSlaughterNScript : ProtogamesSeekNSlaughterBase { }

    /// <summary>Seek-N-Slaughter — Veteran. Creature2Id 71203, TG 12359.</summary>
    [ScriptFilterCreatureId(71203u)]
    public class ProtogamesSeekNSlaughterVScript : ProtogamesSeekNSlaughterBase { }

    // ── Icebox Mk. 2 ───────────────────────────────────────────────────────────
    //   81788 | Blast         — ranged frost attack
    //   79254 | Frost Blast   — frost burst (5s)
    //   81722 | Ice Spikes    — major frost mechanic (20s)
    //   81891 | Self Explosion — AoE explosion (600ms)

    public abstract class ProtogamesIceboxBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 81788, initialDelay:  2.0, interval:  5.0); // Blast
            ScheduleSpell(spell4Id: 79254, initialDelay:  6.0, interval: 12.0); // Frost Blast
            ScheduleSpell(spell4Id: 81891, initialDelay: 10.0, interval: 16.0); // Self Explosion
            ScheduleSpell(spell4Id: 81722, initialDelay: 14.0, interval: 28.0); // Ice Spikes

            AddPhase(healthPct: 40f, OnFrostPhase);
            SetEnrage(seconds: 360.0, enrageSpellId: 79254); // Frost Blast
        }

        private void OnFrostPhase()
        {
            ScheduleSpell(spell4Id: 79254, initialDelay: 2.0, interval:  8.0); // Frost Blast (faster)
            ScheduleSpell(spell4Id: 81722, initialDelay: 5.0, interval: 20.0); // Ice Spikes (faster)
        }
    }

    /// <summary>Icebox Mk. 2 — Normal. Creature2Id 67757, TG 12360.</summary>
    [ScriptFilterCreatureId(67757u)]
    public class ProtogamesIceboxNScript : ProtogamesIceboxBase { }

    /// <summary>Icebox Mk. 2 — Veteran. Creature2Id 71205, TG 12360.</summary>
    [ScriptFilterCreatureId(71205u)]
    public class ProtogamesIceboxVScript : ProtogamesIceboxBase { }

    // ── Super-Invulnotron (Final Boss) ──────────────────────────────────────────
    //   77057 | Smash and Pound — heavy melee slam (3s)
    //   77044 | Blast           — ranged energy attack
    //   81943 | Kinetic Lance   — heavy beam (4.5s)
    //   77081 | Botherbot Smashy — summon botherbot (2s)
    //   77139 | Pillar Damage   — AoE pillar explosion

    public abstract class ProtogamesSuperInvulnotronBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 77057, initialDelay:  2.0, interval:  6.0); // Smash and Pound
            ScheduleSpell(spell4Id: 77044, initialDelay:  4.0, interval:  8.0); // Blast
            ScheduleSpell(spell4Id: 81943, initialDelay:  8.0, interval: 14.0); // Kinetic Lance
            ScheduleSpell(spell4Id: 77081, initialDelay: 12.0, interval: 20.0); // Botherbot Smashy
            ScheduleSpell(spell4Id: 77139, initialDelay: 18.0, interval: 24.0); // Pillar Damage

            AddPhase(healthPct: 50f, OnOverchargePhase);
            AddPhase(healthPct: 20f, OnMeltdownPhase);

            SetEnrage(seconds: 420.0, enrageSpellId: 77139); // Pillar Damage
        }

        private void OnOverchargePhase()
        {
            ScheduleSpell(spell4Id: 81943, initialDelay: 3.0, interval: 10.0); // Kinetic Lance (faster)
            ScheduleSpell(spell4Id: 77139, initialDelay: 6.0, interval: 18.0); // Pillar Damage (faster)
        }

        private void OnMeltdownPhase()
        {
            ScheduleSpell(spell4Id: 77057, initialDelay: 1.0, interval:  4.0); // Smash and Pound (rapid)
            ScheduleSpell(spell4Id: 81943, initialDelay: 3.0, interval:  8.0); // Kinetic Lance (rapid)
            ScheduleSpell(spell4Id: 77139, initialDelay: 5.0, interval: 12.0); // Pillar Damage (rapid)
        }
    }

    /// <summary>Super-Invulnotron — final boss, Normal. Creature2Id 68096, TG 12361.</summary>
    [ScriptFilterCreatureId(68096u)]
    public class ProtogamesSuperInvulnotronNScript : ProtogamesSuperInvulnotronBase { }

    /// <summary>Super-Invulnotron — final boss, Veteran. Creature2Id 71209, TG 12361.</summary>
    [ScriptFilterCreatureId(71209u)]
    public class ProtogamesSuperInvulnotronVScript : ProtogamesSuperInvulnotronBase { }

    // ── Gorganoth (Optional / Secret Boss — no [UPJ] spell data) ─────────────

    /// <summary>Gorganoth — optional secret boss, Normal. Creature2Id 67944.</summary>
    [ScriptFilterCreatureId(67944u)]
    public class ProtogamesGorganothNScript : EncounterBossScript { }

    /// <summary>Gorganoth — optional secret boss, Veteran. Creature2Id 71226.</summary>
    [ScriptFilterCreatureId(71226u)]
    public class ProtogamesGorganothVScript : EncounterBossScript { }
}
