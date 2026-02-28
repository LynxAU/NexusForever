using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    // Coldblood Citadel (WorldId 3522) — Boss Encounter Scripts
    // Source: Creature2.tbl name search (prefix [CBC]).

    // ── Darksisters (3-member council) ────────────────────────────────────────

    /// <summary>Darksister #1. Creature2Id 75472.</summary>
    [ScriptFilterCreatureId(75472u)]
    public class ColdbloodDarksister1Script : EncounterBossScript { }

    /// <summary>Darksister #2. Creature2Id 75473.</summary>
    [ScriptFilterCreatureId(75473u)]
    public class ColdbloodDarksister2Script : EncounterBossScript { }

    /// <summary>Darksister #3. Creature2Id 75474.</summary>
    [ScriptFilterCreatureId(75474u)]
    public class ColdbloodDarksister3Script : EncounterBossScript { }

    // ── Ice Boss ──────────────────────────────────────────────────────────────

    /// <summary>Ice Boss. Creature2Id 75508.</summary>
    [ScriptFilterCreatureId(75508u)]
    public class ColdbloodIceBossScript : EncounterBossScript { }

    // ── High Priest ───────────────────────────────────────────────────────────

    /// <summary>High Priest. Creature2Id 75509.</summary>
    [ScriptFilterCreatureId(75509u)]
    public class ColdbloodHighPriestScript : EncounterBossScript { }

    // ── Harizog Coldblood (Final Boss) ────────────────────────────────────────

    /// <summary>Harizog Coldblood — final boss. Creature2Id 75459.</summary>
    [ScriptFilterCreatureId(75459u)]
    public class ColdbloodHarizogScript : EncounterBossScript { }
}
