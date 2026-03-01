using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Whitevale Offensive adventure (WorldId 1323).
    ///
    /// Type: Assault — players push through Whitevale Detention Center taking down
    /// Dominion command staff in sequential encounters.
    ///
    /// Boss creature IDs are wired via <c>FallbackAdventureBossScripts.cs</c>; completion
    /// remains fallback-based until wave sequencing is fully scripted.
    /// </summary>
    [ScriptFilterOwnerId(1323)]
    public class WhitevaleOffensiveScript : AdventureScript
    {
        protected override int FallbackRequiredBossKills => 3;

        protected override void OnAdventureLoad() { }
    }
}
