using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Instance.Adventure
{
    /// <summary>
    /// Map script for Northern Wilds adventure / War of the Wilds (WorldId 1393).
    ///
    /// Three sequential boss encounters (normal and veteran variants):
    ///   Lord Hoarfrost   25967 (normal) / 52497 (veteran)
    ///   Glaciax          25968 (normal) / 52498 (veteran)
    ///   The Frozen Corrupter  25970 (normal) / 52499 (veteran)
    ///
    /// Uses FallbackRequiredBossKills because only one variant set (normal OR veteran)
    /// spawns per instance  any 3 unique boss deaths completes the adventure.
    /// </summary>
    [ScriptFilterOwnerId(1393)]
    public class NorthernWildsAdventureScript : AdventureScript
    {
        protected override int FallbackRequiredBossKills => 3;

        protected override void OnAdventureLoad()
        {
            // No explicit waves  FallbackRequiredBossKills handles completion.
        }
    }
}
