using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Raid
{
    // Ultimate Protogames (UltiProtogames, WorldId 3041) — Boss Encounter Scripts
    // Source: Creature2.tbl "[UP]" (Ub3r-Proto) name tag search.

    // ── e2675 ─ Hut-Hut (Gorganoth Boss) ─────────────────────────────────────

    /// <summary>Hut-Hut — Gorganoth Boss encounter. Creature2Id 61417.</summary>
    [ScriptFilterCreatureId(61417u)]
    public class UPHutHutScript : EncounterBossScript { }

    // ── e2680 ─ Bev-O-Rage (Vending Machine Boss) ────────────────────────────

    /// <summary>Bev-O-Rage — Vending Machine Boss encounter. Creature2Id 61463.</summary>
    [ScriptFilterCreatureId(61463u)]
    public class UPBevORageScript : EncounterBossScript { }

    // ── Optional Minibosses ───────────────────────────────────────────────────

    /// <summary>Miniboss — Crate Destruction (e2673). Creature2Id 62575.</summary>
    [ScriptFilterCreatureId(62575u)]
    public class UPCrateDestructionScript : EncounterBossScript { }

    /// <summary>Miniboss — Mixed Wave (e2674). Creature2Id 63319.</summary>
    [ScriptFilterCreatureId(63319u)]
    public class UPMixedWaveScript : EncounterBossScript { }
}
