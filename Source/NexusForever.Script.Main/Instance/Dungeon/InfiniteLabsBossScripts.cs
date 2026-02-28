using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    // Infinite Labs (WorldId 2980) — Boss Encounter Scripts
    // Source: PublicEvent 594 objectives.
    // Creature ID 10569 is objective-derived and treated as fallback until broader
    // table parity data confirms the final encounter mapping.

    /// <summary>Infinite Labs boss — Creature2Id 10569 (unverified).</summary>
    [ScriptFilterCreatureId(10569u)]
    public class InfiniteLabsBossScript : EncounterBossScript { }
}
