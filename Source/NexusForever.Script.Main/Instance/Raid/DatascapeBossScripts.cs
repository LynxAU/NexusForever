using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Raid
{
    // Datascape (DataScape, WorldId 1333) — Boss Encounter Scripts
    // Source: Creature2.tbl "[DS] eXXX" name tag search.
    //
    // On death each script calls IContentMapInstance.TriggerBossDeath via EncounterBossScript.
    // DatascapeScript.OnBossDeath tracks kills and fires MatchFinish after all 11 required deaths.

    // ── e385 ─ System Daemons ─────────────────────────────────────────────────

    /// <summary>System Daemons — Null Boss. Creature2Id 30495.</summary>
    [ScriptFilterCreatureId(30495u)]
    public class DSSystemDaemonsNullScript : EncounterBossScript { }

    /// <summary>System Daemons — Binary Boss. Creature2Id 30496.</summary>
    [ScriptFilterCreatureId(30496u)]
    public class DSSystemDaemonsBinaryScript : EncounterBossScript { }

    // ── e390 ─ Maelstrom Authority ────────────────────────────────────────────

    /// <summary>Maelstrom Authority — Air Boss. Creature2Id 30497.</summary>
    [ScriptFilterCreatureId(30497u)]
    public class DSMaelstromAuthorityScript : EncounterBossScript { }

    // ── e393 ─ Gloomclaw ──────────────────────────────────────────────────────

    /// <summary>Gloomclaw. Creature2Id 30498.</summary>
    [ScriptFilterCreatureId(30498u)]
    public class DSGloomclawScript : EncounterBossScript { }

    // ── e395 ─ Elemental Bosses ───────────────────────────────────────────────
    // Six elemental boss encounters in the Datascape. All six must be defeated.

    /// <summary>Earth Elemental Boss. Creature2Id 30499.</summary>
    [ScriptFilterCreatureId(30499u)]
    public class DSEarthElementalScript : EncounterBossScript { }

    /// <summary>Water Elemental Boss. Creature2Id 30500.</summary>
    [ScriptFilterCreatureId(30500u)]
    public class DSWaterElementalScript : EncounterBossScript { }

    /// <summary>Life Elemental Boss. Creature2Id 30501.</summary>
    [ScriptFilterCreatureId(30501u)]
    public class DSLifeElementalScript : EncounterBossScript { }

    /// <summary>Air Elemental Boss. Creature2Id 30502.</summary>
    [ScriptFilterCreatureId(30502u)]
    public class DSAirElementalScript : EncounterBossScript { }

    /// <summary>Fire Elemental Boss. Creature2Id 30503.</summary>
    [ScriptFilterCreatureId(30503u)]
    public class DSFireElementalScript : EncounterBossScript { }

    /// <summary>Logic Elemental Boss. Creature2Id 30504.</summary>
    [ScriptFilterCreatureId(30504u)]
    public class DSLogicElementalScript : EncounterBossScript { }

    // ── e399 ─ Avatus — Final Boss ────────────────────────────────────────────

    /// <summary>Avatus — final boss of Datascape. Creature2Id 30505.</summary>
    [ScriptFilterCreatureId(30505u)]
    public class DSAvatusScript : EncounterBossScript { }
}
