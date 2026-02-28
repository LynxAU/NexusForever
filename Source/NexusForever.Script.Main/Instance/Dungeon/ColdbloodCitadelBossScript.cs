using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    // Coldblood Citadel (WorldId 3522) — Boss Encounter Scripts
    // Spell IDs sourced from Spell4.tbl ([CBC] prefix, encounters e5313–e5315).

    // ── Ice Elemental ───────────────────────────────────────────────────────────
    //   87899 | Ice Rage           — primary frost attack (6s CD)
    //   88042 | Sling Snow Boulders — ranged projectiles
    //   88043 | Pound of Ice       — heavy melee slam
    //   88021 | Howling Winds      — major mechanic (20s cast)
    //   88070 | Enrage             — enrage buff (1s cast)

    public class ColdbloodIceElementalScript : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 87899, initialDelay:  2.0, interval:  8.0); // Ice Rage
            ScheduleSpell(spell4Id: 88042, initialDelay:  5.0, interval: 12.0); // Sling Snow Boulders
            ScheduleSpell(spell4Id: 88043, initialDelay:  8.0, interval: 14.0); // Pound of Ice
            ScheduleSpell(spell4Id: 88021, initialDelay: 15.0, interval: 35.0); // Howling Winds

            AddPhase(healthPct: 40f, OnBlizzardPhase);
            SetEnrage(seconds: 480.0, enrageSpellId: 88070); // Enrage
        }

        private void OnBlizzardPhase()
        {
            ScheduleSpell(spell4Id: 87899, initialDelay: 1.0, interval:  6.0); // Ice Rage (faster)
            ScheduleSpell(spell4Id: 88042, initialDelay: 3.0, interval:  9.0); // Sling Snow Boulders (faster)
            ScheduleSpell(spell4Id: 88021, initialDelay: 8.0, interval: 28.0); // Howling Winds (faster)
        }
    }

    /// <summary>Ice Elemental (Ice Boss). Creature2Id 75508, encounter e5313.</summary>
    [ScriptFilterCreatureId(75508u)]
    public class ColdbloodIceBossScript : ColdbloodIceElementalScript { }

    // ── Darksisters / Coven (Council Fight) ─────────────────────────────────────
    //   88167 | Blood Seed          — blood detonation (3s cast)
    //   88257 | Frost Shard Storm   — frost AoE (2s cast)
    //   88260 | Seeking Shadow      — tracking shadow projectile
    //   88173 | Mark of Corruption  — healing absorb debuff (3s cast, 10s channel)
    //   88018 | Blood Tornado       — persistent AoE
    //   88252 | Soulfrost Trail     — ground frost hazard

    public abstract class ColdbloodCovenBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 88167, initialDelay:  3.0, interval: 12.0); // Blood Seed
            ScheduleSpell(spell4Id: 88257, initialDelay:  5.0, interval: 14.0); // Frost Shard Storm
            ScheduleSpell(spell4Id: 88260, initialDelay:  8.0, interval: 16.0); // Seeking Shadow
            ScheduleSpell(spell4Id: 88173, initialDelay: 12.0, interval: 22.0); // Mark of Corruption
            ScheduleSpell(spell4Id: 88018, initialDelay: 16.0, interval: 24.0); // Blood Tornado
            ScheduleSpell(spell4Id: 88252, initialDelay: 20.0, interval: 26.0); // Soulfrost Trail

            AddPhase(healthPct: 40f, OnDesperationPhase);
            SetEnrage(seconds: 480.0, enrageSpellId: 88167); // Blood Seed
        }

        private void OnDesperationPhase()
        {
            ScheduleSpell(spell4Id: 88167, initialDelay: 2.0, interval:  8.0); // Blood Seed (faster)
            ScheduleSpell(spell4Id: 88257, initialDelay: 4.0, interval: 10.0); // Frost Shard Storm (faster)
            ScheduleSpell(spell4Id: 88260, initialDelay: 6.0, interval: 12.0); // Seeking Shadow (faster)
        }
    }

    /// <summary>Darksister #1 (Coven). Creature2Id 75472, encounter e5314.</summary>
    [ScriptFilterCreatureId(75472u)]
    public class ColdbloodDarksister1Script : ColdbloodCovenBase { }

    /// <summary>Darksister #2 (Coven). Creature2Id 75473, encounter e5314.</summary>
    [ScriptFilterCreatureId(75473u)]
    public class ColdbloodDarksister2Script : ColdbloodCovenBase { }

    /// <summary>Darksister #3 (Coven). Creature2Id 75474, encounter e5314.</summary>
    [ScriptFilterCreatureId(75474u)]
    public class ColdbloodDarksister3Script : ColdbloodCovenBase { }

    // ── High Priest (no [CBC]-prefixed spells in Spell4.tbl) ────────────────────

    /// <summary>High Priest. Creature2Id 75509.</summary>
    [ScriptFilterCreatureId(75509u)]
    public class ColdbloodHighPriestScript : EncounterBossScript { }

    // ── Harizog Coldblood (Final Boss) ───────────────────────────────────────────
    //   88198 | Sovereign Slam    — primary AoE melee (2s cast, 6s CD)
    //   88187 | AoE Spikes        — ground spikes (2.5s cast, 3s channel)
    //   88259 | Jump To Player    — gap closer (3s cast)
    //   88205 | Blood Boil        — blood channel (15s CD, 3s channel)
    //   88210 | Soulfrost Surge   — frost channel (1.5s cast, 12s CD, 15s channel)
    //   88202 | Seeking Shadow    — shadow projectile (10s CD)
    //   88186 | Freezing Frenzy   — frost burst (15s CD)
    //   88317 | Seeking Shadow Bomb — shadow detonation (5s cast)

    public class ColdbloodHarizogBase : BossEncounterScript
    {
        protected override void OnBossLoad()
        {
            ScheduleSpell(spell4Id: 88198, initialDelay:  2.0, interval:  8.0); // Sovereign Slam
            ScheduleSpell(spell4Id: 88187, initialDelay:  5.0, interval: 14.0); // AoE Spikes
            ScheduleSpell(spell4Id: 88259, initialDelay:  8.0, interval: 18.0); // Jump To Player
            ScheduleSpell(spell4Id: 88205, initialDelay: 12.0, interval: 20.0); // Blood Boil
            ScheduleSpell(spell4Id: 88210, initialDelay: 16.0, interval: 22.0); // Soulfrost Surge
            ScheduleSpell(spell4Id: 88202, initialDelay: 20.0, interval: 24.0); // Seeking Shadow

            AddPhase(healthPct: 60f, OnBloodPhase);
            AddPhase(healthPct: 30f, OnShadowPhase);

            SetEnrage(seconds: 600.0, enrageSpellId: 87976); // Summon Ancient Echoes
        }

        private void OnBloodPhase()
        {
            ScheduleSpell(spell4Id: 88205, initialDelay: 3.0, interval: 14.0); // Blood Boil (faster)
            ScheduleSpell(spell4Id: 88186, initialDelay: 6.0, interval: 18.0); // Freezing Frenzy
        }

        private void OnShadowPhase()
        {
            ScheduleSpell(spell4Id: 88202, initialDelay: 2.0, interval: 16.0); // Seeking Shadow (faster)
            ScheduleSpell(spell4Id: 88317, initialDelay: 5.0, interval: 20.0); // Seeking Shadow Bomb
            ScheduleSpell(spell4Id: 88198, initialDelay: 3.0, interval:  6.0); // Sovereign Slam (rapid)
        }
    }

    /// <summary>Harizog Coldblood — final boss. Creature2Id 75459, encounter e5315.</summary>
    [ScriptFilterCreatureId(75459u)]
    public class ColdbloodHarizogScript : ColdbloodHarizogBase { }
}
