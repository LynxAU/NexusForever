using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Farside adventure (WorldId 3010).
    ///
    /// Type: Combat — players fight through the Farside moon base against
    /// Eldan-constructed threats in sequential encounter rooms.
    ///
    /// Creature IDs for this map are still being verified; until then this script uses
    /// fallback completion after three unique boss deaths.
    /// </summary>
    [ScriptFilterOwnerId(3010)]
    public class FarsideAdventureScript : AdventureScript
    {
        protected override int FallbackRequiredBossKills => 3;

        protected override void OnAdventureLoad() { }
    }
}
