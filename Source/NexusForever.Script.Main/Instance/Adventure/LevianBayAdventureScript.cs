using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Levian Bay adventure (WorldId 3176).
    ///
    /// Type: Combat — players push through Levian Bay fighting Purewater zealots
    /// and Grimvoid operatives in sequential encounters.
    ///
    /// Exact Levian Bay encounter IDs are still being mapped; until then this script
    /// uses fallback completion after three unique boss deaths.
    /// </summary>
    [ScriptFilterOwnerId(3176)]
    public class LevianBayAdventureScript : AdventureScript
    {
        protected override int FallbackRequiredBossKills => 3;

        protected override void OnAdventureLoad() { }
    }
}
