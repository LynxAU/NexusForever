using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    // Infinite Labs (WorldId 2980) — Boss Encounter Scripts
    // Source: PublicEvent 594 objectives.
    // TODO: Creature ID 10569 is unverified — not found in Creature2.tbl extraction.

    /// <summary>Infinite Labs boss — Creature2Id 10569 (unverified).</summary>
    [ScriptFilterCreatureId(10569u)]
    public class InfiniteLabsBossScript : EncounterBossScript { }
}
