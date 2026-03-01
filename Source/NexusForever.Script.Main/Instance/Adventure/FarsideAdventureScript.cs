using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Farside adventure (WorldId 3010).
    ///
    /// Type: Combat — players fight through the Farside moon base against
    /// Eldan-constructed threats in sequential encounter rooms.
    ///
    /// Boss creature IDs are wired via <c>FallbackAdventureBossScripts.cs</c>; completion
    /// remains fallback-based until wave sequencing is fully scripted.
    /// </summary>
    [ScriptFilterOwnerId(3010)]
    public class FarsideAdventureScript : AdventureScript
    {
        protected override int FallbackRequiredBossKills => 3;

        protected override void OnAdventureLoad() { }
    }
}
