using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Raid
{
    // Red Moon Terror (WorldId 3032 / 3102) — Boss Encounter Scripts
    // Source: Creature2.tbl "[RMT]" name tag search.
    //
    // These scripts are shared between the 20-man (WorldId 3032) and 40-man (WorldId 3102)
    // raids if the same creature IDs are used for both difficulties. If 40-man uses separate
    // creature IDs, additional scripts will be needed.

    // ── e4550 ─ Engineers ─────────────────────────────────────────────────────

    /// <summary>RMT Engineer — Hammer. Creature2Id 65758.</summary>
    [ScriptFilterCreatureId(65758u)]
    public class RMTEngineerHammerScript : EncounterBossScript { }

    /// <summary>RMT Engineer — Gun. Creature2Id 65759.</summary>
    [ScriptFilterCreatureId(65759u)]
    public class RMTEngineerGunScript : EncounterBossScript { }

    // ── e4554 ─ Mordechai Redmoon ─────────────────────────────────────────────

    /// <summary>Mordechai Redmoon. Creature2Id 65800.</summary>
    [ScriptFilterCreatureId(65800u)]
    public class RMTMordechaiRedmoonScript : EncounterBossScript { }

    // ── Laveka ────────────────────────────────────────────────────────────────

    /// <summary>Laveka. Creature2Id 65997.</summary>
    [ScriptFilterCreatureId(65997u)]
    public class RMTLavekaScript : EncounterBossScript { }

    // ── Robomination ─────────────────────────────────────────────────────────

    /// <summary>Robomination. Creature2Id 66085.</summary>
    [ScriptFilterCreatureId(66085u)]
    public class RMTRobominationScript : EncounterBossScript { }
}
