using NexusForever.Script.Main.Instance;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Dungeon
{
    // Coldblood Citadel (WorldId 3522) — Boss Encounter Scripts
    // Source: PublicEvent 907 objectives.
    // TODO: Creature ID 14447 is unverified — not found in Creature2.tbl extraction.

    /// <summary>Coldblood Citadel boss — Creature2Id 14447 (unverified).</summary>
    [ScriptFilterCreatureId(14447u)]
    public class ColdbloodCitadelBossScript : EncounterBossScript { }
}
