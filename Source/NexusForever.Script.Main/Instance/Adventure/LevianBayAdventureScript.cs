using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Levian Bay adventure (WorldId 3176).
    ///
    /// Type: Combat — players push through Levian Bay fighting Purewater zealots
    /// and Grimvoid operatives in sequential encounters.
    ///
    /// Boss creature IDs are wired via <c>FallbackAdventureBossScripts.cs</c>; completion
    /// remains fallback-based until wave sequencing is fully scripted.
    /// </summary>
    [ScriptFilterOwnerId(3176)]
    public class LevianBayAdventureScript : AdventureScript
    {
        protected override int FallbackRequiredBossKills => 3;

        protected override void OnAdventureLoad() { }
    }
}
