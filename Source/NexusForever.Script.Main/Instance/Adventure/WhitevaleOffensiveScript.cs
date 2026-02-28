using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Whitevale Offensive adventure (WorldId 1323).
    ///
    /// Type: Assault — players push through Whitevale Detention Center taking down
    /// Dominion command staff in sequential encounters.
    ///
    /// Creature IDs for this map are still being verified; until then this script uses
    /// fallback completion after three unique boss deaths.
    /// </summary>
    [ScriptFilterOwnerId(1323)]
    public class WhitevaleOffensiveScript : AdventureScript
    {
        protected override int FallbackRequiredBossKills => 3;

        protected override void OnAdventureLoad() { }
    }
}
